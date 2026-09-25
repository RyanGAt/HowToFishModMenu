using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace HowToFishCustomMenu
{
    // Player list, host-authoritative player actions, item catalogue, locations and boat extras.
    // Remote-player actions only use server paths the game itself uses (TargetRpc teleport /
    // knockback, PlayerVitals server methods, ItemManager spawning), so they need the host.
    internal sealed partial class GameBridge
    {
        private const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        public object LocalPlayerObject { get { RefreshPlayer(); return localPlayer; } }

        public List<object> Players()
        {
            var list = new List<object>();
            if (GetMember(FindType("PlayerManager"), "Players") is IEnumerable players)
                foreach (var p in players) if (p is UnityEngine.Object o && o != null) list.Add(p);
            return list;
        }
        public bool IsLocal(object player) { RefreshPlayer(); return player != null && ReferenceEquals(player, localPlayer); }
        public string PlayerName(object player)
        {
            string name = GetMember(player, "SteamName") as string;
            return string.IsNullOrEmpty(name) ? (player as Component)?.name ?? "PLAYER" : name;
        }
        public Transform PlayerTransform(object player) => GetMember(player, "Transform") as Transform;
        public int PlayerHealth(object player) => GetMember(GetMember(player, "Vitals"), "Health") is int h ? h : -1;
        public bool PlayerIsHost(object player) => GetMember(player, "IsOwner") is bool && GetMember(GetMember(player, "Owner"), "ClientId") is int id && id == 0;

        public bool TeleportPlayer(object player, Vector3 pos)
        {
            if (player == null) return false;
            if (IsLocal(player)) return Teleport(pos);
            if (!IsHost) return false;
            var owner = GetMember(player, "Owner");
            float rot = PlayerTransform(player) is Transform t ? t.eulerAngles.y : 0f;
            return owner != null && InvokeAction(player, "RPCTeleport", owner, pos, rot);
        }
        public bool TeleportPlayerRotated(object player, Vector3 pos, float rot)
        {
            if (player == null || !IsHost) return false;
            var owner = GetMember(player, "Owner");
            if (IsLocal(player)) { InvokeAction(player, "LocalTeleport", pos, rot, false); return true; }
            return owner != null && InvokeAction(player, "RPCTeleport", owner, pos, rot);
        }
        public bool KnockPlayer(object player, Vector3 force)
        {
            var move = GetMember(player, "Movement");
            if (move == null) return false;
            if (IsLocal(player)) return InvokeAction(move, "Knockback", force);
            if (!IsHost) return false;
            var owner = GetMember(player, "Owner");
            return owner != null && InvokeAction(move, "RPCKnockback", owner, force);
        }
        public bool HealPlayer(object player)
        {
            if (!IsHost) return false;
            var vitals = GetMember(player, "Vitals");
            bool ok = InvokeAction(vitals, "Heal", 100);
            InvokeAction(vitals, "RestoreFullness", 100);
            return ok;
        }
        public bool KillPlayer(object player)
        {
            if (!IsHost) return false;
            var vitals = GetMember(player, "Vitals");
            return InvokeAction(vitals, "TakeDamage", 1000, PlayerTransform(player)?.position ?? Vector3.zero, Vector3.up * 5f, true);
        }
        public bool SetGodLobby(bool on)
        {
            if (!IsHost) return false;
            var manager = FindType("PlayerManager");
            bool current = GetMember(manager, "InGodMode") is bool b && b;
            if (current != on) Invoke(manager, "ToggleGodMode");
            return GetMember(manager, "InGodMode") is bool now && now == on;
        }
        public bool GodLobbyActive => GetMember(FindType("PlayerManager"), "InGodMode") is bool b && b;

        // ---------------- Items ----------------
        public List<KeyValuePair<string, object>> Spawnables()
        {
            var list = new List<KeyValuePair<string, object>>();
            if (GetMember(FindType("GameInfo"), "_nameToSpawnable") is IDictionary dict)
                foreach (DictionaryEntry e in dict) list.Add(new KeyValuePair<string, object>((string)e.Key, e.Value));
            list.Sort((a, b) => string.CompareOrdinal(a.Key, b.Key));
            return list;
        }
        // Prefabs never run Awake, so Item.Fish/Weapon/etc. are null on them; match by component type.
        public IEnumerable<object> SpawnablesWith(string member)
        {
            var type = FindType(member);
            if (type == null) return Enumerable.Empty<object>();
            return Spawnables().Select(p => p.Value).Where(v => v is Component c && c != null && (type.IsInstanceOfType(c) || c.GetComponentInChildren(type, true) != null));
        }
        public object SpawnPrefab(object prefab, Vector3 pos, Quaternion rot, float scale = 1f)
        {
            if (!IsHost || prefab == null) return null;
            var manager = GetMember(FindType("ItemManager"), "Instance");
            var item = Invoke(manager, "SpawnNewItem", prefab, pos, rot);
            if (item is Component c && Mathf.Abs(scale - 1f) > .01f) c.transform.localScale *= scale;
            return item;
        }
        public Vector3 InFront(float distance)
        {
            var look = LookTransform ?? Player;
            if (look == null) return Vector3.zero;
            return look.position + look.forward * distance;
        }
        public object HeldItem() { RefreshPlayer(); return GetMember(GetMember(localPlayer, "Holding"), "HeldItem"); }
        public bool DuplicateHeld()
        {
            var held = HeldItem();
            if (held == null || !(GetMember(held, "ID") is byte id)) return false;
            var prefab = Invoke(FindType("GameInfo"), "GetSpawnable", id);
            return SpawnPrefab(prefab, InFront(1.5f), Quaternion.identity) != null;
        }
        public bool DeleteHeld()
        {
            var held = HeldItem();
            return IsHost && held != null && InvokeAction(held, "DestroyItem", (byte)0, byte.MaxValue);
        }
        public int DeleteNearbyItems(float radius)
        {
            if (!IsHost || Player == null) return 0;
            int n = 0;
            var held = HeldItem();
            foreach (var pair in WorldItems().ToList())
            {
                if (ReferenceEquals(pair.Value, held) || GetMember(pair.Value, "SyncedHolder") != null) continue;
                if ((pair.Key.position - Player.position).sqrMagnitude > radius * radius) continue;
                if (InvokeAction(pair.Value, "DestroyItem", (byte)0, byte.MaxValue)) n++;
            }
            return n;
        }
        private static bool SetSyncVar(object target, string field, object value)
        {
            var sync = target?.GetType().GetField(field, All)?.GetValue(target);
            var prop = sync?.GetType().GetProperty("Value", All);
            if (prop == null || !prop.CanWrite) return false;
            prop.SetValue(sync, value, null);
            return true;
        }
        // Server-synced worth: TotalWorth multiplies by _killScoreMultiplier and _syncedRandomWeight.
        public int MultiplyWorldValues(float valueMultiplier, float weightMultiplier, bool fishOnly)
        {
            if (!IsHost) return 0;
            int n = 0;
            foreach (var pair in WorldItems())
            {
                if (fishOnly && GetMember(pair.Value, "Creature") == null) continue;
                bool ok = false;
                if (valueMultiplier != 1f) ok |= SetSyncVar(pair.Value, "_killScoreMultiplier", valueMultiplier);
                if (weightMultiplier != 1f && GetMember(pair.Value, "RandomizedWeight") is float w)
                    ok |= SetSyncVar(pair.Value, "_syncedRandomWeight", w * weightMultiplier);
                if (ok) n++;
            }
            return n;
        }
        public int ScaleWorldItems(float scale, bool fishOnly)
        {
            int n = 0;
            foreach (var pair in WorldItems())
            {
                if (fishOnly && GetMember(pair.Value, "Creature") == null) continue;
                pair.Key.localScale = Vector3.one * scale; n++;
            }
            return n;
        }
        public IEnumerable<KeyValuePair<Transform, object>> World() => WorldItems();
        public string ItemLabel(object item)
        {
            string name = Invoke(item, "GetName") as string ?? (item as Component)?.name ?? "?";
            int worth = GetMember(item, "TotalWorth") is int w ? w : 0;
            return worth > 0 ? name + " $" + worth : name;
        }
        public int ItemWorth(object item) => GetMember(item, "TotalWorth") is int w ? w : 0;
        public bool IsCreature(object item) => GetMember(item, "Creature") is UnityEngine.Object o && o != null;

        // ---------------- Locations ----------------
        public Vector3? IslandCentre => GetMember(FindType("Island"), "CurIsland") is UnityEngine.Object o && o != null
            ? (Vector3?)(GetMember(FindType("Island"), "IslandPos") is Vector3 v ? v : Vector3.zero) : null;
        public float WaterHeight => GetMember(FindType("WaterManager"), "WaterHeight") is float h ? h : 0f;
        public Vector3? Casino => (GetMember(FindType("LocalCasino"), "Instance") as Component)?.transform.position;
        public Vector3? Shop
        {
            get
            {
                var type = FindType("SellBox");
                var box = type == null ? null : UnityEngine.Object.FindObjectOfType(type) as Component;
                return box != null ? box.transform.position : (Vector3?)null;
            }
        }
        public Vector3? BoatDriverSeat
        {
            get
            {
                var boat = GetMember(FindType("BoatManager"), "Boat");
                return (GetMember(boat, "DriverPos") as Transform)?.position ?? (boat as Component)?.transform.position;
            }
        }
        public List<KeyValuePair<string, Vector3>> Npcs()
        {
            var list = new List<KeyValuePair<string, Vector3>>();
            var type = FindType("NPC");
            if (type == null) return list;
            foreach (var o in UnityEngine.Object.FindObjectsOfType(type))
                if (o is Component c) list.Add(new KeyValuePair<string, Vector3>(c.name.Replace("(Clone)", "").Trim(), c.transform.position));
            return list;
        }
        public Vector3 OpenWater(float distanceFromIsland)
        {
            Vector3 centre = IslandCentre ?? Vector3.zero;
            Vector3 from = Player != null ? Player.position - centre : Vector3.forward;
            from.y = 0f;
            if (from.sqrMagnitude < 1f) from = Vector3.forward;
            Vector3 p = centre + from.normalized * distanceFromIsland;
            p.y = WaterHeight + 1.5f;
            return p;
        }
        // Drops a ray from above to find ground; falls back to +2m.
        public Vector3 Grounded(Vector3 p)
        {
            if (Physics.Raycast(p + Vector3.up * 60f, Vector3.down, out RaycastHit hit, 200f, ~0, QueryTriggerInteraction.Ignore))
                return hit.point + Vector3.up * 1.2f;
            return p + Vector3.up * 2f;
        }

        // ---------------- Boat extras ----------------
        public Rigidbody BoatBody => CurrentBoatBody();
        public Transform BoatVisual => GetMember(GetMember(FindType("BoatManager"), "Boat"), "VisualBoat") as Transform;
        public bool BoatImpulse(Vector3 velocityChange, Vector3 torque)
        {
            if (!IsHost) return false;
            var body = CurrentBoatBody();
            if (body == null) return false;
            body.AddForce(velocityChange, ForceMode.VelocityChange);
            if (torque != Vector3.zero) body.AddTorque(torque, ForceMode.VelocityChange);
            return true;
        }
        public bool MoveBoat(Vector3 pos, Quaternion rot)
        {
            if (!IsHost) return false;
            var manager = GetMember(FindType("BoatManager"), "Instance");
            return InvokeAction(manager, "TryMoveBoat", pos, rot);
        }

        // ---------------- Explosions / aim ----------------
        private object explosivePrefab;
        public bool ExplodeAt(Vector3 pos)
        {
            if (!IsHost) return false;
            if (explosivePrefab == null) explosivePrefab = SpawnablesWith("Explosive").FirstOrDefault();
            var item = SpawnPrefab(explosivePrefab, pos, Quaternion.identity);
            var explosive = GetMember(item, "Explosive");
            RefreshPlayer();
            return explosive != null && InvokeAction(explosive, "ForceExplode", localPlayer, true);
        }
        // Best live creature target seen from `from`, for aimbot and silent aim.
        public Vector3? AimPoint(Vector3 from, Vector3 forward, bool wide, bool predict, float projectileSpeed)
        {
            Vector3? best = null;
            float bestScore = float.MaxValue;
            var gameInfo = FindType("GameInfo");
            int mask = Mask(GetMember(gameInfo, "LevelLayer")) | Mask(GetMember(gameInfo, "BoatLayer"));
            foreach (var pair in WorldItems())
            {
                var creature = GetMember(pair.Value, "Creature");
                if (creature == null || GetMember(creature, "IsDead") is bool dead && dead) continue;
                var rig = GetMember(creature, "Rig") as Rigidbody;
                Vector3 p = rig != null ? rig.worldCenterOfMass : pair.Key.position + Vector3.up;
                if (predict && rig != null) p += rig.linearVelocity * Mathf.Clamp(Vector3.Distance(from, p) / Mathf.Max(projectileSpeed, 1f), 0f, 1.5f);
                Vector3 d = p - from;
                float dist = d.magnitude;
                if (dist < .1f || dist > 80f) continue;
                float align = Vector3.Dot(forward, d / dist);
                if (!wide && align < .5f) continue;
                if (Physics.Linecast(from, p, mask, QueryTriggerInteraction.Ignore)) continue;
                float score = (wide ? 0f : (1f - align) * 3f) + dist / 80f;
                if (score < bestScore) { bestScore = score; best = p; }
            }
            return best;
        }

        public int MoneyValue => GetMember(FindType("MoneyManager"), "Money") is int m ? m : 0;

        // ---------------- Camera ----------------
        public bool SetYaw(float angle)
        {
            RefreshPlayer();
            return InvokeAction(GetMember(localPlayer, "Camera"), "SetRot", angle);
        }
    }
}

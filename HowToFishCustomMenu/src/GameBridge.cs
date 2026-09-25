using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;

namespace HowToFishCustomMenu
{
    // Bound to the public types and members confirmed in this game's Assembly-CSharp.dll.
    // Reflection keeps FishNet and game assemblies out of the mod's compile-time dependency list.
    internal sealed partial class GameBridge
    {
        private static readonly Assembly GameAssembly = AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp");
        private object localPlayer;
        private object movement;
        private float originalWalk, originalSprint;
        private float originalJump;
        private bool speedSaved, jumpSaved;
        private Collider capsuleCollider, footCollider;
        private bool capsuleWasEnabled, footWasEnabled, noClipSaved;
        private Vector3 originalGravity;
        private bool gravitySaved;
        private Rigidbody flyingBoatBody;
        private bool boatGravitySaved, oldBoatGravity;

        private static int Mask(object o) => o is LayerMask lm ? lm.value : o is int i ? i : 0;

        // While the menu is open the player can still walk (classic COD menu feel), but
        // jump (Space = select), mouse look, firing and hotbar scrolling are switched off.
        private static readonly string[] MenuBlockedActions = { "PlayerJump", "PlayerLook", "PlayerLeftClick", "PlayerRightClick", "InventoryScroll", "PlayerCrouch", "PlayerDrop", "PlayerPickUp", "InventoryNone", "ChangeBait" };
        private readonly List<object> pausedActions = new List<object>();
        public void SetGameInputBlocked(bool blocked)
        {
            if (blocked)
            {
                var input = GetMember(FindType("GameInfo"), "Input");
                if (!(GetMember(input, "actions") is System.Collections.IEnumerable actions)) return;
                foreach (var action in actions)
                    if (Array.IndexOf(MenuBlockedActions, GetMember(action, "name") as string) >= 0
                        && GetMember(action, "enabled") is bool on && on) { InvokeAction(action, "Disable"); pausedActions.Add(action); }
            }
            else
            {
                foreach (var action in pausedActions) { try { InvokeAction(action, "Enable"); } catch (Exception) { } }
                pausedActions.Clear();
            }
        }

        private Type FindType(string name) => GameAssembly?.GetType(name, false);
        private static object GetMember(object target, string name)
        {
            if (target == null) return null;
            var type = target as Type ?? target.GetType();
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            return type.GetProperty(name, flags)?.GetValue(target is Type ? null : target, null)
                ?? type.GetField(name, flags)?.GetValue(target is Type ? null : target);
        }
        private static object Invoke(object target, string name, params object[] args)
        {
            if (target == null) return null;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var type = target as Type ?? target.GetType();
            object instance = target is Type ? null : target;
            foreach (var method in type.GetMethods(flags))
            {
                if (method.Name != name) continue;
                var parameters = method.GetParameters();
                if (parameters.Length != args.Length) continue;
                bool compatible = true;
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] != null && !parameters[i].ParameterType.IsInstanceOfType(args[i])) { compatible = false; break; }
                }
                if (compatible) return method.Invoke(instance, args);
            }
            return null;
        }
        private static bool InvokeAction(object target, string name, params object[] args)
        {
            if (target == null) return false;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;
            var type = target as Type ?? target.GetType();
            object instance = target is Type ? null : target;
            foreach (var method in type.GetMethods(flags))
            {
                if (method.Name != name) continue;
                var parameters = method.GetParameters();
                if (parameters.Length != args.Length) continue;
                bool compatible = true;
                for (int i = 0; i < args.Length; i++)
                    if (args[i] != null && !parameters[i].ParameterType.IsInstanceOfType(args[i])) { compatible = false; break; }
                if (!compatible) continue;
                method.Invoke(instance, args);
                return true;
            }
            return false;
        }
        private static object GetIndexedMember(object target, string propertyName, object index)
        {
            if (target == null) return null;
            var property = target.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return property?.GetValue(target, new[] { index });
        }
        private void RefreshPlayer()
        {
            var type = FindType("Player");
            var current = type == null ? null : GetMember(type, "LocalPlayer");
            if (!ReferenceEquals(current, localPlayer))
            {
                RestoreNoClip();
                localPlayer = current;
                movement = GetMember(localPlayer, "Movement");
                speedSaved = jumpSaved = false;
            }
        }
        public Transform Player
        {
            get { RefreshPlayer(); return GetMember(localPlayer, "Transform") as Transform; }
        }
        public Transform LookTransform
        {
            get { RefreshPlayer(); return GetMember(GetMember(localPlayer, "Camera"), "CamTransform") as Transform; }
        }
        public Transform Boat
        {
            get { var type = FindType("BoatManager"); var boat = type == null ? null : GetMember(type, "Boat"); return boat is Component c ? c.transform : null; }
        }
        public bool Ready => Player != null;
        public bool IsHost => GetMember(localPlayer, "IsServerInitialized") is bool host && host;
        public bool IsSolo => IsSoloSession();
        public string Status => Ready ? "Connected to Player.LocalPlayer / Player.Movement" : "Waiting for Player.LocalPlayer";
        public bool JumpPressed
        {
            get
            {
                var gameInfo = FindType("GameInfo");
                var input = gameInfo == null ? null : GetMember(gameInfo, "Input");
                var actions = GetMember(input, "actions");
                var jump = GetIndexedMember(actions, "Item", "PlayerJump");
                return Invoke(jump, "IsPressed") is bool pressed && pressed;
            }
        }

        public bool SetSpeed(float multiplier)
        {
            RefreshPlayer();
            if (movement == null) return false;
            var type = movement.GetType();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var walk = type.GetField("_walkSpeed", flags);
            var sprint = type.GetField("_sprintSpeed", flags);
            if (walk == null || sprint == null) return false;
            if (!speedSaved) { originalWalk = (float)walk.GetValue(movement); originalSprint = (float)sprint.GetValue(movement); speedSaved = true; }
            walk.SetValue(movement, originalWalk * multiplier);
            sprint.SetValue(movement, originalSprint * multiplier);
            return true;
        }
        public bool Teleport(Vector3 position)
        {
            RefreshPlayer();
            if (movement == null) return false;
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            movement.GetType().GetField("_moveInput", flags)?.SetValue(movement, Vector2.zero);
            movement.GetType().GetField("_curVel", flags)?.SetValue(movement, Vector3.zero);
            return InvokeAction(movement, "Teleport", position, true);
        }
        public bool TeleportToNearestPlayer()
        {
            var origin = Player;
            if (origin == null) return false;
            var players = GetMember(FindType("PlayerManager"), "OtherPlayers") as System.Collections.IEnumerable;
            if (players == null) return false;
            Transform nearest = null;
            float best = float.MaxValue;
            foreach (var other in players)
            {
                var target = GetMember(other, "Transform") as Transform;
                if (target == null) continue;
                float distance = (target.position - origin.position).sqrMagnitude;
                if (distance >= best) continue;
                nearest = target;
                best = distance;
            }
            if (nearest == null) return false;
            Vector3 offset = nearest.right * 2f + Vector3.up;
            return Teleport(nearest.position + offset);
        }
        public bool SetJumpMultiplier(float multiplier)
        {
            RefreshPlayer();
            if (movement == null) return false;
            var field = movement.GetType().GetField("_jumpForce", BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) return false;
            if (!jumpSaved) { originalJump = (float)field.GetValue(movement); jumpSaved = true; }
            field.SetValue(movement, originalJump * multiplier);
            return true;
        }
        public bool AirJump()
        {
            RefreshPlayer();
            return movement != null && GetMember(movement, "Grounded") is bool grounded && !grounded && InvokeAction(movement, "Jump");
        }
        public bool LaunchLocal(float upwardForce)
        {
            RefreshPlayer();
            return movement != null && InvokeAction(movement, "Knockback", Vector3.up * upwardForce);
        }
        public int LaunchLobby(float upwardForce)
        {
            if (!IsHost || upwardForce <= 0f) return 0;
            var players = GetMember(FindType("PlayerManager"), "Players") as System.Collections.IEnumerable;
            if (players == null) return 0;
            int launched = 0;
            foreach (var player in players)
            {
                var targetMovement = GetMember(player, "Movement");
                var owner = GetMember(player, "Owner");
                if (targetMovement == null || owner == null) continue;
                if (InvokeAction(targetMovement, "RPCKnockback", owner, Vector3.up * upwardForce)) launched++;
            }
            return launched;
        }
        private void RestoreNoClip()
        {
            if (capsuleCollider != null) capsuleCollider.enabled = capsuleWasEnabled;
            if (footCollider != null) footCollider.enabled = footWasEnabled;
            capsuleCollider = footCollider = null;
            noClipSaved = false;
        }
        public bool SetNoClip(bool enabled)
        {
            RefreshPlayer();
            if (enabled)
            {
                if (movement == null) return false;
                if (!noClipSaved)
                {
                    const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
                    capsuleCollider = movement.GetType().GetField("_col", flags)?.GetValue(movement) as Collider;
                    footCollider = movement.GetType().GetField("_footCol", flags)?.GetValue(movement) as Collider;
                    if (capsuleCollider == null && footCollider == null) return false;
                    capsuleWasEnabled = capsuleCollider != null && capsuleCollider.enabled;
                    footWasEnabled = footCollider != null && footCollider.enabled;
                    noClipSaved = true;
                }
                if (capsuleCollider != null) capsuleCollider.enabled = false;
                if (footCollider != null) footCollider.enabled = false;
                return true;
            }
            RestoreNoClip();
            return true;
        }
        public bool SetMoonGravity(bool enabled)
        {
            if (!IsHost || !IsSoloSession()) return false;
            if (enabled)
            {
                if (!gravitySaved) { originalGravity = Physics.gravity; gravitySaved = true; }
                Physics.gravity = new Vector3(originalGravity.x, -2f, originalGravity.z);
                return true;
            }
            if (gravitySaved) Physics.gravity = originalGravity;
            gravitySaved = false;
            return true;
        }
        public bool InstantCatch()
        {
            RefreshPlayer();
            if (!IsHost) return false;
            var held = GetMember(GetMember(localPlayer, "Holding"), "HeldItem");
            var rod = GetMember(held, "FishingRod");
            var bait = GetMember(rod, "Bait");
            var baitTransform = GetMember(bait, "transform") as Transform;
            if (bait == null || baitTransform == null || GetMember(bait, "ItemOnBait") != null) return false;
            var waterType = FindType("WaterManager");
            float waterHeight = waterType != null && GetMember(waterType, "WaterHeight") is float h ? h : float.MaxValue;
            if (baitTransform.position.y >= waterHeight) return false;
            float time = GetMember(bait, "RandomizedCatchTime") is float t ? t : 0f;
            var timer = bait.GetType().GetProperty("TimeUnderWater", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            var setter = timer?.GetSetMethod(true);
            if (setter == null) return false;
            setter.Invoke(bait, new object[] { time + 1f });
            return true;
        }
        // Typed calls into MoneyManager (server-only: the shared wallet is a host SyncVar).
        public bool SetMoney(int amount)
        {
            if (!IsHost || amount < 0 || global::MoneyManager.Instance == null) return false;
            int current = global::MoneyManager.Money;
            if (amount > current) global::MoneyManager.AddMoney(amount - current, global::Player.LocalPlayer);
            else if (amount < current) global::MoneyManager.RemoveMoney(current - amount, global::Player.LocalPlayer);
            return global::MoneyManager.Money == amount;
        }
        public bool AddMoney(int amount)
        {
            if (!IsHost || amount <= 0 || global::MoneyManager.Instance == null) return false;
            int before = global::MoneyManager.Money;
            global::MoneyManager.AddMoney(amount, global::Player.LocalPlayer);
            return global::MoneyManager.Money > before;
        }
        public bool SpawnItem(string name, int quantity)
        {
            if (!IsHost || string.IsNullOrWhiteSpace(name) || quantity < 1 || quantity > 25) return false;
            var gameInfo = FindType("GameInfo");
            var prefab = gameInfo == null ? null : Invoke(gameInfo, "GetSpawnable", name.Trim());
            if (prefab == null && byte.TryParse(name, out byte id)) prefab = gameInfo == null ? null : Invoke(gameInfo, "GetSpawnable", id);
            if (prefab == null) return false;
            var managerType = FindType("ItemManager");
            var manager = managerType == null ? null : GetMember(managerType, "Instance");
            if (manager == null) return false;
            var origin = LookTransform != null ? LookTransform : Player;
            if (origin == null) return false;
            Vector3 forward = origin.forward;
            Vector3 spawn = origin.position + forward * 2.5f;
            bool spawned = false;
            for (int i = 0; i < quantity; i++)
            {
                Vector3 offset = (i == 0) ? Vector3.zero : new Vector3((i % 5) * 0.45f, 0.2f * (i / 5), (i / 5) * 0.45f);
                spawned |= Invoke(manager, "SpawnNewItem", prefab, spawn + offset, Quaternion.identity) != null;
            }
            return spawned;
        }
        public int SpawnFishRain(int quantity)
        {
            if (!IsHost || quantity < 1 || quantity > 20 || Player == null) return 0;
            var prefabs = GetMember(FindType("GameInfo"), "_nameToSpawnable") as System.Collections.IDictionary;
            var manager = GetMember(FindType("ItemManager"), "Instance");
            if (prefabs == null || manager == null) return 0;
            var fishPrefabs = SpawnablesWith("Fish").ToList();
            if (fishPrefabs.Count == 0) return 0;
            int spawned = 0;
            for (int i = 0; i < quantity; i++)
            {
                var prefab = fishPrefabs[UnityEngine.Random.Range(0, fishPrefabs.Count)];
                var position = Player.position + new Vector3(UnityEngine.Random.Range(-8f, 8f), UnityEngine.Random.Range(14f, 22f), UnityEngine.Random.Range(-8f, 8f));
                var item = Invoke(manager, "SpawnNewItem", prefab, position, Quaternion.identity);
                if (item == null) continue;
                spawned++;
                var rig = GetMember(item, "Rig") as Rigidbody;
                if (rig != null) rig.linearVelocity = new Vector3(UnityEngine.Random.Range(-3f, 3f), -2f, UnityEngine.Random.Range(-3f, 3f));
            }
            return spawned;
        }
        public int FishTornadoTick()
        {
            if (!IsHost || Player == null) return 0;
            int affected = 0;
            Vector3 centre = Player.position;
            foreach (var pair in WorldItems())
            {
                if (GetMember(pair.Value, "Fish") == null) continue;
                var rig = GetMember(pair.Value, "Rig") as Rigidbody;
                if (rig == null || rig.isKinematic) continue;
                Vector3 radial = rig.position - centre;
                radial.y = 0f;
                if (radial.sqrMagnitude > 400f || radial.sqrMagnitude < .1f) continue;
                Vector3 tangent = Vector3.Cross(Vector3.up, radial.normalized);
                rig.AddForce((tangent * 24f - radial * 1.4f + Vector3.up * 18f), ForceMode.Acceleration);
                affected++;
                if (affected >= 40) break;
            }
            return affected;
        }
        public bool SetGodMode(bool enabled)
        {
            if (!IsHost || !IsSoloSession()) return false;
            var manager = FindType("PlayerManager");
            // Game flag affects every server-side player.
            bool current = GetMember(manager, "InGodMode") is bool active && active;
            if (current != enabled) Invoke(manager, "ToggleGodMode");
            return (GetMember(manager, "InGodMode") is bool now) && now == enabled;
        }
        private bool IsSoloSession()
        {
            var players = GetMember(FindType("PlayerManager"), "Players") as System.Collections.ICollection;
            return players != null && players.Count == 1;
        }
        public bool TeleportToIsland(int index)
        {
            if (!IsHost || !IsSoloSession()) return false;
            var islandManager = FindType("IslandManager");
            if (GetMember(islandManager, "IsLoading") is bool loading && loading) return false;
            int total = GetMember(islandManager, "TotalIslands") is int count ? count : 0;
            if (index < 0 || index >= total - 1 || index > byte.MaxValue) return false;
            return InvokeAction(FindType("OnlineIslandManager"), "TpToSpecificIsland", (byte)index);
        }
        public bool AimAtTarget(bool adsOnly, bool smooth, bool wide, bool predict)
        {
            RefreshPlayer();
            if (!Ready) return false;
            var weapon = GetMember(GetMember(GetMember(localPlayer, "Holding"), "HeldItem"), "Weapon");
            if (adsOnly && !(GetMember(weapon, "IsAds") is bool ads && ads)) return false;
            var playerCamera = GetMember(localPlayer, "Camera");
            var cameraTransform = GetMember(playerCamera, "CamTransform") as Transform;
            if (playerCamera == null || cameraTransform == null) return false;
            Transform nearest = null;
            Vector3 aimPoint = Vector3.zero;
            float bestScore = float.MaxValue;
            foreach (var pair in WorldItems())
            {
                var creature = GetMember(pair.Value, "Creature");
                if (creature == null || GetMember(creature, "IsDead") is bool dead && dead) continue;
                var targetTransform = GetMember(creature, "transform") as Transform;
                if (targetTransform == null) continue;
                var rig = GetMember(creature, "Rig") as Rigidbody;
                Vector3 targetPos = rig != null ? rig.worldCenterOfMass : targetTransform.position + Vector3.up;
                if (predict && rig != null)
                {
                    float speed = GetMember(weapon, "_projSpeed") is float s && s > 1f ? s : 60f;
                    targetPos += rig.linearVelocity * Mathf.Clamp(Vector3.Distance(cameraTransform.position, targetPos) / speed, 0f, 1.5f);
                }
                Vector3 delta = targetPos - cameraTransform.position;
                float distance = delta.magnitude;
                if (distance < 0.1f || distance > 60f) continue;
                var gameInfo = FindType("GameInfo");
                int levelMask = Mask(GetMember(gameInfo, "LevelLayer"));
                int boatMask = Mask(GetMember(gameInfo, "BoatLayer"));
                if (Physics.Linecast(cameraTransform.position, targetPos, levelMask | boatMask, QueryTriggerInteraction.Ignore)) continue;
                float alignment = Vector3.Dot(cameraTransform.forward, delta / distance);
                if (!wide && alignment < 0.2f) continue;
                float score = (wide ? 0f : 1f - alignment) + distance / 60f;
                if (score >= bestScore) continue;
                bestScore = score;
                nearest = targetTransform;
                aimPoint = targetPos;
            }
            if (nearest == null) return false;
            Vector3 desired = Quaternion.LookRotation(aimPoint - cameraTransform.position, Vector3.up).eulerAngles;
            var rotation = playerCamera.GetType().GetField("_rot", BindingFlags.Instance | BindingFlags.NonPublic);
            if (rotation == null) return false;
            Vector3 euler = smooth ? new Vector3(
                Mathf.LerpAngle(((Vector3)rotation.GetValue(playerCamera)).x, desired.x, Mathf.Clamp01(Time.deltaTime * 8f)),
                Mathf.LerpAngle(((Vector3)rotation.GetValue(playerCamera)).y, desired.y, Mathf.Clamp01(Time.deltaTime * 8f)), 0f) : desired;
            euler.x = Mathf.Clamp(Mathf.DeltaAngle(0f, euler.x), -89f, 89f);
            rotation.SetValue(playerCamera, euler);
            Invoke(playerCamera, "SetCamPosRot");
            return true;
        }
        public bool Triggerbot(bool adsOnly)
        {
            RefreshPlayer();
            var weapon = GetMember(GetMember(GetMember(localPlayer, "Holding"), "HeldItem"), "Weapon");
            var camera = LookTransform;
            if (weapon == null || camera == null || adsOnly && !(GetMember(weapon, "IsAds") is bool ads && ads)) return false;
            if (!Physics.Raycast(camera.position, camera.forward, out RaycastHit hit, 100f)) return false;
            var itemType = FindType("ItemManager");
            var item = itemType == null ? null : Invoke(itemType, "Get", hit.transform);
            var creature = GetMember(item, "Creature");
            if (creature == null || GetMember(creature, "IsDead") is bool dead && dead) return false;
            return InvokeAction(weapon, "Shoot");
        }
        private Rigidbody CurrentBoatBody()
        {
            var type = FindType("BoatManager");
            var boat = type == null ? null : GetMember(type, "Boat");
            return GetMember(boat, "HiddenPhysicsRig") as Rigidbody;
        }
        public bool ApplyBoatThrust(float multiplier)
        {
            if (!IsHost || multiplier <= 1f) return false;
            var body = CurrentBoatBody();
            if (body == null) return false;
            body.AddForce(body.transform.forward * (multiplier - 1f) * 15f, ForceMode.Acceleration);
            return true;
        }
        public bool ApplyBoatVertical(float amount)
        {
            if (!IsHost) return false;
            var body = CurrentBoatBody();
            if (body == null) return false;
            body.AddForce(Vector3.up * amount, ForceMode.Acceleration);
            return true;
        }
        public bool SetBoatFlying(bool enabled)
        {
            if (!IsHost) return false;
            var body = CurrentBoatBody();
            if (enabled)
            {
                if (body == null) return false;
                if (!boatGravitySaved || flyingBoatBody != body)
                {
                    flyingBoatBody = body;
                    oldBoatGravity = body.useGravity;
                    boatGravitySaved = true;
                }
                body.useGravity = false;
                body.linearVelocity = Vector3.zero;
                return true;
            }
            if (!boatGravitySaved) return true;
            if (flyingBoatBody != null) flyingBoatBody.useGravity = oldBoatGravity;
            flyingBoatBody = null;
            boatGravitySaved = false;
            return true;
        }
        public bool SetRoulette(string colour) => false;
        private IEnumerable<KeyValuePair<Transform, object>> WorldItems()
        {
            var type = FindType("ItemManager");
            var items = type == null ? null : GetMember(type, "Items") as System.Collections.IDictionary;
            if (items == null) yield break;
            foreach (System.Collections.DictionaryEntry pair in items)
                if (pair.Key is Transform transform && transform != null) yield return new KeyValuePair<Transform, object>(transform, pair.Value);
        }
        public IEnumerable<Transform> Fish()
        {
            foreach (var pair in WorldItems()) if (GetMember(pair.Value, "Creature") != null) yield return pair.Key;
        }
        public IEnumerable<Transform> Items()
        {
            foreach (var pair in WorldItems()) yield return pair.Key;
        }
    }
}

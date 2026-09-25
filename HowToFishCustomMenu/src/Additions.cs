using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HowToFishCustomMenu
{
    // Chat kill-feed, seagull army, bosses, auto-sell, kill aura, camera modes,
    // presets, menu sounds and the startup banner.
    public sealed partial class Plugin
    {
        // ---------------- Chat kill-feed ----------------
        // The host sends through the game's own OnlineChatManager ObserversRpc using the lobby
        // id as sender, so every player (modded or not) sees "[Server] KRAKEN // ...".
        private bool killFeed = true;
        private float nextChat;
        private void Announce(string text, bool force = false)
        {
            if (!killFeed && !force) return;
            if (!Host || Time.unscaledTime < nextChat) return;
            nextChat = Time.unscaledTime + .35f; // don't flood the chat
            try
            {
                var chat = global::OnlineChatManager.Instance;
                ulong lobby = global::SteamManager.CurrentLobbyID.m_SteamID;
                if (chat != null) chat.SendChatMessage(lobby, "KRAKEN // " + text);
            }
            catch (Exception ex) { Logger.LogWarning("Chat: " + ex.Message); }
        }
        private void AnnounceOption(Option o)
        {
            if (!killFeed || !Host || o.Kind == OptionKind.Submenu || o.Kind == OptionKind.Input || o.Kind == OptionKind.Label) return;
            string name = System.Text.RegularExpressions.Regex.Replace(o.Display, @"\s*\[.*?\]|\s*\(.*?\)", "").Trim();
            if (Current.Targeted && target != null && o.Kind == OptionKind.Action && !name.StartsWith("TARGET"))
                Announce(Bridge.PlayerName(target) + " GOT " + name.Replace(" PLAYER", "").Replace("PLAYER ", "") + "!");
            else if (o.Kind == OptionKind.Toggle)
                Announce("HOST " + (o.Get() ? "ENABLED " : "DISABLED ") + name);
            else if (o.Kind == OptionKind.Action && Current.Title.Contains("LOBBY"))
                Announce("HOST ACTIVATED " + name);
        }

        // ---------------- Seagull army ----------------
        private int SpawnBirds(Vector3 centre, int count, string kind = "seagull")
        {
            var prefab = Bridge.Spawnables().FirstOrDefault(p => p.Key == kind).Value;
            if (prefab == null) { Note("No " + kind + " prefab"); return 0; }
            int n = 0;
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2f / count;
                var pos = centre + new Vector3(Mathf.Cos(a) * 6f, 8f + (i % 3) * 2f, Mathf.Sin(a) * 6f);
                int k = i;
                spawnQueue.Enqueue(() => { if (Bridge.SpawnPrefab(prefab, pos, Quaternion.Euler(0, k * 30f, 0)) != null) n++; });
            }
            return count;
        }

        // ---------------- Bosses ----------------
        private static readonly KeyValuePair<string, string>[] Bosses =
        {
            new KeyValuePair<string, string>("bowheadwhale", "BOWHEAD WHALE"),
            new KeyValuePair<string, string>("mutatedbowheadwhale", "MUTATED WHALE"),
            new KeyValuePair<string, string>("giantpiranha", "GIANT PIRANHA"),
            new KeyValuePair<string, string>("theoldpike", "THE OLD PIKE"),
            new KeyValuePair<string, string>("goblinshark", "GOBLIN SHARK"),
            new KeyValuePair<string, string>("spidercrab", "SPIDER CRAB"),
        };
        private bool bossRush;
        private int bossRushIndex;
        private float nextBoss;
        private global::Item currentRushBoss;
        private bool SpawnBoss(string key, Vector3 near)
        {
            var prefab = Bridge.Spawnables().FirstOrDefault(p => p.Key == key).Value;
            if (prefab == null) { Note("Boss not found: " + key); return false; }
            // Bosses are sea creatures: put them in the water beside the target.
            var pos = near + new Vector3(UnityEngine.Random.Range(-6f, 6f), 0, UnityEngine.Random.Range(-6f, 6f));
            pos.y = Mathf.Min(pos.y, Bridge.WaterHeight - 1.5f);
            var item = Bridge.SpawnPrefab(prefab, pos, Quaternion.Euler(0, UnityEngine.Random.Range(0, 360f), 0)) as global::Item;
            if (item == null) return false;
            if (bossRush) currentRushBoss = item;
            return true;
        }
        private Vector3 NearestWater(Vector3 from)
        {
            var p = from; p.y = Bridge.WaterHeight - 1.5f;
            if (Bridge.WaterHeight > from.y - 3f) return p;
            return Bridge.OpenWater(Vector3.Distance(from, IslandOrigin) + 15f) + Vector3.down * 3f;
        }
        private void TickBossRush()
        {
            if (!bossRush || !Host || Time.unscaledTime < nextBoss) return;
            bool alive = currentRushBoss != null && !currentRushBoss.IsDestroying && currentRushBoss.Creature != null && !currentRushBoss.Creature.IsDead;
            if (alive) { nextBoss = Time.unscaledTime + 2f; return; }
            if (bossRushIndex >= Bosses.Length) { bossRush = false; Note("BOSS RUSH COMPLETE!"); Announce("BOSS RUSH COMPLETE!", true); return; }
            var boss = Bosses[bossRushIndex++];
            if (SpawnBoss(boss.Key, NearestWater(Bridge.Player.position)))
            {
                ShowBanner("BOSS " + bossRushIndex + "/" + Bosses.Length, boss.Value);
                Announce("BOSS RUSH " + bossRushIndex + "/" + Bosses.Length + ": " + boss.Value + " HAS APPEARED", true);
            }
            nextBoss = Time.unscaledTime + 6f;
        }

        // ---------------- Auto-sell ----------------
        // Uses the same server call the sell box uses, then removes the item.
        private int SellItems(IEnumerable<global::Item> items)
        {
            int n = 0, money = 0;
            foreach (var item in items.ToList())
            {
                if (item == null || item.IsDestroying || item.DeadPlayer || (item.Creature && !item.Creature.IsDead) || item.TotalWorth <= 0) continue;
                money += item.TotalWorth;
                global::MoneyManager.SellItem(item);
                item.DestroyItem((byte)global::DestroyReason.Immediate);
                n++;
            }
            if (n > 0) { Note("Sold " + n + " items for $" + money); Announce("HOST SOLD " + n + " ITEMS FOR $" + money); }
            else Note("Nothing sellable");
            return n;
        }
        private IEnumerable<global::Item> NearbyItems(float radius)
        {
            var me = Bridge.Player.position;
            foreach (var pair in Bridge.World())
                if (pair.Value is global::Item it && it.SyncedHolder == null && (pair.Key.position - me).sqrMagnitude < radius * radius) yield return it;
        }
        private bool autoSellFish;
        private float nextAutoSell;
        private void TickAutoSell()
        {
            if (!autoSellFish || !Host || Time.unscaledTime < nextAutoSell) return;
            nextAutoSell = Time.unscaledTime + 1f;
            var dead = NearbyItems(6f).Where(i => i.Creature != null && i.Creature.IsDead).ToList();
            if (dead.Count > 0) SellItems(dead);
        }

        // ---------------- Kill aura ----------------
        private bool killAura;
        private float auraRange = 6f, nextAura;
        private void TickKillAura()
        {
            if (!killAura || !Bridge.Ready || Time.unscaledTime < nextAura) return;
            nextAura = Time.unscaledTime + .25f;
            var me = global::Player.LocalPlayer;
            if (me == null) return;
            var pos = Bridge.Player.position;
            foreach (var pair in Bridge.World().ToList())
            {
                if (!(pair.Value is global::Item it) || it.Creature == null || it.Creature.IsDead) continue;
                if ((pair.Key.position - pos).sqrMagnitude > auraRange * auraRange) continue;
                var dir = (pair.Key.position - pos).normalized;
                // LocalHit is the game's normal client-side hit path (it forwards to the server).
                it.LocalHit(pair.Key, pair.Key.position, dir, me, 50, false, dir * 5f);
            }
        }

        // ---------------- Camera modes ----------------
        private int camMode; // 0 normal, 1 third person, 2 free cam, 3 spectate
        private Vector3 freePos; private float freeYaw, freePitch, thirdDistance = 4.5f;
        private GameObject standIn;
        private static readonly string[] CamModes = { "FIRST PERSON", "THIRD PERSON", "FREE CAM", "SPECTATE TARGET" };
        private void SetCamMode(int mode)
        {
            camMode = mode;
            Bridge.BlockActions("freecam", new[] { "PlayerMove", "PlayerJump", "PlayerLook", "PlayerCrouch" }, mode == 2);
            var cam = GameCamera;
            if (mode == 2 && cam != null) { freePos = cam.transform.position; freeYaw = cam.transform.eulerAngles.y; freePitch = cam.transform.eulerAngles.x; }
            if (mode == 3 && target == null) Note("Pick a target in PLAYERS first");
            if (mode != 1 && standIn != null) { Destroy(standIn); standIn = null; }
        }
        private void TickCamera()
        {
            if (camMode == 0) return;
            var cam = GameCamera;
            var me = Bridge.Player;
            if (cam == null || me == null) return;
            var t = cam.transform;
            if (camMode == 1)
            {
                // The game deletes your own body model locally, so draw a simple stand-in.
                if (standIn == null) BuildStandIn();
                standIn.transform.SetPositionAndRotation(me.position, Quaternion.Euler(0, t.eulerAngles.y, 0));
                var look = t.rotation;
                var head = me.position + Vector3.up * 1.6f;
                float d = thirdDistance;
                if (Physics.Raycast(head, look * Vector3.back, out var hit, d, global::GameInfo.LevelLayer.value, QueryTriggerInteraction.Ignore)) d = hit.distance - .2f;
                t.position = head + look * new Vector3(.6f, .3f, -d);
            }
            else if (camMode == 2)
            {
                var mouse = Mouse.current;
                if (mouse != null && !shown) { var md = mouse.delta.ReadValue() * .12f; freeYaw += md.x; freePitch = Mathf.Clamp(freePitch - md.y, -89f, 89f); }
                var rot = Quaternion.Euler(freePitch, freeYaw, 0);
                Vector3 mv = Vector3.zero;
                if (!shown)
                {
                    if (Input.GetKey(KeyCode.W)) mv += Vector3.forward; if (Input.GetKey(KeyCode.S)) mv += Vector3.back;
                    if (Input.GetKey(KeyCode.D)) mv += Vector3.right; if (Input.GetKey(KeyCode.A)) mv += Vector3.left;
                    if (Input.GetKey(KeyCode.Space)) mv += Vector3.up; if (Input.GetKey(KeyCode.LeftControl)) mv += Vector3.down;
                }
                freePos += rot * mv * (Input.GetKey(KeyCode.LeftShift) ? 40f : 12f) * Time.unscaledDeltaTime;
                t.SetPositionAndRotation(freePos, rot);
            }
            else if (camMode == 3 && target is global::Player p && p != null && p.CamObject != null)
            {
                t.SetPositionAndRotation(p.CamObject.position, p.CamObject.rotation);
            }
        }
        private void BuildStandIn()
        {
            standIn = new GameObject("KRAKEN_StandIn");
            buildRoot = standIn.transform; buildLayer = 0;
            Part(PrimitiveType.Capsule, new Vector3(0, .9f, 0), new Vector3(.7f, .8f, .7f), Accent, collider: false);
            Part(PrimitiveType.Sphere, new Vector3(0, 1.85f, 0), Vector3.one * .55f, new Color(1f, .85f, .7f), collider: false);
            Part(PrimitiveType.Cube, new Vector3(0, 1.9f, .25f), new Vector3(.4f, .12f, .1f), Color.black, collider: false);
        }

        // ---------------- Presets ----------------
        private ConfigEntry<KeyCode>[] presetKeys;
        private string PresetFile => Path.Combine(Paths.ConfigPath, "kraken_presets.txt");
        private readonly Dictionary<int, List<string>> presets = new Dictionary<int, List<string>>();
        private void BindPresets()
        {
            presetKeys = new[]
            {
                Config.Bind("Keys", "LoadPreset1", KeyCode.F11, "Load preset 1"),
                Config.Bind("Keys", "LoadPreset2", KeyCode.None, "Load preset 2"),
                Config.Bind("Keys", "LoadPreset3", KeyCode.None, "Load preset 3"),
            };
            try
            {
                if (File.Exists(PresetFile))
                    foreach (var line in File.ReadAllLines(PresetFile))
                    {
                        int bar = line.IndexOf('|');
                        if (bar < 1 || !int.TryParse(line.Substring(0, bar), out int slot)) continue;
                        if (!presets.ContainsKey(slot)) presets[slot] = new List<string>();
                        presets[slot].Add(line.Substring(bar + 1));
                    }
            }
            catch (Exception ex) { Logger.LogWarning("Presets: " + ex.Message); }
        }
        // Walk every static menu and collect toggles/sliders by "MENU/ROW" path.
        private IEnumerable<KeyValuePair<string, Option>> PresetOptions()
        {
            var seen = new HashSet<Menu>();
            var todo = new Stack<Menu>(); todo.Push(root);
            while (todo.Count > 0)
            {
                var m = todo.Pop();
                if (!seen.Add(m) || m.Dynamic != null) continue;
                foreach (var o in m.Options)
                {
                    if (o.Kind == OptionKind.Submenu && o.Open != null && !o.Name.Contains("PLAYERS"))
                    {
                        Menu sub = null;
                        try { sub = o.Open(); } catch { }
                        if (sub != null) todo.Push(sub);
                    }
                    else if ((o.Kind == OptionKind.Toggle || o.Kind == OptionKind.Slider) && o.Name != null && !o.Name.Contains("PLAYER") && !o.Name.Contains("FREEZE"))
                        yield return new KeyValuePair<string, Option>(m.Title + "/" + o.Name, o);
                }
            }
        }
        private void SavePreset(int slot)
        {
            var list = new List<string>();
            foreach (var p in PresetOptions())
            {
                if (p.Value.Kind == OptionKind.Toggle && p.Value.Get()) list.Add(p.Key + "=1");
                else if (p.Value.Kind == OptionKind.Slider) list.Add(p.Key + "=" + p.Value.GetValue().ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
            presets[slot] = list;
            try { File.WriteAllLines(PresetFile, presets.SelectMany(kv => kv.Value.Select(v => kv.Key + "|" + v))); } catch (Exception ex) { Logger.LogWarning(ex); }
            Note("PRESET " + slot + " SAVED (" + list.Count(v => v.EndsWith("=1")) + " toggles on)");
        }
        private void LoadPreset(int slot)
        {
            if (!presets.TryGetValue(slot, out var list)) { Note("Preset " + slot + " is empty"); return; }
            var values = list.Select(l => l.Split('=')).Where(a => a.Length == 2).ToDictionary(a => a[0], a => a[1]);
            int on = 0;
            foreach (var p in PresetOptions())
            {
                try
                {
                    if (p.Value.Kind == OptionKind.Toggle)
                    {
                        bool want = values.TryGetValue(p.Key, out var v) && v == "1";
                        if (p.Value.Get() != want && (!want || p.Value.Available())) p.Value.Set(want);
                        if (want) on++;
                    }
                    else if (values.TryGetValue(p.Key, out var s) && float.TryParse(s, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float f))
                        p.Value.SetValue(f);
                }
                catch (Exception ex) { Logger.LogWarning("Preset " + p.Key + ": " + ex.Message); }
            }
            Note("PRESET " + slot + " LOADED (" + on + " toggles on)");
            PlaySound("Click");
        }
        private Menu presetMenu;
        private Menu PresetMenu()
        {
            if (presetMenu != null) return presetMenu;
            var m = presetMenu = new Menu("PRESETS");
            for (int i = 1; i <= 3; i++)
            {
                int slot = i;
                m.Add(new Option { Kind = OptionKind.Action, NameFn = () => "LOAD PRESET " + slot + (presets.ContainsKey(slot) ? "" : " (EMPTY)") + "  [" + (presetKeys[slot - 1].Value == KeyCode.None ? "-" : presetKeys[slot - 1].Value.ToString().ToUpper()) + "]", OnSelect = () => LoadPreset(slot) });
                m.Action("SAVE CURRENT SETUP TO PRESET " + slot, () => SavePreset(slot));
            }
            m.Label("Saves every toggle and slider you have set.");
            return m;
        }

        // ---------------- Menu sounds & startup banner ----------------
        private bool menuSounds = true;
        private void PlaySound(string clip)
        {
            if (!menuSounds) return;
            try { global::AudioManager.PlayGlobalClip(clip, false, .5f, 0f); } catch { }
        }
        private const float BannerTime = 6f;
        private string bannerTitle = "", bannerSub = "";
        private float bannerUntil;
        private bool greeted;
        private float readySince = -1f;
        private void ShowBanner(string title, string sub)
        {
            bannerTitle = title; bannerSub = sub; bannerUntil = Time.unscaledTime + BannerTime;
        }
        private void TickGreeting()
        {
            if (!Bridge.Ready) { greeted = false; readySince = -1f; return; }
            if (greeted) return;
            if (readySince < 0f) readySince = Time.unscaledTime;
            if (Time.unscaledTime - readySince < 2f) return; // let the loading screen clear first
            greeted = true;
            Logger.LogInfo("Startup banner shown");
            ShowBanner("KRAKEN LOADED", "v" + Version + "  //  PRESS " + menuKey.Value.ToString().ToUpper() + " OR LB + D-PAD UP");
            PlaySound("PlayerSpawn");
            if (Host) Announce("v" + Version + " LOADED // WELCOME TO THE MODDED LOBBY", true);
        }
        private void DrawBanner(Color accent)
        {
            if (Time.unscaledTime > bannerUntil) return;
            float remaining = bannerUntil - Time.unscaledTime, alpha = Mathf.Clamp01(remaining) * Mathf.Clamp01((BannerTime - remaining) * 3f);
            var r = new Rect(0, Screen.height * .3f, Screen.width, 130);
            Rect(new Rect(0, r.y, Screen.width, r.height), new Color(0, 0, 0, .55f * alpha));
            Rect(new Rect(0, r.y, Screen.width, 4), new Color(accent.r, accent.g, accent.b, alpha));
            Rect(new Rect(0, r.yMax - 4, Screen.width, 4), new Color(accent.r, accent.g, accent.b, alpha));
            Text(new Rect(0, r.y + 10, Screen.width, 80), bannerTitle, new GUIStyle(centre) { fontSize = 64 }, new Color(1, 1, 1, alpha));
            Text(new Rect(0, r.y + 88, Screen.width, 30), bannerSub, new GUIStyle(small) { fontSize = 18 }, new Color(.75f, .88f, 1f, alpha));
        }

        private void TickAdditions()
        {
            TickGreeting();
            TickBossRush();
            TickAutoSell();
            TickKillAura();
            if (presetKeys != null && editing == null && capturing == null && !ChatOpen())
                for (int i = 0; i < presetKeys.Length; i++)
                    if (presetKeys[i].Value != KeyCode.None && Input.GetKeyDown(presetKeys[i].Value)) LoadPreset(i + 1);
        }

        // ---------------- Menus ----------------
        private Menu bossMenu;
        private Menu BossMenu()
        {
            if (bossMenu != null) return bossMenu;
            var m = bossMenu = new Menu("BOSS SPAWNER");
            foreach (var b in Bosses)
            {
                var boss = b;
                m.Action("SPAWN " + boss.Value, () => { if (SpawnBoss(boss.Key, NearestWater(Bridge.Player.position))) { Note(boss.Value + " SPAWNED"); Announce(boss.Value + " HAS APPEARED!"); } }, IsHostNow, NeedHost);
            }
            m.Action("SPAWN BOSS ON TARGET", () =>
            {
                var t = Bridge.PlayerTransform(target);
                if (t == null) { Note("Pick a target in PLAYERS first"); return; }
                var b = Bosses[UnityEngine.Random.Range(0, Bosses.Length)];
                if (SpawnBoss(b.Key, NearestWater(t.position))) Announce(b.Value + " WAS SENT AFTER " + Bridge.PlayerName(target) + "!");
            }, IsHostNow, NeedHost);
            m.Toggle("BOSS RUSH", () => bossRush, v => { bossRush = v; bossRushIndex = 0; currentRushBoss = null; nextBoss = 0; if (v) Announce("BOSS RUSH STARTED!", true); }, IsHostNow, NeedHost);
            m.Label("Rush: next boss spawns when the last one dies.");
            return m;
        }
        private Menu extrasMenu;
        private Menu ExtrasMenu()
        {
            if (extrasMenu != null) return extrasMenu;
            var m = extrasMenu = new Menu("EXTRAS");
            m.Action("UNLOCK ALL [HOST]", () => Note(UnlockAll()), IsHostNow, NeedHost);
            m.Sub("BOSS SPAWNER", BossMenu);
            m.Action("SEAGULL ARMY ON TARGET", () =>
            {
                var t = Bridge.PlayerTransform(target) ?? Bridge.Player;
                SpawnBirds(t.position, 12); Note("SEAGULL ARMY INCOMING");
                if (target != null) Announce(Bridge.PlayerName(target) + " GOT SEAGULL ARMY!");
            }, IsHostNow, NeedHost);
            m.Action("SEAGULL ARMY ON ME", () => { SpawnBirds(Bridge.Player.position, 12); Note("SEAGULL ARMY"); }, IsHostNow, NeedHost);
            m.Action("ALBATROSS SQUAD ON TARGET", () => { var t = Bridge.PlayerTransform(target) ?? Bridge.Player; SpawnBirds(t.position, 4, "albatross"); }, IsHostNow, NeedHost);
            m.Action("AUTO-SELL HELD ITEM", () => { var held = Bridge.HeldItem() as global::Item; if (held == null) Note("Hold something to sell"); else SellItems(new[] { held }); }, IsHostNow, NeedHost);
            m.Action("AUTO-SELL EVERYTHING NEAR ME (10M)", () => SellItems(NearbyItems(10f)), IsHostNow, NeedHost);
            m.Toggle("AUTO-SELL DEAD FISH NEAR ME", () => autoSellFish, v => autoSellFish = v, IsHostNow, NeedHost);
            m.Toggle("KILL AURA", () => killAura, v => killAura = v, InWorld, NeedPlayer);
            m.Slider("KILL AURA RANGE", () => auraRange, v => auraRange = v, 2f, 20f, 1f, "0m");
            m.Choice("CAMERA", CamModes, () => camMode, SetCamMode);
            m.Slider("THIRD PERSON DISTANCE", () => thirdDistance, v => thirdDistance = v, 2f, 12f, .5f, "0.0m");
            m.Sub("PRESETS", PresetMenu);
            m.Toggle("CHAT KILL-FEED (HOST)", () => killFeed, v => killFeed = v);
            m.Action("SAY KRAKEN IN CHAT", () => Announce("THIS LOBBY IS POWERED BY KRAKEN v" + Version, true), IsHostNow, NeedHost);
            m.Toggle("MENU SOUNDS", () => menuSounds, v => menuSounds = v);
            return m;
        }
    }
}

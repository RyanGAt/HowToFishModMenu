using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace HowToFishCustomMenu
{
    [BepInPlugin("sunshineplunge.howtofish.custommenu", "FISH TOOL - Modded Lobby Edition", "0.2.3")]
    public sealed class Plugin : BaseUnityPlugin
    {
        // Replace with a version-verified integration for How to Fish.
        internal IGameBridge Bridge = new UnboundGameBridge();
        private readonly string[] tabs = { "MAIN MODS", "PLAYER", "FISHING", "ITEMS", "TELEPORT", "CASINO", "ESP", "TROLL", "FUN", "LOBBY", "SETTINGS" };
        private ConfigEntry<KeyCode> menuKey;
        private ConfigEntry<float> flySpeed, boatMultiplier;
        private ConfigEntry<bool> espEnabled;
        private readonly List<Transform> targets = new List<Transform>();
        private Rect window = new Rect(100, 65, 690, 560);
        private GUIStyle title, subtitle, entry, active, info, section;
        private Texture2D black, green, dark, tint;
        private bool shown, fly, boatBoost, god, disco, rainbow, cameraRoll, slowMotion, chaos, miniHud;
        private bool activateSelected;
        private int page, selected, maxSelection;
        private Vector3 savedPosition;
        private bool hasSavedPosition, gravitySaved, oldGravity;
        private Rigidbody playerBody;
        private float nextRefresh, nextChaos, nextColour, roll, originalFov = -1f, originalTimeScale = 1f;
        private Camera trackedCamera;
        private Quaternion cameraOriginal;
        private Color hue = new Color(.18f, 1f, .35f);
        private string message = "FISH TOOL LOADED // INSERT TO TOGGLE", money = "10000", itemName = "", qty = "1", colour = "Green";
        private const float W = 690, H = 560;
        private float y;
        private int row;

        private void Awake()
        {
            menuKey = Config.Bind("Keys", "MenuKey", KeyCode.Insert, "Opens/closes the classic mod menu");
            flySpeed = Config.Bind("Movement", "FlySpeed", 12f, "Fly speed");
            boatMultiplier = Config.Bind("Boat", "SpeedMultiplier", 2.5f, "Boat boost multiplier");
            espEnabled = Config.Bind("ESP", "Enabled", false, "Entity label overlay");
            Logger.LogInfo("FISH TOOL v0.2.3 menu ready. Press " + menuKey.Value);
        }
        private void Update()
        {
            if (Input.GetKeyDown(menuKey.Value)) shown = !shown;
            if (shown)
            {
                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Backspace)) { if (page != 0) { page = 0; selected = 0; } else shown = false; }
                if (Input.GetKeyDown(KeyCode.UpArrow)) selected = Math.Max(0, selected - 1);
                if (Input.GetKeyDown(KeyCode.DownArrow)) selected = Mathf.Min(Math.Max(0, maxSelection - 1), selected + 1);
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) activateSelected = true;
                if (Input.GetKeyDown(KeyCode.LeftArrow) && page > 0) { page--; selected = 0; }
                if (Input.GetKeyDown(KeyCode.RightArrow) && page < tabs.Length - 1) { page++; selected = 0; }
            }
            if (Bridge.Ready && fly) UpdateFlight();
            else if (fly) StopFlight();
            if (boatBoost && Bridge.Ready && Bridge.IsHost && Bridge.Boat != null && Input.GetKey(KeyCode.W))
            {
                var body = Bridge.Boat.GetComponent<Rigidbody>();
                if (body != null) body.AddForce(Bridge.Boat.forward * (boatMultiplier.Value - 1f) * 15f, ForceMode.Acceleration);
            }
            if (espEnabled.Value && Bridge.Ready && Time.unscaledTime > nextRefresh)
            {
                nextRefresh = Time.unscaledTime + .75f;
                targets.Clear(); AddTargets(Bridge.Fish()); AddTargets(Bridge.Items());
            }
            if (rainbow && Time.unscaledTime > nextColour)
            {
                nextColour = Time.unscaledTime + .045f;
                hue = Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * .17f, 1), .82f, 1f);
                if (green != null) { Destroy(green); green = Solid(hue); active.normal.background = green; }
            }
            if (cameraRoll) RollCamera();
            if (chaos && Time.unscaledTime >= nextChaos) TickChaos();
        }
        private void AddTargets(IEnumerable<Transform> items)
        {
            if (items == null) return;
            foreach (var t in items) { if (t != null && targets.Count < 200) targets.Add(t); }
        }
        private void Note(string s) { message = s; Logger.LogInfo(s); }
        private void UpdateFlight()
        {
            var t = Bridge.Player;
            if (t == null) { StopFlight(); return; }
            var rb = t.GetComponent<Rigidbody>();
            if (rb != null)
            {
                if (!gravitySaved || playerBody != rb) { playerBody = rb; oldGravity = rb.useGravity; gravitySaved = true; }
                rb.useGravity = false; rb.velocity = Vector3.zero;
            }
            var cam = Camera.main;
            Vector3 forward = cam != null ? cam.transform.forward : t.forward;
            Vector3 right = cam != null ? cam.transform.right : t.right;
            forward.y = right.y = 0; forward.Normalize(); right.Normalize();
            Vector3 d = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) d += forward;
            if (Input.GetKey(KeyCode.S)) d -= forward;
            if (Input.GetKey(KeyCode.D)) d += right;
            if (Input.GetKey(KeyCode.A)) d -= right;
            if (Input.GetKey(KeyCode.Space)) d += Vector3.up;
            if (Input.GetKey(KeyCode.LeftControl)) d -= Vector3.up;
            t.position += d.normalized * flySpeed.Value * Time.deltaTime;
        }
        private void StopFlight()
        {
            fly = false;
            if (gravitySaved && playerBody != null) playerBody.useGravity = oldGravity;
            gravitySaved = false; playerBody = null;
        }
        private void RollCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            if (cam != trackedCamera) { trackedCamera = cam; cameraOriginal = cam.transform.localRotation; }
            roll = Mathf.Repeat(roll + Time.unscaledDeltaTime * 85f, 360f);
            // Visual-only roll added to the camera each frame. FPS controllers may override it.
            cam.transform.localRotation *= Quaternion.AngleAxis(Time.unscaledDeltaTime * 85f, Vector3.forward);
        }
        private void SetRoll(bool on)
        {
            cameraRoll = on;
            if (!on && trackedCamera != null) trackedCamera.transform.localRotation = cameraOriginal;
            if (!on) trackedCamera = null;
        }
        private void SetSlow(bool on)
        {
            if (on && !slowMotion) originalTimeScale = Time.timeScale;
            slowMotion = on; Time.timeScale = on ? .35f : originalTimeScale;
        }
        private void SetFov(float fov)
        {
            var cam = Camera.main;
            if (cam == null) { Note("Camera not ready"); return; }
            if (originalFov < 0f) originalFov = cam.fieldOfView;
            cam.fieldOfView = fov;
            Note("Camera FOV -> " + fov.ToString("F0"));
        }
        private void ResetFov()
        {
            if (originalFov < 0) return;
            if (Camera.main != null) Camera.main.fieldOfView = originalFov;
            originalFov = -1f;
        }
        private void TickChaos()
        {
            nextChaos = Time.unscaledTime + 10f;
            // Restrict chaotic effects to presentation; never modify other players or the economy.
            int pick = UnityEngine.Random.Range(0, 5);
            switch (pick)
            {
                case 0: rainbow = !rainbow; Note("CHAOS // RAINBOW UI " + (rainbow ? "ON" : "OFF")); break;
                case 1: SetFov(UnityEngine.Random.Range(62f, 115f)); Note("CHAOS // CAMERA ZOOM"); break;
                case 2: SetRoll(!cameraRoll); Note("CHAOS // SPIN CAMERA"); break;
                case 3: disco = !disco; Note("CHAOS // DISCO OVERLAY"); break;
                default: miniHud = !miniHud; Note("CHAOS // HUD SWITCH"); break;
            }
        }
        private void StopChaos() { chaos = false; disco = false; rainbow = false; SetRoll(false); ResetFov(); nextChaos = 0; Note("CHAOS STOPPED"); }
        private void OnDisable() { StopFlight(); StopChaos(); if (slowMotion) SetSlow(false); }
        private void OnDestroy()
        {
            StopFlight(); StopChaos(); if (slowMotion) SetSlow(false);
            if (black != null) Destroy(black); if (green != null) Destroy(green); if (dark != null) Destroy(dark); if (tint != null) Destroy(tint);
        }
        private static Texture2D Solid(Color c) { var t = new Texture2D(1, 1); t.SetPixel(0, 0, c); t.Apply(); return t; }
        private void InitStyle()
        {
            if (black != null) return;
            black = Solid(new Color(.025f, .025f, .025f, .97f));
            green = Solid(hue); dark = Solid(new Color(.12f, .12f, .12f, .94f));
            tint = Solid(new Color(.2f, .9f, .4f, .13f));
            title = new GUIStyle(GUI.skin.label) { fontSize = 29, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            title.normal.textColor = hue;
            subtitle = new GUIStyle(GUI.skin.label) { fontSize = 12, alignment = TextAnchor.MiddleCenter };
            subtitle.normal.textColor = Color.white;
            section = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
            section.normal.textColor = hue;
            entry = new GUIStyle(GUI.skin.button) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(16, 0, 0, 0) };
            entry.normal.background = dark; entry.normal.textColor = Color.white;
            entry.hover.background = green; entry.hover.textColor = Color.black;
            active = new GUIStyle(entry); active.normal.background = green; active.normal.textColor = Color.black;
            info = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            info.normal.textColor = Color.white;
        }
        private void OnGUI()
        {
            if (!shown && !espEnabled.Value && !disco && !miniHud) return;
            InitStyle();
            if (disco && Event.current.type == EventType.Repaint)
            {
                GUI.color = new Color(hue.r, hue.g, hue.b, .12f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), tint);
                GUI.color = Color.white;
            }
            if (espEnabled.Value && Bridge.Ready) DrawEsp();
            if (miniHud) GUI.Label(new Rect(15, 10, 490, 25), "FISH TOOL // " + message, info);
            if (!shown) return;
            if (rainbow) { title.normal.textColor = hue; section.normal.textColor = hue; }
            window = GUI.Window(745323, window, DrawWindow, "", GUIStyle.none);
        }
        private void DrawEsp()
        {
            var cam = Camera.main;
            if (cam == null || Bridge.Player == null) return;
            foreach (var t in targets)
            {
                if (t == null) continue;
                var p = cam.WorldToScreenPoint(t.position + Vector3.up);
                if (p.z <= 0f || p.x < 0 || p.x > Screen.width || p.y < 0 || p.y > Screen.height) continue;
                float distance = Vector3.Distance(Bridge.Player.position, t.position);
                if (distance > 175f) continue;
                GUI.color = Color.cyan;
                GUI.Label(new Rect(p.x - 100, Screen.height - p.y, 200, 26), t.name + " [" + distance.ToString("F0") + "m]");
            }
            GUI.color = Color.white;
        }
        private void DrawWindow(int id)
        {
            GUI.DrawTexture(new Rect(0, 0, W, H), black);
            GUI.DrawTexture(new Rect(0, 0, W, 4), green);
            GUI.Label(new Rect(0, 12, W, 48), "FISH TOOL", title);
            GUI.Label(new Rect(0, 55, W, 20), "HOW TO FISH // MODDED LOBBY EDITION v0.2.3", subtitle);
            GUI.Box(new Rect(22, 88, W - 44, 399), GUIContent.none);
            GUI.BeginGroup(new Rect(36, 97, W - 70, 387));
            y = 0f; row = 0;
            switch (page)
            {
                case 0: Main(); break;
                case 1: Player(); break;
                case 2: Fishing(); break;
                case 3: Items(); break;
                case 4: Teleport(); break;
                case 5: Casino(); break;
                case 6: Esp(); break;
                case 7: Troll(); break;
                case 8: Fun(); break;
                case 9: Lobby(); break;
                case 10: Settings(); break;
            }
            maxSelection = row;
            selected = Mathf.Clamp(selected, 0, Math.Max(0, row - 1));
            if (row == 0) activateSelected = false;
            GUI.EndGroup();
            GUI.Label(new Rect(22, 491, W - 44, 26), message, info);
            GUI.Label(new Rect(22, 523, W - 44, 24), "↑↓ SELECT    ENTER ACTIVATE    ←→ CATEGORY    BACKSPACE BACK    INSERT CLOSE", subtitle);
            GUI.DragWindow(new Rect(0, 0, W, 85));
        }
        private void Heading(string s)
        {
            GUI.Label(new Rect(8, y, 598, 32), s, section); y += 43f;
        }
        private bool Button(string s, bool available = true)
        {
            int index = row++;
            Rect bounds = new Rect(8, y, 595, 30);
            Event current = Event.current;
            // Standard IMGUI buttons retain keyboard focus even after the green row moves.
            // Draw a passive row and handle activation ourselves so Enter always selects
            // the highlighted entry, not whichever button Unity focused previously.
            bool mouseClick = available && current.type == EventType.MouseDown
                && current.button == 0 && bounds.Contains(current.mousePosition);
            if (mouseClick)
            {
                selected = index;
                current.Use();
            }
            bool keyboardClick = index == selected && activateSelected
                && current.type == EventType.Layout;
            if (keyboardClick) activateSelected = false; // Also consume on disabled rows.

            bool on = index == selected;
            bool wasEnabled = GUI.enabled;
            GUI.enabled = available;
            GUI.Box(bounds, (on ? "➤  " : "     ") + s, on ? active : entry);
            GUI.enabled = wasEnabled;
            y += 32f;
            return available && (mouseClick || keyboardClick);
        }
        private void Text(string s) { GUI.Label(new Rect(13, y, 580, 43), s, info); y += 43f; }
        private void Go(int n) { page = n; selected = 0; }
        private void GameAction(string name, Func<bool> callback)
        {
            bool ready = Bridge.Ready && Bridge.IsHost;
            if (!Button(name + (!ready ? "  [NEEDS VERIFIED HOST HOOK]" : ""), ready)) return;
            try { Note(callback() ? name + " executed" : name + ": not bound to this game build"); }
            catch (Exception ex) { Note(name + ": " + ex.Message); }
        }
        private void Main()
        {
            Heading("MAIN MODS");
            for (int i = 1; i < tabs.Length; i++) { int n = i; if (Button(tabs[i] + "    >")) Go(n); }
        }
        private void Player()
        {
            Heading("PLAYER MODS");
            bool ready = Bridge.Ready && Bridge.Player != null;
            if (Button("FLY MODE " + (fly ? "[ON]" : "[OFF]"), ready)) { if (fly) StopFlight(); else fly = true; }
            GameAction("GOD MODE " + (god ? "[ON]" : "[OFF]"), () => { bool next = !god; if (!Bridge.SetGodMode(next)) return false; god = next; return true; });
            Text("Fly: WASD + Space / Left Ctrl. Player hook required.");
            Text("Other-player manipulation is not implemented in the prototype.");
        }
        private void Fishing()
        {
            Heading("FISHING MODS");
            GameAction("INSTANT CATCH", () => Bridge.InstantCatch());
            Text("Auto-fish / fish rain / giant fish need verified game hooks.");
        }
        private void Items()
        {
            Heading("ITEM SPAWNER & MONEY");
            GUI.Label(new Rect(11, y, 125, 28), "Money:", info);
            money = GUI.TextField(new Rect(140, y, 177, 28), money); y += 35;
            GameAction("SET MONEY", () => int.TryParse(money, out int amount) && Bridge.SetMoney(amount));
            GUI.Label(new Rect(11, y, 125, 28), "Item ID:", info);
            itemName = GUI.TextField(new Rect(140, y, 295, 28), itemName); y += 35;
            GUI.Label(new Rect(11, y, 125, 28), "Quantity:", info);
            qty = GUI.TextField(new Rect(140, y, 95, 28), qty); y += 35;
            GameAction("SPAWN ITEM", () => int.TryParse(qty, out int amount) && amount > 0 && Bridge.SpawnItem(itemName, amount));
        }
        private void Teleport()
        {
            Heading("TELEPORT & BOATS");
            bool ready = Bridge.Ready && Bridge.Player != null;
            if (Button("SAVE POSITION", ready)) { savedPosition = Bridge.Player.position; hasSavedPosition = true; Note("Saved position"); }
            if (Button("TELEPORT TO SAVED", ready && hasSavedPosition)) { Bridge.Player.position = savedPosition; Note("Local teleport; network may correct it"); }
            if (Button("BOAT BOOST " + (boatBoost ? "[ON]" : "[OFF]"), ready && Bridge.IsHost && Bridge.Boat != null)) boatBoost = !boatBoost;
            if (Button("BOOST MULTIPLIER " + boatMultiplier.Value.ToString("F1") + "x")) boatMultiplier.Value = boatMultiplier.Value >= 8 ? 1.5f : boatMultiplier.Value + .5f;
        }
        private void Casino()
        {
            Heading("ROULETTE CHEATS");
            if (Button("COLOUR: " + colour)) colour = colour == "Green" ? "Red" : colour == "Red" ? "Black" : "Green";
            GameAction("FORCE NEXT RESULT", () => Bridge.SetRoulette(colour));
            Text("Requires host-side roulette resolution + visual physics integration.");
        }
        private void Esp()
        {
            Heading("ESP MENU");
            if (Button("ITEM / FISH LABELS " + (espEnabled.Value ? "[ON]" : "[OFF]"), Bridge.Ready)) espEnabled.Value = !espEnabled.Value;
            Text("Tracked objects: " + targets.Count + " / 200. Needs game entity hook.");
        }
        private void Troll()
        {
            Heading("TROLL MENU // LOCAL PRANKS");
            if (Button("SPIN MY CAMERA " + (cameraRoll ? "[ON]" : "[OFF]"))) SetRoll(!cameraRoll);
            if (Button("FISH-EYE CAMERA (FOV 115)")) SetFov(115f);
            if (Button("EXTREME ZOOM (FOV 35)")) SetFov(35f);
            if (Button("RESET CAMERA FOV")) ResetFov();
            if (Button("DISCO SCREEN " + (disco ? "[ON]" : "[OFF]"))) disco = !disco;
            Text("Pranks here affect YOUR view only. No remote player griefing hooks.");
        }
        private void Fun()
        {
            Heading("FUN MODS");
            if (Button("RAINBOW MOD MENU " + (rainbow ? "[ON]" : "[OFF]"))) rainbow = !rainbow;
            if (Button("SLOW MOTION (LOCAL) " + (slowMotion ? "[ON]" : "[OFF]"))) SetSlow(!slowMotion);
            if (Button("HUD STATUS " + (miniHud ? "[ON]" : "[OFF]"))) miniHud = !miniHud;
            if (Button("CHAOS MODE " + (chaos ? "[ON]" : "[OFF]"))) { if (chaos) StopChaos(); else { chaos = true; nextChaos = Time.unscaledTime; } }
            if (Button("RESET ALL FUN EFFECTS")) { StopChaos(); if (slowMotion) SetSlow(false); miniHud = false; }
            Text("Chaos randomises local camera, colours and overlays every 10 seconds.");
        }
        private void Lobby()
        {
            Heading("LOBBY MODS");
            Text("Host-only game-wide modes need verified networking hooks.");
            Text("Local effects are already usable from FUN and TROLL.");
            Text("Status: " + Bridge.Status);
        }
        private void Settings()
        {
            Heading("SETTINGS / DEBUG");
            Text("Toggle: " + menuKey.Value + " (editable in BepInEx config)");
            Text("Bridge: " + Bridge.GetType().Name);
            Text("Player detected: " + (Bridge.Player != null) + "    Host: " + Bridge.IsHost);
            if (Button("FLY SPEED " + flySpeed.Value.ToString("F0"))) flySpeed.Value = flySpeed.Value >= 40 ? 5 : flySpeed.Value + 5;
            if (Button("SAVE CONFIG")) { Config.Save(); Note("Saved configuration"); }
            if (Button("BACK TO MAIN")) Go(0);
        }
    }
}

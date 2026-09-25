using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace HowToFishCustomMenu
{
    // Controller navigation, rebindable hotkeys, saved locations and the sky base.
    public sealed partial class Plugin
    {
        // ---------------- Controller ----------------
        // Classic combo: hold LB + D-pad Up to open/close. D-pad scrolls/adjusts, A selects, B goes back.
        private readonly Dictionary<ButtonControl, float> padRepeat = new Dictionary<ButtonControl, float>();
        private static Gamepad Pad => Gamepad.current;
        private bool PadDown(Func<Gamepad, ButtonControl> pick)
        {
            var pad = Pad;
            return pad != null && pick(pad).wasPressedThisFrame;
        }
        private bool PadRepeat(Func<Gamepad, ButtonControl> pick)
        {
            var pad = Pad;
            if (pad == null) return false;
            var b = pick(pad);
            if (b.wasPressedThisFrame) { padRepeat[b] = Time.unscaledTime + .35f; return true; }
            if (b.isPressed && padRepeat.TryGetValue(b, out float at) && Time.unscaledTime >= at) { padRepeat[b] = Time.unscaledTime + .07f; return true; }
            return false;
        }
        private bool PadMenuCombo() => Pad != null && Pad.leftShoulder.isPressed && Pad.dpad.up.wasPressedThisFrame;

        private bool NavUp() => Pressed(KeyCode.UpArrow) || (!PadMenuComboHeld() && PadRepeat(p => p.dpad.up));
        private bool NavDown() => Pressed(KeyCode.DownArrow) || PadRepeat(p => p.dpad.down);
        private bool NavLeft() => Pressed(KeyCode.LeftArrow) || PadRepeat(p => p.dpad.left);
        private bool NavRight() => Pressed(KeyCode.RightArrow) || PadRepeat(p => p.dpad.right);
        private bool NavSelect() => Input.GetKeyDown(KeyCode.Space) || PadDown(p => p.buttonSouth);
        private bool NavBack() => Input.GetKeyDown(KeyCode.Backspace) || PadDown(p => p.buttonEast);
        private bool PadMenuComboHeld() => Pad != null && Pad.leftShoulder.isPressed;

        // ---------------- Keybinds ----------------
        private ConfigEntry<KeyCode> keySave, keyLoad, keySkyBase, keyFly, keyNoClip, keyGod, keyCar;
        private ConfigEntry<KeyCode> capturing;
        private string capturingName;

        private void BindKeys()
        {
            keySave = Config.Bind("Keys", "SaveLocation", KeyCode.F5, "Save your current location to the quick slot");
            keyLoad = Config.Bind("Keys", "LoadLocation", KeyCode.F6, "Teleport to the quick slot location");
            keySkyBase = Config.Bind("Keys", "SkyBase", KeyCode.F7, "Teleport to the sky base (builds it if needed)");
            keyFly = Config.Bind("Keys", "ToggleFly", KeyCode.F8, "Toggle fly mode");
            keyNoClip = Config.Bind("Keys", "ToggleNoClip", KeyCode.F9, "Toggle no clip");
            keyCar = Config.Bind("Keys", "Car", KeyCode.F10, "Get in / out of the KRAKEN water car (spawns it if needed)");
            keyGod = Config.Bind("Keys", "ToggleGodMode", KeyCode.None, "Toggle personal god mode (host)");
            LoadLocations();
        }

        private static bool ChatOpen()
        {
            try { return global::ChatManager.IsTyping; } catch { return false; }
        }

        private void TickHotkeys()
        {
            if (capturing != null)
            {
                if (Input.GetKeyDown(KeyCode.Escape)) { capturing = null; Note("Rebind cancelled"); return; }
                foreach (KeyCode k in Enum.GetValues(typeof(KeyCode)))
                {
                    if (k == KeyCode.None || k >= KeyCode.Mouse0 && k <= KeyCode.Mouse6) continue;
                    if (!Input.GetKeyDown(k)) continue;
                    capturing.Value = k == KeyCode.Backspace ? KeyCode.None : k;
                    Config.Save();
                    Note(capturingName + " -> " + (capturing.Value == KeyCode.None ? "UNBOUND" : capturing.Value.ToString().ToUpper()));
                    capturing = null;
                    break;
                }
                return;
            }
            if (editing != null || ChatOpen() || !Bridge.Ready) return;
            if (Hit(keySave)) SaveQuick();
            if (Hit(keyLoad)) LoadQuick();
            if (Hit(keySkyBase)) GoSkyBase();
            if (Hit(keyFly)) { if (fly) StopFly(); else fly = true; Note("FLY MODE [" + (fly ? "ON" : "OFF") + "]"); }
            if (Hit(keyNoClip)) { if (Bridge.SetNoClip(!noClip)) noClip = !noClip; Note("NO CLIP [" + (noClip ? "ON" : "OFF") + "]"); }
            if (Hit(keyCar)) ToggleCar();
            if (Hit(keyGod)) { if (Host) { Patches.PersonalGod = !Patches.PersonalGod; Note("GOD MODE [" + (Patches.PersonalGod ? "ON" : "OFF") + "]"); } else Note("GOD MODE // " + NeedHost); }
        }
        private static bool Hit(ConfigEntry<KeyCode> k) => k.Value != KeyCode.None && Input.GetKeyDown(k.Value);

        private Menu keybindMenu;
        private Menu KeybindMenu()
        {
            if (keybindMenu != null) return keybindMenu;
            var m = keybindMenu = new Menu("KEYBINDS");
            void Row(string name, Func<ConfigEntry<KeyCode>> entry) => m.Add(new Option
            {
                Kind = OptionKind.Action,
                NameFn = () => name + ":  " + (capturing == entry() ? "PRESS A KEY..." : entry().Value == KeyCode.None ? "UNBOUND" : entry().Value.ToString().ToUpper()),
                OnSelect = () => { capturing = entry(); capturingName = name; Note("Press a key for " + name + " (Backspace = unbind, Esc = cancel)"); }
            });
            Row("OPEN MENU", () => menuKey);
            Row("SAVE LOCATION", () => keySave);
            Row("LOAD LOCATION", () => keyLoad);
            Row("SKY BASE", () => keySkyBase);
            Row("FLY MODE", () => keyFly);
            Row("NO CLIP", () => keyNoClip);
            Row("WATER CAR", () => keyCar);
            Row("GOD MODE", () => keyGod);
            m.Label("Controller: LB + D-PAD UP opens the menu");
            m.Footer = "SPACE ON A ROW, THEN PRESS THE NEW KEY";
            return m;
        }

        // ---------------- Saved locations ----------------
        // Stored relative to the current island centre so they survive island reloads.
        private readonly List<KeyValuePair<string, Vector3>> locations = new List<KeyValuePair<string, Vector3>>();
        private Vector3? quickLocation;
        private string LocationsFile => Path.Combine(Paths.ConfigPath, "kraken_locations.txt");
        private Vector3 IslandOrigin => Bridge.IslandCentre ?? Vector3.zero;

        private void LoadLocations()
        {
            try
            {
                if (!File.Exists(LocationsFile)) return;
                foreach (var line in File.ReadAllLines(LocationsFile))
                {
                    var p = line.Split('|');
                    if (p.Length != 4) continue;
                    var v = new Vector3(float.Parse(p[1], System.Globalization.CultureInfo.InvariantCulture), float.Parse(p[2], System.Globalization.CultureInfo.InvariantCulture), float.Parse(p[3], System.Globalization.CultureInfo.InvariantCulture));
                    if (p[0] == "QUICK") quickLocation = v; else locations.Add(new KeyValuePair<string, Vector3>(p[0], v));
                }
            }
            catch (Exception ex) { Logger.LogWarning("Locations: " + ex.Message); }
        }
        private void SaveLocations()
        {
            try
            {
                string F(float f) => f.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
                var lines = new List<string>();
                if (quickLocation.HasValue) lines.Add("QUICK|" + F(quickLocation.Value.x) + "|" + F(quickLocation.Value.y) + "|" + F(quickLocation.Value.z));
                foreach (var l in locations) lines.Add(l.Key.Replace("|", "") + "|" + F(l.Value.x) + "|" + F(l.Value.y) + "|" + F(l.Value.z));
                File.WriteAllLines(LocationsFile, lines);
            }
            catch (Exception ex) { Logger.LogWarning("Locations: " + ex.Message); }
        }
        private void SaveQuick()
        {
            if (Bridge.Player == null) return;
            quickLocation = Bridge.Player.position - IslandOrigin;
            SaveLocations();
            Note("LOCATION SAVED  [" + keyLoad.Value.ToString().ToUpper() + " TO LOAD]");
        }
        private void LoadQuick()
        {
            if (!quickLocation.HasValue) { Note("No saved location yet"); return; }
            Go(IslandOrigin + quickLocation.Value, "Saved location");
        }
        private void AddLocation()
        {
            if (Bridge.Player == null) return;
            string name = "LOCATION " + (locations.Count + 1);
            locations.Add(new KeyValuePair<string, Vector3>(name, Bridge.Player.position - IslandOrigin));
            SaveLocations();
            Note(name + " saved");
        }
        private Menu LocationsMenu()
        {
            var m = new Menu("SAVED LOCATIONS");
            m.Dynamic = () =>
            {
                var rows = new List<Option>
                {
                    new Option { Kind = OptionKind.Action, NameFn = () => "SAVE LOCATION (QUICK)  [" + keySave.Value.ToString().ToUpper() + "]", OnSelect = SaveQuick },
                    new Option { Kind = OptionKind.Action, NameFn = () => "LOAD LOCATION (QUICK)  [" + keyLoad.Value.ToString().ToUpper() + "]", OnSelect = LoadQuick },
                    new Option { Kind = OptionKind.Action, Name = "ADD NEW SAVED LOCATION", OnSelect = AddLocation },
                };
                for (int i = 0; i < locations.Count; i++)
                {
                    var l = locations[i];
                    rows.Add(new Option { Kind = OptionKind.Action, Name = "GO TO " + l.Key, OnSelect = () => Go(IslandOrigin + l.Value, l.Key) });
                }
                if (locations.Count > 0)
                    rows.Add(new Option { Kind = OptionKind.Action, Name = "DELETE LAST LOCATION", OnSelect = () => { locations.RemoveAt(locations.Count - 1); SaveLocations(); Note("Deleted"); } });
                return rows;
            };
            m.Footer = "SAVED TO BepInEx/config/kraken_locations.txt";
            return m;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace HowToFishCustomMenu
{
    [BepInPlugin("sunshineplunge.howtofish.custommenu", "KRAKEN - Modded Lobby Edition", Version)]
    public sealed partial class Plugin : BaseUnityPlugin
    {
        public const string Version = "1.2.0";
        internal GameBridge Bridge;
        private Harmony harmony;
        private ConfigEntry<KeyCode> menuKey;
        private ConfigEntry<float> flySpeed;

        // Menu navigation
        private readonly Stack<Menu> stack = new Stack<Menu>();
        private Menu root;
        private bool shown;
        private Option editing; // input row currently capturing text
        private float repeatAt;
        private KeyCode repeatKey;

        // Feed (bottom-left, like the classic iPrintln feed)
        private readonly List<KeyValuePair<string, float>> feed = new List<KeyValuePair<string, float>>();

        private void Awake()
        {
            menuKey = Config.Bind("Keys", "MenuKey", KeyCode.Insert, "Opens/closes the mod menu");
            flySpeed = Config.Bind("Movement", "FlySpeed", 14f, "Fly speed");
            BindKeys();
            BindPresets();
            Bridge = new GameBridge();
            Patches.Bridge = Bridge;
            Patches.SilentAimTarget = from =>
            {
                var look = Bridge.LookTransform;
                return look == null ? null : Bridge.AimTarget(look.position, look.forward, aimWide, aimPrediction, 80f);
            };
            harmony = new Harmony("sunshineplunge.howtofish.custommenu");
            Patches.Apply(harmony, Logger);
            root = BuildMenus();
            Logger.LogInfo("KRAKEN v" + Version + " ready. Press " + menuKey.Value);
            Note("KRAKEN v" + Version + " // PRESS " + menuKey.Value.ToString().ToUpper());
        }

        internal void Note(string s)
        {
            feed.Add(new KeyValuePair<string, float>(s, Time.unscaledTime + 5f));
            if (feed.Count > 6) feed.RemoveAt(0);
            Logger.LogInfo(s);
        }
        private void Result(string what, bool ok, string fail = "unavailable here")
            => Note(ok ? what : what + " // " + fail);

        // ---------------- Navigation ----------------
        private Menu Current => stack.Count > 0 ? stack.Peek() : root;
        private void SetOpen(bool open)
        {
            if (shown == open) return;
            shown = open;
            editing = null;
            Bridge.SetGameInputBlocked(open);
        }
        private bool Pressed(KeyCode key)
        {
            if (Input.GetKeyDown(key)) { repeatKey = key; repeatAt = Time.unscaledTime + .35f; return true; }
            if (repeatKey == key && Input.GetKey(key) && Time.unscaledTime >= repeatAt) { repeatAt = Time.unscaledTime + .06f; return true; }
            return false;
        }
        private void Navigate()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) { SetOpen(false); return; }
            var menu = Current;
            var rows = menu.Rows;
            if (editing != null)
            {
                string text = editing.GetText();
                foreach (char c in Input.inputString)
                {
                    if (c == '\b') { if (text.Length > 0) text = text.Substring(0, text.Length - 1); }
                    else if (c == '\n' || c == '\r') { editing = null; break; }
                    else if (!char.IsControl(c) && text.Length < 32) text += c;
                }
                if (editing != null) editing.SetText(text);
                return;
            }
            if (capturing != null) return;
            if (rows.Count == 0) { if (NavBack()) Back(); return; }
            bool up = NavUp();
            if (up) { menu.Selected = (menu.Selected - 1 + rows.Count) % rows.Count; PlaySound("Hover"); }
            if (NavDown()) { menu.Selected = (menu.Selected + 1) % rows.Count; PlaySound("Hover"); }
            menu.Selected = Mathf.Clamp(menu.Selected, 0, rows.Count - 1);
            // Skip non-interactive labels in the direction of travel.
            for (int guard = 0; guard < rows.Count && rows[menu.Selected].Kind == OptionKind.Label; guard++)
                menu.Selected = (menu.Selected + (up ? rows.Count - 1 : 1)) % rows.Count;
            var o = rows[menu.Selected];
            if (NavLeft()) Adjust(o, -1);
            if (NavRight()) Adjust(o, 1);
            if (NavSelect()) { PlaySound("Click"); Activate(o); }
            if (NavBack()) { if (stack.Count == 0 && PadDown(p => p.buttonEast)) SetOpen(false); else Back(); }
        }
        private void Back() { if (stack.Count > 0) stack.Pop(); }
        private void Adjust(Option o, int dir)
        {
            if (o.Kind != OptionKind.Slider && o.Kind != OptionKind.Choice) return;
            float v = o.GetValue() + o.Step * dir;
            if (o.Kind == OptionKind.Choice) v = (v + o.Choices.Length) % o.Choices.Length;
            else v = Mathf.Clamp(Mathf.Round(v / o.Step) * o.Step, o.Min, o.Max);
            o.SetValue(v);
        }
        private void Activate(Option o)
        {
            if (!o.Available()) { Note(o.Name + " // " + (string.IsNullOrEmpty(o.Requirement) ? "UNAVAILABLE" : o.Requirement)); return; }
            try
            {
                switch (o.Kind)
                {
                    case OptionKind.Action: o.OnSelect(); break;
                    case OptionKind.Toggle: o.Set(!o.Get()); Note(o.Name + " [" + (o.Get() ? "ON" : "OFF") + "]"); break;
                    case OptionKind.Slider:
                    case OptionKind.Choice: Adjust(o, 1); break;
                    case OptionKind.Submenu:
                        var next = o.Open();
                        if (next != null) { next.Selected = 0; next.Scroll = 0; stack.Push(next); }
                        break;
                    case OptionKind.Input: editing = o; break;
                }
                AnnounceOption(o);
            }
            catch (Exception ex) { Note(o.Name + " failed: " + ex.GetBaseException().Message); Logger.LogError(ex); }
        }

        // ---------------- Frame loop ----------------
        private void Update()
        {
            if (capturing == null && (Input.GetKeyDown(menuKey.Value) || PadMenuCombo())) SetOpen(!shown);
            try { TickHotkeys(); } catch (Exception ex) { Logger.LogWarning("Hotkeys: " + ex.Message); }
            if (shown)
            {
                Navigate();
            }
            try { TickFeatures(); }
            catch (Exception ex) { Logger.LogWarning("Tick: " + ex.GetBaseException().Message + " @ " + ex.StackTrace); }
            try { TickSkySign(); } catch { }
            try { TickAdditions(); } catch (Exception ex) { Logger.LogWarning("Extras: " + ex.GetBaseException().Message); }
            try { TickRace(); } catch { }
            try { TickCar(); } catch (Exception ex) { Logger.LogWarning("Car: " + ex.GetBaseException().Message); }
            // ESP refreshes on its own so a failure elsewhere can't blank it.
            try { if (Bridge.Ready && Time.unscaledTime >= nextEsp && (espFish || espItems || espPlayers || espValuable)) RefreshEsp(); }
            catch (Exception ex) { Logger.LogWarning("ESP: " + ex.GetBaseException().Message); }
            feed.RemoveAll(f => f.Value < Time.unscaledTime);
        }
        private void LateUpdate()
        {
            try { TickLate(); }
            catch (Exception ex) { Logger.LogWarning("LateTick: " + ex.GetBaseException().Message); }
        }
        private void OnDestroy()
        {
            SetOpen(false);
            ResetEverything();
            harmony?.UnpatchSelf();
            DestroyTextures();
        }
    }
}

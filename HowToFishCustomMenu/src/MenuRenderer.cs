using System;
using System.Collections.Generic;
using UnityEngine;

namespace HowToFishCustomMenu
{
    // Old-school Xbox 360 COD style: dark translucent column, bright blue header and
    // selection bar, value tags on the right, scroll bar, feed bottom-left.
    public sealed partial class Plugin
    {
        private readonly string[] accentNames = { "COD BLUE", "CYAN", "GREEN", "RED", "PURPLE", "ORANGE", "PINK" };
        private readonly Color[] accentColours =
        {
            new Color(.1f, .45f, 1f), new Color(0f, .85f, 1f), new Color(.2f, 1f, .3f), new Color(1f, .2f, .2f),
            new Color(.65f, .3f, 1f), new Color(1f, .55f, .1f), new Color(1f, .35f, .8f)
        };
        private int accentIndex, menuPos;
        private float menuScale = 1f;
        private const int VisibleRows = 14;
        private const float MenuW = 360f, HeaderH = 86f, TitleH = 30f, RowH = 27f, FooterH = 40f;

        private Texture2D white;
        private GUIStyle big, small, rowStyle, valueStyle, centre, feedStyle, espStyle;

        private void InitStyles()
        {
            if (white != null) return;
            white = new Texture2D(1, 1); white.SetPixel(0, 0, Color.white); white.Apply();
            big = new GUIStyle(GUI.skin.label) { fontSize = 38, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            small = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            rowStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, clipping = TextClipping.Clip, wordWrap = false };
            valueStyle = new GUIStyle(rowStyle) { alignment = TextAnchor.MiddleRight };
            centre = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            feedStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
            espStyle = new GUIStyle(GUI.skin.label) { fontSize = 12, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter };
        }
        private void DestroyTextures() { if (white != null) Destroy(white); }

        private void Rect(Rect r, Color c) { var old = GUI.color; GUI.color = c; GUI.DrawTexture(r, white); GUI.color = old; }
        private void Text(Rect r, string s, GUIStyle st, Color c, bool shadow = true)
        {
            if (shadow) { st.normal.textColor = new Color(0, 0, 0, .85f); GUI.Label(new Rect(r.x + 1.5f, r.y + 1.5f, r.width, r.height), s, st); }
            st.normal.textColor = c; GUI.Label(r, s, st);
        }

        private void OnGUI()
        {
            InitStyles();
            var accent = Accent;
            if (disco && Event.current.type == EventType.Repaint)
                Rect(new Rect(0, 0, Screen.width, Screen.height), new Color(Mathf.Abs(Mathf.Sin(Time.time * 3f)), Mathf.Abs(Mathf.Sin(Time.time * 2.1f)), Mathf.Abs(Mathf.Sin(Time.time * 1.3f)), .18f));
            if (Bridge.Ready && espTargets.Count > 0 && (espFish || espItems || espPlayers || espValuable)) DrawEsp();
            DrawFeed(accent);
            DrawSpeedo(accent);
            if (Time.unscaledTime < chaosBannerUntil)
            {
                Text(new Rect(0, Screen.height * .18f, Screen.width, 90), chaosEvent, centre, accent);
            }
            if (!shown)
            {
                Text(new Rect(10, 6, 400, 22), "KRAKEN v" + Version + "  [" + menuKey.Value.ToString().ToUpper() + "]", new GUIStyle(small) { alignment = TextAnchor.MiddleLeft }, new Color(1, 1, 1, .55f));
                return;
            }
            var saved = GUI.matrix;
            float scale = menuScale * Mathf.Max(1f, Screen.height / 1080f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            DrawMenu(accent, scale);
            GUI.matrix = saved;
        }

        private void DrawMenu(Color accent, float scale)
        {
            float sw = Screen.width / scale, sh = Screen.height / scale;
            var menu = Current;
            var rows = menu.Rows;
            int visible = Mathf.Min(VisibleRows, Mathf.Max(1, rows.Count));
            float h = HeaderH + TitleH + visible * RowH + FooterH + 8f;
            float x = menuPos == 0 ? sw - MenuW - 40f : menuPos == 1 ? 40f : (sw - MenuW) / 2f;
            float y = Mathf.Max(20f, (sh - h) / 2f - 40f);

            // Body + side rails
            Rect(new Rect(x, y, MenuW, h), new Color(0f, 0f, 0f, .78f));
            Rect(new Rect(x - 3, y, 3, h), accent);
            Rect(new Rect(x + MenuW, y, 3, h), accent);

            // Header banner
            Rect(new Rect(x, y, MenuW, HeaderH), new Color(accent.r * .35f, accent.g * .35f, accent.b * .35f, .95f));
            Rect(new Rect(x, y + HeaderH - 4, MenuW, 4), accent);
            Text(new Rect(x, y + 6, MenuW, 50), "KRAKEN", big, Color.white);
            Text(new Rect(x, y + 54, MenuW, 20), "v" + Version + "  //  MODDED LOBBY EDITION", small, Color.Lerp(accent, Color.white, .6f));

            // Current page title
            float ty = y + HeaderH;
            Text(new Rect(x, ty, MenuW, TitleH), menu.Title, new GUIStyle(small) { fontSize = 15 }, Color.white);
            Rect(new Rect(x + 20, ty + TitleH - 2, MenuW - 40, 1), new Color(accent.r, accent.g, accent.b, .6f));

            // Rows with scroll window
            if (menu.Selected < menu.Scroll) menu.Scroll = menu.Selected;
            if (menu.Selected >= menu.Scroll + visible) menu.Scroll = menu.Selected - visible + 1;
            menu.Scroll = Mathf.Clamp(menu.Scroll, 0, Mathf.Max(0, rows.Count - visible));
            float ry = ty + TitleH + 4f;
            for (int i = menu.Scroll; i < Mathf.Min(rows.Count, menu.Scroll + visible); i++)
            {
                var o = rows[i];
                var r = new Rect(x + 6, ry, MenuW - 20, RowH - 2);
                bool sel = i == menu.Selected;
                bool avail = o.Kind == OptionKind.Label || SafeAvailable(o);
                if (sel)
                {
                    Rect(r, accent);
                    Rect(new Rect(r.x, r.y, 4, r.height), Color.white);
                }
                Color textCol = o.Kind == OptionKind.Label ? new Color(.7f, .7f, .7f) : !avail ? new Color(.55f, .55f, .6f) : sel ? Color.white : new Color(.92f, .92f, .92f);
                string name = editing == o ? o.Display + "  _" : o.Display;
                Text(new Rect(r.x + 12, r.y, r.width - 110, r.height), name, rowStyle, textCol);
                string val = o.Value();
                if (!string.IsNullOrEmpty(val))
                {
                    Color vc = o.Kind == OptionKind.Toggle ? (o.Get() ? (sel ? Color.white : new Color(.3f, 1f, .4f)) : (sel ? new Color(1, 1, 1, .7f) : new Color(1f, .35f, .35f))) : (sel ? Color.white : accent);
                    if (o.Kind == OptionKind.Input && editing == o) vc = Color.yellow;
                    Text(new Rect(r.x + r.width - 170, r.y, 162, r.height), val, valueStyle, vc);
                }
                ry += RowH;
            }
            // Scroll bar
            if (rows.Count > visible)
            {
                float trackY = ty + TitleH + 4f, trackH = visible * RowH;
                Rect(new Rect(x + MenuW - 10, trackY, 4, trackH), new Color(1, 1, 1, .12f));
                float thumbH = Mathf.Max(18f, trackH * visible / rows.Count);
                float thumbY = trackY + (trackH - thumbH) * menu.Scroll / Mathf.Max(1, rows.Count - visible);
                Rect(new Rect(x + MenuW - 10, thumbY, 4, thumbH), accent);
            }
            // Footer
            float fy = y + h - FooterH;
            Rect(new Rect(x, fy, MenuW, FooterH), new Color(accent.r * .35f, accent.g * .35f, accent.b * .35f, .95f));
            string footer = !string.IsNullOrEmpty(menu.Footer) ? menu.Footer
                : editing != null ? "TYPE  //  ENTER WHEN DONE"
                : "↑↓ SCROLL  SPACE SELECT  ←→ ADJUST  BKSP BACK";
            Text(new Rect(x, fy + 2, MenuW, 18), footer, small, Color.white);
            Text(new Rect(x, fy + 19, MenuW, 18), (rows.Count > 0 ? (menu.Selected + 1) + " / " + rows.Count : "") + "   v" + Version, small, Color.Lerp(accent, Color.white, .6f));
        }
        private bool SafeAvailable(Option o) { try { return o.Available(); } catch { return false; } }

        private void DrawFeed(Color accent)
        {
            float y = Screen.height - 60f;
            for (int i = feed.Count - 1; i >= 0; i--)
            {
                float alpha = Mathf.Clamp01(feed[i].Value - Time.unscaledTime);
                Text(new Rect(24, y, 800, 22), feed[i].Key, feedStyle, new Color(1, 1, 1, alpha));
                Rect(new Rect(14, y + 4, 4, 14), new Color(accent.r, accent.g, accent.b, alpha));
                y -= 22f;
            }
        }

        private void DrawEsp()
        {
            var cam = GameCamera;
            var me = Bridge.Player;
            if (cam == null || me == null) return;
            foreach (var pair in espTargets)
            {
                var t = pair.Key;
                if (t == null) continue;
                float dist = Vector3.Distance(me.position, t.position);
                if (dist > espRange) continue;
                bool isPlayer = pair.Value.StartsWith("P|");
                Vector3 head = ToScreen(cam, t.position + Vector3.up * (isPlayer ? 2f : .6f));
                Vector3 foot = ToScreen(cam, t.position);
                if (head.z <= 0f) continue;
                char kind = pair.Value[0];
                Color c = espRainbow ? Color.HSVToRGB(Mathf.Repeat(Time.time * .3f + dist * .01f, 1f), 1f, 1f)
                    : kind == 'P' ? new Color(1f, .3f, .3f) : kind == 'F' ? new Color(.2f, .9f, 1f) : kind == 'V' ? new Color(1f, .85f, .1f) : new Color(.6f, 1f, .5f);
                string label = pair.Value.Substring(2) + (espDistance ? " [" + dist.ToString("0") + "m]" : "");
                Text(new Rect(head.x - 120, head.y - 18, 240, 18), label, espStyle, c);
                if (espBoxes)
                {
                    float bh = Mathf.Max(8f, Mathf.Abs(foot.y - head.y)), bw = bh * (isPlayer ? .5f : .9f);
                    var b = new Rect(head.x - bw / 2, head.y, bw, bh);
                    Rect(new Rect(b.x, b.y, b.width, 1.5f), c); Rect(new Rect(b.x, b.yMax, b.width, 1.5f), c);
                    Rect(new Rect(b.x, b.y, 1.5f, b.height), c); Rect(new Rect(b.xMax, b.y, 1.5f, b.height), c);
                }
                if (espTracers && foot.z > 0f) Line(new Vector2(Screen.width / 2f, tracerOrigin == 0 ? Screen.height : tracerOrigin == 1 ? Screen.height / 2f : 0f), new Vector2(foot.z > 0 ? foot.x : head.x, foot.z > 0 ? foot.y : head.y), c);
            }
        }
        // Viewport coords work even when the game camera renders into a low-res target.
        private static Vector3 ToScreen(Camera cam, Vector3 world)
        {
            Vector3 v = cam.WorldToViewportPoint(world);
            return new Vector3(v.x * Screen.width, (1f - v.y) * Screen.height, v.z);
        }
        private Camera GameCamera
        {
            get
            {
                var cam = global::GameInfo.CurCamera;
                if (cam == null && global::Player.LocalPlayer != null) cam = global::Player.LocalPlayer.CurCam;
                return cam != null ? cam : Camera.main;
            }
        }
        private void Line(Vector2 a, Vector2 b, Color c)
        {
            var m = GUI.matrix;
            float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            GUIUtility.RotateAroundPivot(angle, a);
            Rect(new Rect(a.x, a.y, (b - a).magnitude, 1.5f), c);
            GUI.matrix = m;
        }
    }
}

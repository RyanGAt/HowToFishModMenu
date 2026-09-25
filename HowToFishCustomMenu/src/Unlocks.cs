using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HowToFishCustomMenu
{
    // Unlock all (host save progression), per-island teleport buttons and the car race track.
    public sealed partial class Plugin
    {
        // ---------------- Unlock all ----------------
        private string UnlockAll()
        {
            if (!Host) return "host only";
            var done = new List<string>();
            try { var isl = global::OnlineIslandManager.Instance; if (isl != null) { isl._maxIslandUnlocked.Value = byte.MaxValue; done.Add("islands"); } } catch (Exception ex) { Logger.LogWarning(ex); }
            try { var boat = global::BoatManager.Boat; if (boat != null) { boat.UnlockBoat(); boat.UnlockBoatRadar(); done.Add("boat + radar"); } } catch (Exception ex) { Logger.LogWarning(ex); }
            try { global::NPCManager.UnlockGrill(); done.Add("grill"); } catch (Exception ex) { Logger.LogWarning(ex); }
            try
            {
                foreach (var p in global::PlayerManager.Players)
                {
                    var inv = p.Inventory;
                    if (inv == null) continue;
                    var costs = typeof(global::PlayerInventory).GetField("_extraSlotCosts", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(inv) as int[];
                    inv.UnlockExtraPocket((byte)(costs?.Length ?? 3));
                    int baits = global::GameInfo.AllBaits?.Count ?? 0;
                    for (byte b = 1; b < baits; b++) for (int k = 0; k < 50; k++) inv.ServerBoughtBait(b);
                }
                done.Add("pockets + 50 of every bait");
            }
            catch (Exception ex) { Logger.LogWarning(ex); }
            return done.Count == 0 ? "nothing to unlock here" : "Unlocked: " + string.Join(", ", done);
        }

        // ---------------- Islands ----------------
        private static string IslandName(int index)
        {
            // Islands are scenes after the menu scene (IslandBuildOffset = 1); their file names are readable.
            string path = SceneUtility.GetScenePathByBuildIndex(index + 1);
            string name = string.IsNullOrEmpty(path) ? "ISLAND " + (index + 1) : System.IO.Path.GetFileNameWithoutExtension(path);
            return System.Text.RegularExpressions.Regex.Replace(name, "([a-z])([A-Z0-9])", "$1 $2").ToUpperInvariant();
        }
        private Menu IslandMenu()
        {
            var m = new Menu("ISLAND TELEPORT");
            m.Dynamic = () =>
            {
                var rows = new List<Option>();
                int total = global::IslandManager.TotalIslands - 1; // last scene isn't a playable island
                for (int i = 0; i < total; i++)
                {
                    int idx = i;
                    rows.Add(new Option
                    {
                        Kind = OptionKind.Action,
                        Name = (idx + 1) + ". " + IslandName(idx),
                        OnSelect = () =>
                        {
                            if (global::IslandManager.IsLoading) { Note("An island is already loading"); return; }
                            if (driving) ExitCar();
                            global::OnlineIslandManager.TpToSpecificIsland((byte)idx);
                            Note("Travelling to " + IslandName(idx) + "...");
                        },
                        Available = IsHostNow,
                        Requirement = NeedHost,
                    });
                }
                if (rows.Count == 0) rows.Add(new Option { Kind = OptionKind.Label, Name = "Load into a world first" });
                rows.Add(new Option { Kind = OptionKind.Label, Name = "Moves the whole lobby." });
                return rows;
            };
            return m;
        }

        // ---------------- Race track ----------------
        // Oval circuit floating just above the sea next to the island, with a jump ramp,
        // checkpoint arches, a start/finish gantry and a lap timer. Local build, like the sky base.
        private GameObject track;
        private Vector3 trackCentre;
        private const float TrackRx = 130f, TrackRz = 70f, TrackWidth = 16f;
        private const int TrackSegments = 72;
        private float lapStart, bestLap = -1f, lastLap = -1f;
        private int lapCount, nextCheckpoint;
        private bool racing;
        private static readonly float[] Checkpoints = { .25f, .6f, .8f }; // .5 is the jump gap

        private Vector3 TrackPoint(float t, float lift = 0f)
        {
            float a = (t + .75f) * Mathf.PI * 2f; // t=0 is the middle of the near straight, t=.5 the far straight
            return trackCentre + new Vector3(Mathf.Cos(a) * TrackRx, TrackHeight(t) + lift, Mathf.Sin(a) * TrackRz);
        }
        // Flat at sea level with a big ramp on the back straight (t≈0.5) that ends in a gap you jump.
        private static float TrackHeight(float t)
        {
            float d = t - .5f;
            if (d > -.07f && d < 0f) return 1f + (d + .07f) / .07f * 7f; // ramp up
            if (d >= 0f && d < .025f) return -50f;                           // the gap (no road)
            return 1f;
        }

        private void BuildTrack()
        {
            if (track != null) Destroy(track);
            var origin = IslandOrigin;
            Vector3 dir = Bridge.Player != null ? Bridge.Player.position - origin : Vector3.forward;
            dir.y = 0; if (dir.sqrMagnitude < 1f) dir = Vector3.forward;
            trackCentre = origin + dir.normalized * 260f;
            trackCentre.y = Bridge.WaterHeight;
            track = new GameObject("KRAKEN_RaceTrack");
            buildRoot = track.transform;
            buildLayer = LevelLayerIndex();
            Color glow = Accent, road = new Color(.12f, .12f, .14f), kerbA = new Color(.9f, .1f, .1f), kerbB = Color.white;

            for (int i = 0; i < TrackSegments; i++)
            {
                float t0 = i / (float)TrackSegments, t1 = (i + 1) / (float)TrackSegments;
                if (TrackHeight(t0) < -10f || TrackHeight(t1) < -10f) continue; // leave the jump gap open
                Vector3 a = TrackPoint(t0), b = TrackPoint(t1);
                Vector3 mid = (a + b) / 2f, fwd = b - a;
                var rot = Quaternion.LookRotation(fwd);
                var seg = Part(PrimitiveType.Cube, Vector3.zero, new Vector3(TrackWidth, .4f, fwd.magnitude + .6f), road);
                seg.transform.SetPositionAndRotation(mid, rot);
                // Kerbs alternate red/white; glowing barrier posts on both edges.
                foreach (int side in new[] { -1, 1 })
                {
                    var kerb = Part(PrimitiveType.Cube, Vector3.zero, new Vector3(1.2f, .45f, fwd.magnitude + .6f), i % 2 == 0 ? kerbA : kerbB, collider: false);
                    kerb.transform.SetPositionAndRotation(mid + rot * new Vector3(side * (TrackWidth / 2f - .6f), .05f, 0), rot);
                    if (i % 3 == 0)
                    {
                        var post = Part(PrimitiveType.Cube, Vector3.zero, new Vector3(.4f, 1.6f, .4f), glow, emissive: true, collider: false);
                        post.transform.SetPositionAndRotation(mid + rot * new Vector3(side * (TrackWidth / 2f + .4f), .8f, 0), rot);
                    }
                }
                // Centre line dashes.
                if (i % 2 == 0)
                {
                    var dash = Part(PrimitiveType.Cube, Vector3.zero, new Vector3(.3f, .42f, fwd.magnitude * .5f), Color.white, collider: false);
                    dash.transform.SetPositionAndRotation(mid + Vector3.up * .01f, rot);
                }
            }
            // Start/finish gantry and checkpoint arches at the quarter points.
            Arch(0f, Color.white, "START / FINISH");
            for (int c = 0; c < Checkpoints.Length; c++) Arch(Checkpoints[c], glow, "CHECKPOINT " + (c + 1));
            // Chequered start line.
            for (int x = 0; x < 8; x++) for (int z = 0; z < 2; z++)
                {
                    Vector3 p = TrackPoint(0f);
                    var rot = Quaternion.LookRotation(TrackPoint(.01f) - TrackPoint(0f));
                    var sq = Part(PrimitiveType.Cube, Vector3.zero, new Vector3(TrackWidth / 8f, .42f, 1f), (x + z) % 2 == 0 ? Color.white : Color.black, collider: false);
                    sq.transform.SetPositionAndRotation(p + rot * new Vector3(-TrackWidth / 2f + TrackWidth / 16f + x * TrackWidth / 8f, .02f, z - .5f), rot);
                }
            Note("RACE TRACK BUILT");
        }
        private void Arch(float t, Color c, string label)
        {
            Vector3 p = TrackPoint(t);
            var rot = Quaternion.LookRotation(TrackPoint(t + .005f) - p);
            foreach (int side in new[] { -1, 1 })
            {
                var leg = Part(PrimitiveType.Cube, Vector3.zero, new Vector3(.8f, 9f, .8f), c, emissive: true, collider: false);
                leg.transform.SetPositionAndRotation(p + rot * new Vector3(side * (TrackWidth / 2f + 1f), 4.5f, 0), rot);
            }
            var top = Part(PrimitiveType.Cube, Vector3.zero, new Vector3(TrackWidth + 3f, 1.4f, .8f), new Color(.05f, .06f, .1f), collider: false);
            top.transform.SetPositionAndRotation(p + Vector3.up * 9f, rot);
            var sign = new GameObject("Label");
            sign.transform.SetParent(buildRoot, false);
            sign.transform.SetPositionAndRotation(p + Vector3.up * 9f + rot * new Vector3(0, 0, -.45f), rot * Quaternion.Euler(0, 180f, 0));
            var text = sign.AddComponent<TextMesh>();
            text.text = label; text.fontSize = 80; text.characterSize = .08f; text.anchor = TextAnchor.MiddleCenter; text.color = c; text.fontStyle = FontStyle.Bold;
            try { var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); text.font = font; sign.GetComponent<MeshRenderer>().material = font.material; } catch { }
        }
        private void GoTrack()
        {
            if (!Bridge.Ready) return;
            if (track == null) BuildTrack();
            if (driving) ExitCar();
            if (car == null) SpawnCar();
            Vector3 start = TrackPoint(-.012f);
            car.transform.position = start;
            carYaw = Quaternion.LookRotation(TrackPoint(0f) - start).eulerAngles.y;
            car.transform.rotation = Quaternion.Euler(0, carYaw, 0);
            EnterCar();
            racing = true; lapCount = 0; nextCheckpoint = 1; lapStart = Time.time;
            Note("RACE ON! Drive through the checkpoints. Jump the gap on the back straight.");
        }
        private void TickRace()
        {
            if (!racing || track == null || car == null || !driving) return;
            Vector3 d = car.transform.position - trackCentre;
            float t = Mathf.Repeat(Mathf.Atan2(d.z / TrackRz, d.x / TrackRx) / (Mathf.PI * 2f) - .75f, 1f);
            // Checkpoints must be hit in order; crossing the start after all of them completes a lap.
            int n = Checkpoints.Length;
            bool crossed = nextCheckpoint <= n ? Mathf.Abs(Mathf.DeltaAngle(t * 360f, Checkpoints[nextCheckpoint - 1] * 360f)) < 6f : t < .03f || t > .985f;
            if (!crossed) return;
            if (nextCheckpoint <= n) { nextCheckpoint++; Note("CHECKPOINT " + (nextCheckpoint - 1) + "  " + (Time.time - lapStart).ToString("0.00") + "s"); return; }
            lastLap = Time.time - lapStart;
            if (bestLap < 0 || lastLap < bestLap) { bestLap = lastLap; Note("NEW BEST LAP! " + lastLap.ToString("0.00") + "s"); }
            else Note("LAP " + (lapCount + 1) + ": " + lastLap.ToString("0.00") + "s");
            lapCount++; nextCheckpoint = 1; lapStart = Time.time;
        }
        private void DrawRaceHud(Color accent)
        {
            if (!racing || !driving) return;
            var r = new Rect(Screen.width / 2f - 170, 20, 340, 70);
            Rect(r, new Color(0, 0, 0, .6f));
            Rect(new Rect(r.x, r.y, r.width, 4), accent);
            Text(new Rect(r.x, r.y + 4, r.width, 36), (Time.time - lapStart).ToString("0.00") + "s", new GUIStyle(centre) { fontSize = 28 }, Color.white);
            Text(new Rect(r.x, r.y + 42, r.width, 22), "LAP " + (lapCount + 1) + "   CP " + (nextCheckpoint - 1) + "/" + Checkpoints.Length + "   BEST " + (bestLap < 0 ? "--" : bestLap.ToString("0.00") + "s"), small, accent);
        }
        private Menu trackMenu;
        private Menu TrackMenu()
        {
            if (trackMenu != null) return trackMenu;
            var m = trackMenu = new Menu("RACE TRACK");
            m.Action("START RACE (TP + CAR)", GoTrack, InWorld, NeedPlayer);
            m.Action("REBUILD TRACK HERE", () => { BuildTrack(); }, InWorld, NeedPlayer);
            m.Action("STOP RACE", () => { racing = false; Note("Race stopped"); });
            m.Action("RESET BEST LAP", () => { bestLap = -1f; Note("Best lap reset"); });
            m.Action("REMOVE TRACK", () => { racing = false; if (track != null) Destroy(track); track = null; });
            m.Label("Local build: others see you racing on the sea.");
            return m;
        }
    }
}

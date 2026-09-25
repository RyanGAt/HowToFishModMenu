using System;
using UnityEngine;

namespace HowToFishCustomMenu
{
    // Floating sky base built from local primitives above the island. It exists on YOUR
    // client (you can stand on it); players without KRAKEN won't see it and will fall.
    public sealed partial class Plugin
    {
        private GameObject skyBase;
        private const float SkyHeight = 60f;
        private Vector3 SkyBaseCentre => IslandOrigin + Vector3.up * SkyHeight;
        private int buildLayer;
        private static readonly Vector3 T = new Vector3(-5f, 0f, -5f); // lookout tower centre
        private Transform buildRoot, skySign;

        // Palette: navy deck, warm wood, grey rock, glowing accent trims.
        private static readonly Color Navy = new Color(.07f, .1f, .18f), Wood = new Color(.55f, .38f, .22f),
            DarkWood = new Color(.36f, .24f, .14f), Rock = new Color(.32f, .33f, .36f), Steel = new Color(.75f, .8f, .88f);

        private void BuildSkyBase()
        {
            if (skyBase != null) return;
            skyBase = new GameObject("KRAKEN_SkyBase");
            skyBase.transform.position = SkyBaseCentre;
            buildRoot = skyBase.transform;
            buildLayer = LevelLayerIndex();
            Color glow = Accent;

            // Floating rock underside: stacked, shrinking discs with a glowing crystal tip.
            Disc(new Vector3(0, -1.6f, 0), 15f, 2.2f, Rock);
            Disc(new Vector3(0, -4.2f, 0), 11f, 3f, Rock * .85f);
            Disc(new Vector3(0, -7.4f, 0), 6.5f, 3.4f, Rock * .7f);
            Disc(new Vector3(0, -10.2f, 0), 2.6f, 2.4f, glow, emissive: true);
            var tip = Part(PrimitiveType.Sphere, new Vector3(0, -11.8f, 0), Vector3.one * 2.2f, glow, emissive: true);

            // Main deck: wooden planks inside a navy rim with a glowing trim ring.
            Disc(new Vector3(0, 0, 0), 16f, .6f, Navy);
            for (int i = -7; i <= 7; i++)
            {
                float half = Mathf.Sqrt(Mathf.Max(0f, 15f * 15f - (i * 2f) * (i * 2f)));
                Part(PrimitiveType.Cube, new Vector3(i * 2f, .33f, 0), new Vector3(1.9f, .08f, half * 2f), i % 2 == 0 ? Wood : DarkWood);
            }
            Disc(new Vector3(0, .36f, 0), 4.5f, .06f, Navy, collider: false);
            Disc(new Vector3(0, .38f, 0), 3.6f, .06f, glow, emissive: true, collider: false);

            // Railing: posts with glowing caps and rails around the edge (gap for the diving board).
            const int posts = 28;
            for (int i = 0; i < posts; i++)
            {
                if (i == 0) continue;
                float a = i * Mathf.PI * 2f / posts, a2 = (i + 1) * Mathf.PI * 2f / posts;
                var p = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 15.4f;
                Part(PrimitiveType.Cube, p + Vector3.up * .9f, new Vector3(.25f, 1.2f, .25f), Steel);
                Part(PrimitiveType.Sphere, p + Vector3.up * 1.6f, Vector3.one * .35f, glow, emissive: true, collider: false);
                if (i == posts - 1) continue;
                var p2 = new Vector3(Mathf.Cos(a2), 0, Mathf.Sin(a2)) * 15.4f;
                var mid = (p + p2) / 2f;
                var rail = Part(PrimitiveType.Cube, mid + Vector3.up * 1.45f, new Vector3(.12f, .12f, (p2 - p).magnitude), Steel);
                rail.transform.localRotation = Quaternion.LookRotation(p2 - p);
                var low = Part(PrimitiveType.Cube, mid + Vector3.up * .8f, new Vector3(.08f, .5f, (p2 - p).magnitude), glow * .6f, emissive: true);
                low.transform.localRotation = rail.transform.localRotation;
            }

            // Diving board off the open side of the rail.
            Part(PrimitiveType.Cube, new Vector3(18.5f, .45f, 0), new Vector3(7f, .2f, 1.4f), Steel);
            Part(PrimitiveType.Cube, new Vector3(21.8f, .6f, 0), new Vector3(.3f, .2f, 1.4f), glow, emissive: true);

            // Lookout tower: four pillars, spiral stairs round a centre column, upper deck.
            Part(PrimitiveType.Cylinder, T + new Vector3(0, 4.3f, 0), new Vector3(1.2f, 4f, 1.2f), Steel);
            foreach (var c in new[] { T + new Vector3(-3, 0, -3), T + new Vector3(3, 0, -3), T + new Vector3(-3, 0, 3), T + new Vector3(3, 0, 3) })
                Part(PrimitiveType.Cylinder, c + Vector3.up * 4.3f, new Vector3(.5f, 4f, .5f), Navy);
            // Spiral stairs wrap around outside the tower deck and finish level with it.
            for (int i = 0; i < 17; i++)
            {
                float a = 200f + i * 21f;
                var step = Part(PrimitiveType.Cube, Vector3.zero, new Vector3(2f, .25f, 1.3f), i % 2 == 0 ? Wood : DarkWood);
                step.transform.localRotation = Quaternion.Euler(0, a, 0);
                step.transform.localPosition = T + new Vector3(0, .55f + i * .5f, 0) + step.transform.localRotation * new Vector3(6.2f, 0, 0);
            }
            Disc(T + new Vector3(0, 8.6f, 0), 5f, .4f, Navy);
            Disc(T + new Vector3(0, 8.82f, 0), 4.4f, .06f, Wood, collider: false);
            Disc(T + new Vector3(0, 11.5f, 0), 5.6f, .3f, glow * .8f, emissive: true, collider: false);
            for (int i = 0; i < 4; i++)
            {
                float a = i * 90f + 45f;
                var q = Quaternion.Euler(0, a, 0);
                Part(PrimitiveType.Cylinder, T + new Vector3(0, 10.1f, 0) + q * new Vector3(4.2f, 0, 0), new Vector3(.2f, 1.4f, .2f), Steel, collider: false);
            }

            // KRAKEN sign above the tower roof.
            var sign = new GameObject("Sign");
            sign.transform.SetParent(buildRoot, false);
            sign.transform.localPosition = T + new Vector3(0, 13.6f, 0);
            sign.transform.localRotation = Quaternion.Euler(0, 45f, 0);
            var text = sign.AddComponent<TextMesh>();
            text.text = "KRAKEN";
            text.fontSize = 120; text.characterSize = .12f; text.anchor = TextAnchor.MiddleCenter; text.alignment = TextAlignment.Center;
            text.fontStyle = FontStyle.Bold;
            text.color = glow;
            try
            {
                var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font != null) { text.font = font; sign.GetComponent<MeshRenderer>().material = font.material; }
            }
            catch { }
            skySign = sign.transform;

            // Lounge corner: sofa blocks, table and a couple of lamps.
            Part(PrimitiveType.Cube, new Vector3(9, .9f, -6), new Vector3(6, .9f, 1.6f), Navy);
            Part(PrimitiveType.Cube, new Vector3(9, 1.6f, -6.7f), new Vector3(6, 1.4f, .4f), Navy);
            Part(PrimitiveType.Cylinder, new Vector3(9, .75f, -3.5f), new Vector3(2.2f, .3f, 2.2f), Wood);
            foreach (var lamp in new[] { new Vector3(10, 0, 8), new Vector3(-10, 0, 8), new Vector3(11, 0, -3) })
            {
                Part(PrimitiveType.Cylinder, lamp + Vector3.up * 1.8f, new Vector3(.18f, 1.5f, .18f), Steel, collider: false);
                Part(PrimitiveType.Sphere, lamp + Vector3.up * 3.5f, Vector3.one * .7f, Color.Lerp(glow, Color.white, .5f), emissive: true, collider: false);
                AddLight(lamp + Vector3.up * 3.5f, 14f, 1.6f, Color.Lerp(glow, Color.white, .5f));
            }
            AddLight(new Vector3(0, 4, 0), 30f, 1.2f, glow);
            AddLight(new Vector3(0, -12f, 0), 25f, 3f, glow);

            Note("SKY BASE BUILT " + SkyHeight.ToString("0") + "m UP");
        }

        // One sign that always turns to face you, so it never reads backwards.
        private void TickSkySign()
        {
            if (skySign == null) return;
            var cam = GameCamera;
            if (cam == null) return;
            Vector3 d = skySign.position - cam.transform.position; d.y = 0;
            if (d.sqrMagnitude > .01f) skySign.rotation = Quaternion.LookRotation(d);
        }
        private void AddLight(Vector3 local, float range, float intensity, Color c)
        {
            var l = new GameObject("Light").AddComponent<Light>();
            l.transform.SetParent(buildRoot, false);
            l.transform.localPosition = local;
            l.type = LightType.Point; l.range = range; l.intensity = intensity; l.color = c;
        }
        // Flat disc: a cylinder whose capsule collider is swapped for a mesh collider so you can stand on the edge.
        private GameObject Disc(Vector3 local, float radius, float height, Color c, bool emissive = false, bool collider = true)
            => Part(PrimitiveType.Cylinder, local, new Vector3(radius * 2f, height / 2f, radius * 2f), c, emissive, collider);

        private int LevelLayerIndex()
        {
            try
            {
                int mask = global::GameInfo.LevelLayer.value;
                for (int i = 0; i < 32; i++) if ((mask & (1 << i)) != 0) return i;
            }
            catch { }
            return 0;
        }
        private GameObject Part(PrimitiveType type, Vector3 local, Vector3 scale, Color colour, bool emissive = false, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.transform.SetParent(buildRoot, false);
            go.transform.localPosition = local;
            go.transform.localScale = scale;
            go.layer = buildLayer;
            try { go.tag = "Level"; } catch { }
            var col = go.GetComponent<Collider>();
            if (!collider) Destroy(col);
            else if (type == PrimitiveType.Cylinder)
            {
                Destroy(col);
                go.AddComponent<MeshCollider>().sharedMesh = go.GetComponent<MeshFilter>().sharedMesh;
            }
            go.GetComponent<Renderer>().material = MakeMaterial(colour, emissive);
            return go;
        }
        private static Material MakeMaterial(Color colour, bool emissive)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Universal Render Pipeline/Simple Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", colour);
            mat.color = colour;
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", emissive ? .8f : .25f);
            if (emissive)
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", colour * 2.2f);
            }
            return mat;
        }
        private void GoSkyBase()
        {
            if (!Bridge.Ready) return;
            BuildSkyBase();
            skyBase.transform.position = SkyBaseCentre;
            Go(SkyBaseCentre + new Vector3(5f, 2f, 6f), "Sky base");
        }
        private void RemoveSkyBase()
        {
            if (skyBase != null) Destroy(skyBase);
            skyBase = null;
            Note("Sky base removed");
        }
        private Menu skyMenu;
        private Menu SkyBaseMenu()
        {
            if (skyMenu != null) return skyMenu;
            var m = skyMenu = new Menu("SKY BASE");
            m.Add(new Option { Kind = OptionKind.Action, NameFn = () => "TELEPORT TO SKY BASE  [" + keySkyBase.Value.ToString().ToUpper() + "]", OnSelect = GoSkyBase, Available = InWorld, Requirement = NeedPlayer });
            m.Action("TELEPORT TO LOOKOUT TOWER", () => { BuildSkyBase(); Go(SkyBaseCentre + T + new Vector3(1.5f, 10.5f, 1.5f), "Lookout"); }, InWorld, NeedPlayer);
            m.Action("TELEPORT TO DIVING BOARD", () => { BuildSkyBase(); Go(SkyBaseCentre + new Vector3(20.5f, 2f, 0), "Diving board"); }, InWorld, NeedPlayer);
            m.Action("BUILD / REBUILD SKY BASE", () => { RemoveSkyBase(); BuildSkyBase(); }, InWorld, NeedPlayer);
            m.Action("TELEPORT BACK TO ISLAND", () => Go(Bridge.Grounded(IslandOrigin), "Island"), InWorld, NeedPlayer);
            m.Action("REMOVE SKY BASE", RemoveSkyBase);
            m.Label("Local build: friends need KRAKEN to stand on it.");
            return m;
        }
    }
}

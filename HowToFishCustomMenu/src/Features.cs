using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HowToFishCustomMenu
{
    // Feature state and the per-frame effects that keep toggles applied.
    public sealed partial class Plugin
    {
        // Player
        private bool fly, noClip, airJump, demiGod, neverHungry, spinbot, walkOnWater, freezeAir;
        private float speedMult = 1f, jumpMult = 1f, gravityMult = 1f;
        private int bodyShape; // 0 normal, 1 giant, 2 tiny, 3 skinny, 4 wide
        private Vector3 originalScale = Vector3.one; private Transform scaledPlayer;
        private Rigidbody playerBody; private bool bodyGravitySaved, bodyOldGravity;
        private Vector3 originalGravity; private bool gravitySaved;
        private float spinAngle, nextHeal;
        // Fishing / items
        private bool autoFish, fishTornado, fishMagnet, flyingFish, floatingItems, spinningItems, bouncingItems;
        private float fishScale = 1f, valueMult = 2f, weightMult = 1f, itemScale = 1f;
        private int spawnQty = 1;
        private string itemSearch = "", itemId = "", money = "100000";
        private float nextAutoFish, nextItemTick;
        private readonly Queue<Action> spawnQueue = new Queue<Action>();
        // Weapons / aim
        private bool noRecoil, instantReload;
        private float damageMult = 1f;
        private bool infiniteAmmo, rapidFire, noCooldown, zeroSpread, fastProjectiles, noWeaponKick, fishCannon;
        private bool aimEnabled, aimAdsOnly = false, aimSmooth, aimWide, aimPrediction, triggerbot;
        private float nextTrigger, nextCannon;
        // Boat
        private bool boatBoost, boatFlying, boatSpin, rainbowBoat, driveOnLand;
        private float boatMult = 2.5f, boatScale = 1f;
        // Teleport
        private string coordX = "0", coordY = "20", coordZ = "0";
        // Players / lobby
        private object target; // selected player
        private readonly Dictionary<object, float> frozen = new Dictionary<object, float>();
        private readonly HashSet<object> spinning = new HashSet<object>(), bouncing = new HashSet<object>(), rainOn = new HashSet<object>(), healAura = new HashSet<object>();
        private readonly Dictionary<object, Vector3> freezePos = new Dictionary<object, Vector3>();
        private float nextPlayerTick;
        private bool unlimitedMoney;
        // Visual
        private bool disco, rainbowMenu, rainbowWorld, drunkCam, upsideDown, cameraSpin, noFog, superFog;
        private float fov = -1f, timeScale = 1f, originalFov = -1f;
        private bool fogSaved, oldFog; private float oldFogDensity; private Color oldFogColor, oldAmbient;
        private Camera fovCamera;
        // ESP
        private bool espFish, espItems, espPlayers, espValuable, espBoxes, espTracers, espDistance = true, espRainbow;
        private float espRange = 150f;
        private int tracerOrigin;
        private readonly List<KeyValuePair<Transform, string>> espTargets = new List<KeyValuePair<Transform, string>>();
        private float nextEsp;

        private bool Host => Bridge.Ready && Bridge.IsHost;
        private Color Accent => rainbowMenu ? Color.HSVToRGB(Mathf.Repeat(Time.unscaledTime * .15f, 1f), .8f, 1f) : accentColours[accentIndex];

        private void TickFeatures()
        {
            if (!Bridge.Ready) { if (fly) StopFly(); return; }
            var player = Bridge.Player;
            if (fly) UpdateFly(player);
            bool jump = Bridge.JumpPressed;
            if (airJump && jump && !wasJump && !shown && !fly) Bridge.AirJump();
            wasJump = jump;
            if (spinbot) { spinAngle = Mathf.Repeat(spinAngle + Time.deltaTime * 900f, 360f); Bridge.SetYaw(spinAngle); }
            if (walkOnWater && !fly && playerBody != null)
            {
                float water = Bridge.WaterHeight;
                if (player.position.y < water + .05f)
                {
                    var p = playerBody.position; p.y = water + .05f; playerBody.position = p;
                    var v = playerBody.linearVelocity; if (v.y < 0) v.y = 0; playerBody.linearVelocity = v;
                }
            }
            if (playerBody == null) playerBody = player.GetComponent<Rigidbody>();
            if (freezeAir && playerBody != null && !fly) playerBody.linearVelocity = Vector3.zero;
            if (Host && Time.unscaledTime >= nextHeal)
            {
                nextHeal = Time.unscaledTime + 1f;
                if (demiGod) Bridge.HealPlayer(Bridge.LocalPlayerObject);
                else if (neverHungry) Bridge.HealPlayer(Bridge.LocalPlayerObject);
                if (unlimitedMoney && MoneyNow() < 1000000) Bridge.SetMoney(9999999);
            }

            // Fishing
            if (autoFish && Host && Time.unscaledTime >= nextAutoFish) { nextAutoFish = Time.unscaledTime + .25f; Bridge.InstantCatch(); }
            if (spawnQueue.Count > 0) for (int i = 0; i < 4 && spawnQueue.Count > 0; i++) spawnQueue.Dequeue()();

            // Boat
            if (Host && Bridge.BoatBody != null)
            {
                var body = Bridge.BoatBody;
                if (boatBoost && Input.GetKey(KeyCode.W)) Bridge.ApplyBoatThrust(boatMult);
                if (boatFlying)
                {
                    if (jump) Bridge.ApplyBoatVertical(18f);
                    if (Input.GetKey(KeyCode.LeftControl)) Bridge.ApplyBoatVertical(-18f);
                }
                if (boatSpin) body.angularVelocity = new Vector3(0f, 8f, 0f);
                if (driveOnLand && Input.GetKey(KeyCode.W)) body.AddForce(body.transform.forward * 25f + Vector3.up * 6f, ForceMode.Acceleration);
            }
            if (rainbowBoat && Time.frameCount % 6 == 0 && Bridge.BoatVisual != null) TintRenderers(Bridge.BoatVisual, Color.HSVToRGB(Mathf.Repeat(Time.time * .3f, 1f), 1f, 1f));

            // Players (host authority)
            if (Host && Time.unscaledTime >= nextPlayerTick) TickPlayers();

            // Items
            if (Host && Time.unscaledTime >= nextItemTick && (floatingItems || spinningItems || bouncingItems || flyingFish || fishMagnet))
            {
                nextItemTick = Time.unscaledTime + .1f;
                int n = 0;
                foreach (var pair in Bridge.World())
                {
                    if (++n > 150) break;
                    var rig = (pair.Value as Component) != null ? pair.Key.GetComponent<Rigidbody>() : null;
                    if (rig == null || rig.isKinematic) continue;
                    bool fish = Bridge.IsCreature(pair.Value);
                    if (floatingItems || flyingFish && fish) rig.AddForce(-Physics.gravity * 1.15f * .1f, ForceMode.VelocityChange);
                    if (spinningItems) rig.angularVelocity = new Vector3(0, 15f, 0);
                    if (bouncingItems && UnityEngine.Random.value < .05f) rig.AddForce(Vector3.up * 8f, ForceMode.VelocityChange);
                    if (fishMagnet && fish)
                    {
                        Vector3 d = player.position + Vector3.up - rig.position;
                        if (d.sqrMagnitude < 60f * 60f && d.sqrMagnitude > 4f) rig.AddForce(d.normalized * 3f, ForceMode.VelocityChange);
                    }
                }
            }

            // Visual
            if (rainbowWorld) { RenderSettings.fogColor = Color.HSVToRGB(Mathf.Repeat(Time.time * .2f, 1f), .8f, 1f); RenderSettings.ambientLight = RenderSettings.fogColor; }
            if (chaosOn && Time.unscaledTime >= nextChaos) TickChaos();

        }
        private bool wasJump;

        private void TickLate()
        {
            if (!Bridge.Ready) return;
            var cam = GameCamera;
            if (cam != null)
            {
                float roll = 0f;
                if (drunkCam) roll += Mathf.Sin(Time.time * 1.7f) * 12f;
                if (upsideDown) roll += 180f;
                if (cameraSpin) roll += Mathf.Repeat(Time.time * 120f, 360f);
                if (roll != 0f) cam.transform.rotation *= Quaternion.AngleAxis(roll, Vector3.forward);
                if (fov > 0f) { if (fovCamera != cam) { fovCamera = cam; originalFov = cam.fieldOfView; } cam.fieldOfView = fov + (drunkCam ? Mathf.Sin(Time.time * 2.3f) * 8f : 0f); }
            }
            if (!shown && aimEnabled) Bridge.AimAtTarget(aimAdsOnly, aimSmooth, aimWide, aimPrediction);
            if (!shown && triggerbot && Time.unscaledTime >= nextTrigger) { nextTrigger = Time.unscaledTime + .12f; Bridge.Triggerbot(aimAdsOnly); }
            if (fishTornado && Host) Bridge.FishTornadoTick();
            if (fishCannon && Host && !shown && Input.GetMouseButton(2) && Time.unscaledTime >= nextCannon) { nextCannon = Time.unscaledTime + .12f; LaunchFish(); }
            Patches.NoRecoil = noRecoil; Patches.NoCooldown = noCooldown; Patches.DamageMultiplier = damageMult;
            if (instantReload) Bridge.InstantReload();
            try { TickCamera(); } catch (Exception ex) { Logger.LogWarning("Camera: " + ex.Message); }
            Bridge.UpdateWeaponMods(infiniteAmmo, rapidFire, noCooldown, zeroSpread, fastProjectiles, noWeaponKick);
        }

        private void TickPlayers()
        {
            nextPlayerTick = Time.unscaledTime + .1f;
            foreach (var p in Bridge.Players())
            {
                var t = Bridge.PlayerTransform(p);
                if (t == null) continue;
                if (freezePos.TryGetValue(p, out var pos)) Bridge.TeleportPlayer(p, pos);
                if (spinning.Contains(p)) Bridge.TeleportPlayerRotated(p, t.position, Mathf.Repeat(Time.time * 720f, 360f));
                if (bouncing.Contains(p) && UnityEngine.Random.value < .08f) Bridge.KnockPlayer(p, Vector3.up * 14f);
                if (rainOn.Contains(p) && UnityEngine.Random.value < .3f) SpawnFishAround(t.position + Vector3.up * 14f, 1, 6f, 1f);
                if (healAura.Contains(p) && UnityEngine.Random.value < .1f) Bridge.HealPlayer(p);
            }
        }

        // ---------------- Helpers ----------------
        private int MoneyNow() => Bridge.MoneyValue;
        private void UpdateFly(Transform t)
        {
            var rb = t.GetComponent<Rigidbody>();
            if (rb != null)
            {
                if (!bodyGravitySaved) { bodyOldGravity = rb.useGravity; bodyGravitySaved = true; }
                rb.useGravity = false; rb.linearVelocity = Vector3.zero;
            }
            var look = Bridge.LookTransform ?? t;
            Vector3 f = look.forward, r = look.right; f.y = r.y = 0; f.Normalize(); r.Normalize();
            Vector3 d = Vector3.zero;
            if (!shown)
            {
                if (Input.GetKey(KeyCode.W)) d += f; if (Input.GetKey(KeyCode.S)) d -= f;
                if (Input.GetKey(KeyCode.D)) d += r; if (Input.GetKey(KeyCode.A)) d -= r;
                if (Bridge.JumpPressed) d += Vector3.up; if (Input.GetKey(KeyCode.LeftControl)) d -= Vector3.up;
            }
            float speed = flySpeed.Value * (Input.GetKey(KeyCode.LeftShift) ? 2.5f : 1f);
            if (d.sqrMagnitude > 0f) Bridge.Teleport(t.position + d.normalized * speed * Time.deltaTime);
        }
        private void StopFly()
        {
            fly = false;
            var t = Bridge.Player;
            var rb = t != null ? t.GetComponent<Rigidbody>() : null;
            if (rb != null && bodyGravitySaved) rb.useGravity = bodyOldGravity;
            bodyGravitySaved = false;
        }
        private void SetBodyShape(int shape)
        {
            var t = Bridge.Player;
            if (t == null) return;
            if (scaledPlayer != t) { scaledPlayer = t; originalScale = t.localScale; }
            bodyShape = shape;
            Vector3[] shapes = { Vector3.one, Vector3.one * 3f, Vector3.one * .35f, new Vector3(.25f, 1f, .25f), new Vector3(2.5f, .7f, 2.5f) };
            t.localScale = Vector3.Scale(originalScale, shapes[shape]);
        }
        private void SetGravity(float multiplier)
        {
            if (!gravitySaved) { originalGravity = Physics.gravity; gravitySaved = true; }
            gravityMult = multiplier;
            Physics.gravity = originalGravity * multiplier;
        }
        private void SetFog(bool? off, bool? heavy)
        {
            if (!fogSaved) { oldFog = RenderSettings.fog; oldFogDensity = RenderSettings.fogDensity; oldFogColor = RenderSettings.fogColor; oldAmbient = RenderSettings.ambientLight; fogSaved = true; }
            if (off.HasValue) noFog = off.Value;
            if (heavy.HasValue) superFog = heavy.Value;
            RenderSettings.fog = superFog || (!noFog && oldFog);
            RenderSettings.fogDensity = superFog ? .08f : oldFogDensity;
            if (superFog) { RenderSettings.fogMode = FogMode.ExponentialSquared; }
        }
        private void RestoreWorldVisuals()
        {
            rainbowWorld = false;
            if (!fogSaved) return;
            RenderSettings.fog = oldFog; RenderSettings.fogDensity = oldFogDensity; RenderSettings.fogColor = oldFogColor; RenderSettings.ambientLight = oldAmbient;
            noFog = superFog = false;
        }
        private static void TintRenderers(Transform root, Color c)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>())
                foreach (var m in r.materials) { if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c); else if (m.HasProperty("_Color")) m.color = c; }
        }
        private void SetTimeScale(float s) { timeScale = s; Time.timeScale = s; }

        private int SpawnFishAround(Vector3 centre, int count, float radius, float scale)
        {
            var prefabs = Bridge.SpawnablesWith("Fish").ToList();
            if (prefabs.Count == 0) return 0;
            int n = 0;
            for (int i = 0; i < count; i++)
            {
                float a = i * Mathf.PI * 2f / count;
                var pos = centre + new Vector3(Mathf.Cos(a) * radius, UnityEngine.Random.Range(0f, 1f), Mathf.Sin(a) * radius);
                if (Bridge.SpawnPrefab(prefabs[UnityEngine.Random.Range(0, prefabs.Count)], pos, Quaternion.Euler(0, UnityEngine.Random.Range(0, 360f), 0), scale * fishScale) != null) n++;
            }
            return n;
        }
        private void QueueSpawn(object prefab, int count, float scale = 1f)
        {
            for (int i = 0; i < count; i++)
            {
                int k = i;
                spawnQueue.Enqueue(() =>
                {
                    Vector3 offset = new Vector3((k % 6) * .5f - 1.25f, .3f + (k / 36) * .4f, ((k / 6) % 6) * .5f);
                    Bridge.SpawnPrefab(prefab, Bridge.InFront(2.5f) + offset, Quaternion.identity, scale * itemScale);
                });
            }
        }
        private void LaunchFish()
        {
            var look = Bridge.LookTransform;
            var prefabs = Bridge.SpawnablesWith("Fish").ToList();
            if (look == null || prefabs.Count == 0) return;
            var item = Bridge.SpawnPrefab(prefabs[UnityEngine.Random.Range(0, prefabs.Count)], look.position + look.forward * 1.8f, look.rotation, fishScale) as Component;
            var rig = item != null ? item.GetComponent<Rigidbody>() : null;
            if (rig != null) rig.linearVelocity = look.forward * 45f;
        }
        private void LaunchNearby(float force)
        {
            var p = Bridge.Player;
            if (p == null) return;
            int n = 0;
            foreach (var pair in Bridge.World())
            {
                var rig = pair.Key.GetComponent<Rigidbody>();
                if (rig == null || rig.isKinematic) continue;
                Vector3 d = pair.Key.position - p.position;
                if (d.sqrMagnitude > 20f * 20f) continue;
                rig.AddForce((d.normalized + Vector3.up) * force, ForceMode.VelocityChange); n++;
            }
            Note("Launched " + n + " objects");
        }

        private void RefreshEsp()
        {
            nextEsp = Time.unscaledTime + .5f;
            espTargets.Clear();
            if (espPlayers)
                foreach (var p in Bridge.Players())
                    if (!Bridge.IsLocal(p) && Bridge.PlayerTransform(p) is Transform t) espTargets.Add(new KeyValuePair<Transform, string>(t, "P|" + Bridge.PlayerName(p)));
            foreach (var pair in Bridge.World())
            {
                if (espTargets.Count > 250) break;
                bool fish = Bridge.IsCreature(pair.Value);
                int worth = Bridge.ItemWorth(pair.Value);
                bool valuable = espValuable && worth >= 100;
                if ((fish && espFish) || (!fish && espItems) || valuable)
                {
                    string label;
                    try { label = Bridge.ItemLabel(pair.Value); } catch (Exception) { label = pair.Key.name.Replace("(Clone)", ""); }
                    espTargets.Add(new KeyValuePair<Transform, string>(pair.Key, (valuable ? "V|" : fish ? "F|" : "I|") + label));
                }
            }
        }

        private void ResetEverything()
        {
            if (driving) ExitCar();
            if (camMode != 0) SetCamMode(0);
            killAura = bossRush = autoSellFish = false;
            StopFly();
            Bridge.SetNoClip(false); noClip = false;
            Bridge.ResetWeaponMods();
            Bridge.SetBoatFlying(false); boatFlying = boatBoost = boatSpin = driveOnLand = false;
            if (gravitySaved) { Physics.gravity = originalGravity; gravityMult = 1f; }
            if (bodyShape != 0) SetBodyShape(0);
            if (speedMult != 1f) { speedMult = 1f; Bridge.SetSpeed(1f); }
            if (jumpMult != 1f) { jumpMult = 1f; Bridge.SetJumpMultiplier(1f); }
            if (fovCamera != null && originalFov > 0f) fovCamera.fieldOfView = originalFov;
            fov = -1f;
            SetTimeScale(1f);
            RestoreWorldVisuals();
            drunkCam = upsideDown = cameraSpin = disco = spinbot = freezeAir = walkOnWater = false;
            floatingItems = spinningItems = bouncingItems = flyingFish = fishMagnet = fishTornado = false;
            freezePos.Clear(); spinning.Clear(); bouncing.Clear(); rainOn.Clear(); healAura.Clear();
            Patches.PersonalGod = Patches.ExplosiveBullets = Patches.SilentAim = false;
            noRecoil = noCooldown = instantReload = false; damageMult = 1f;
            chaosOn = false;
        }
    }
}

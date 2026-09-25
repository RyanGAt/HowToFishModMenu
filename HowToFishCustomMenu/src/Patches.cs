using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace HowToFishCustomMenu
{
    // Runtime Harmony hooks on the game's own server/client code paths.
    // Patched by name so the mod keeps loading if a game update renames a member;
    // a missing target is logged and its feature just stays inert.
    internal static class Patches
    {
        public static bool PersonalGod, UnlimitedBait, ExplosiveBullets, SilentAim;
        public static BepInEx.Logging.ManualLogSource Log;
        public static bool NoRecoil, NoCooldown;
        public static float DamageMultiplier = 1f;
        public static int ForcedRoulette = -1; // -1 off, 0 Black, 1 Red, 2 Green (BetColor order)
        public static bool GuaranteedWin;
        public static float WinningsMultiplier = 1f;
        public static Func<Vector3, Vector3?> SilentAimTarget; // camera pos -> target point
        public static GameBridge Bridge;
        public static string Report = "";
        private static float nextExplosion;

        private static Type T(string name) => AppDomain.CurrentDomain.GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == "Assembly-CSharp")?.GetType(name, false);

        public static void Apply(Harmony harmony, BepInEx.Logging.ManualLogSource log)
        {
            Log = log;
            int ok = 0, total = 0;
            void Patch(string type, string method, string prefix = null, string postfix = null)
            {
                total++;
                try
                {
                    var target = AccessTools.Method(T(type), method);
                    if (target == null) { log.LogWarning("Hook missing: " + type + "." + method); return; }
                    harmony.Patch(target,
                        prefix == null ? null : new HarmonyMethod(typeof(Patches).GetMethod(prefix, BindingFlags.Static | BindingFlags.NonPublic)),
                        postfix == null ? null : new HarmonyMethod(typeof(Patches).GetMethod(postfix, BindingFlags.Static | BindingFlags.NonPublic)));
                    ok++;
                }
                catch (Exception ex) { log.LogWarning("Hook failed " + type + "." + method + ": " + ex.Message); }
            }
            Patch("PlayerVitals", "TakeDamage", prefix: nameof(TakeDamagePrefix));
            Patch("PlayerInventory", "ServerOnBaitUsed", prefix: nameof(BaitUsedPrefix));
            Patch("CasinoManager", "ServerRouletteResult", prefix: nameof(RoulettePrefix));
            Patch("Item", "AddBetMultiplier", prefix: nameof(BetMultiplierPrefix));
            Patch("ProjectileManager", "Hit", postfix: nameof(HitPostfix));
            Patch("ProjectileManager", "AddProjectile", prefix: nameof(AddProjectilePrefix));
            Patch("ProjectileManager", "AddProjectiles", prefix: nameof(AddProjectilesPrefix));
            Patch("PlayerCamera", "Recoil", prefix: nameof(RecoilPrefix));
            Patch("PlayerToolMovement", "Recoil", prefix: nameof(RecoilPrefix));
            Patch("Weapon", "AddModelRecoil", prefix: nameof(RecoilPrefix));
            Patch("Weapon", "HasCooldown", prefix: nameof(HasCooldownPrefix));
            total++;
            try
            {
                harmony.Patch(AccessTools.PropertyGetter(typeof(global::Attachments), "Damage"),
                    postfix: new HarmonyMethod(typeof(Patches).GetMethod(nameof(DamagePostfix), BindingFlags.Static | BindingFlags.NonPublic)));
                ok++;
            }
            catch (Exception ex) { log.LogWarning("Hook failed Attachments.Damage: " + ex.Message); }
            Report = ok + "/" + total + " hooks active";
            log.LogInfo("Harmony: " + Report);
        }

        // Host only: the game applies damage on the server, so this protects the host's own player.
        private static bool TakeDamagePrefix(object __instance)
        {
            if (!PersonalGod || Bridge == null) return true;
            var player = __instance.GetType().GetField("_player", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(__instance);
            return !Bridge.IsLocal(player);
        }

        private static bool BaitUsedPrefix() => !UnlimitedBait;

        // Typed against the game's BetColor so Harmony writes the changed result back.
        private static void RoulettePrefix(ref global::BetColor winColor)
        {
            var before = winColor;
            if (GuaranteedWin)
            {
                var bet = typeof(global::CasinoManager).GetField("_curBetColor", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null);
                if (bet is global::BetColor b) winColor = b;
            }
            else if (ForcedRoulette >= 0) winColor = (global::BetColor)ForcedRoulette;
            if (winColor != before) Log?.LogInfo("Roulette rigged: " + before + " -> " + winColor);
        }

        private static void BetMultiplierPrefix(ref float multiplier)
        {
            if (WinningsMultiplier > 1f && multiplier > 1f) multiplier *= WinningsMultiplier;
        }

        private static void HitPostfix(object[] __args)
        {
            if (!ExplosiveBullets || Bridge == null || !Bridge.IsHost || Time.time < nextExplosion) return;
            var projectile = __args[0];
            var owner = projectile?.GetType().GetField("Owner")?.GetValue(projectile);
            if (!Bridge.IsLocal(owner) || !(__args[2] is RaycastHit hit)) return;
            nextExplosion = Time.time + .08f;
            Bridge.ExplodeAt(hit.point + hit.normal * .3f);
        }

        private static void AddProjectilePrefix(bool isLocal, ref Vector3 pos, ref Vector3 velocity)
        {
            if (!SilentAim || !isLocal) return;
            // Weapon.Shoot moves pos far below the map when its point-blank ray already hit.
            if (pos.y < -5000f) return;
            var target = SilentAimTarget?.Invoke(pos);
            if (target.HasValue) velocity = (target.Value - pos).normalized * velocity.magnitude;
        }

        private static void AddProjectilesPrefix(bool isLocal, ref Vector3 pos, ref Vector3[] velocities)
        {
            if (!SilentAim || !isLocal || velocities == null || velocities.Length == 0 || pos.y < -5000f) return;
            var target = SilentAimTarget?.Invoke(pos);
            if (!target.HasValue) return;
            Vector3 mean = Vector3.zero;
            foreach (var v in velocities) mean += v;
            Quaternion shift = Quaternion.FromToRotation(mean.normalized, (target.Value - pos).normalized);
            var copy = new Vector3[velocities.Length];
            for (int i = 0; i < velocities.Length; i++) copy[i] = shift * velocities[i];
            velocities = copy;
        }

        // Only the local player's gun fires these, so skipping them removes camera kick,
        // weapon model kick and view punch.
        private static bool RecoilPrefix() => !NoRecoil;

        private static bool HasCooldownPrefix(global::Weapon __instance, ref bool __result)
        {
            if (!NoCooldown || __instance.Holder != global::Player.LocalPlayer) return true;
            __result = false;
            return false;
        }

        // Weapon.Damage -> Attachments.Damage feeds both projectiles and point-blank hits.
        private static void DamagePostfix(global::Attachments __instance, ref int __result)
        {
            if (DamageMultiplier <= 1f || __instance.Weapon == null || __instance.Weapon.Holder != global::Player.LocalPlayer) return;
            __result = Mathf.RoundToInt(__result * DamageMultiplier);
        }
    }
}

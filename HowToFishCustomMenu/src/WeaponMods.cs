using System;
using System.Collections.Generic;
using System.Reflection;

namespace HowToFishCustomMenu
{
    internal sealed partial class GameBridge
    {
        private object modifiedWeapon;
        private readonly Dictionary<string, object> originalWeaponValues = new Dictionary<string, object>();
        private static readonly BindingFlags WeaponFlags = BindingFlags.Instance | BindingFlags.NonPublic;

        private object HeldWeapon()
        {
            RefreshPlayer();
            return GetMember(GetMember(GetMember(localPlayer, "Holding"), "HeldItem"), "Weapon");
        }

        public bool HasHeldWeapon => HeldWeapon() != null;

        private void SetWeaponField(object weapon, string name, object value, bool enabled)
        {
            var field = weapon.GetType().GetField(name, WeaponFlags);
            if (field == null) return;
            if (!originalWeaponValues.ContainsKey(name)) originalWeaponValues[name] = field.GetValue(weapon);
            field.SetValue(weapon, enabled ? value : originalWeaponValues[name]);
        }

        public void ResetWeaponMods()
        {
            if (modifiedWeapon != null)
            {
                foreach (var original in originalWeaponValues)
                {
                    try { modifiedWeapon.GetType().GetField(original.Key, WeaponFlags)?.SetValue(modifiedWeapon, original.Value); }
                    catch (Exception) { /* A despawned weapon can no longer be restored. */ }
                }
            }
            modifiedWeapon = null;
            originalWeaponValues.Clear();
        }

        public bool RefillMagazine()
        {
            var weapon = HeldWeapon();
            var attachments = GetMember(weapon, "Attachments");
            var ammo = GetMember(attachments, "AmmoPerMag");
            var setter = weapon?.GetType().GetProperty("Ammo", BindingFlags.Instance | BindingFlags.Public)?.GetSetMethod(true);
            if (setter == null || !(ammo is int count)) return false;
            setter.Invoke(weapon, new object[] { count });
            return true;
        }

        public void UpdateWeaponMods(bool infiniteAmmo, bool rapidFire, bool noCooldown, bool zeroSpread, bool fastProjectiles, bool noKick)
        {
            if (!infiniteAmmo && !rapidFire && !noCooldown && !zeroSpread && !fastProjectiles && !noKick)
            {
                if (modifiedWeapon != null) ResetWeaponMods();
                return;
            }
            var weapon = HeldWeapon();
            if (!ReferenceEquals(weapon, modifiedWeapon))
            {
                ResetWeaponMods();
                modifiedWeapon = weapon;
            }
            if (weapon == null) return;
            if (infiniteAmmo) RefillMagazine();
            if (rapidFire)
            {
                SetWeaponField(weapon, "_timeBetweenShots", .06f, true);
            }
            else if (originalWeaponValues.ContainsKey("_timeBetweenShots")) SetWeaponField(weapon, "_timeBetweenShots", null, false);
            SetWeaponField(weapon, "_fullAuto", true, rapidFire);
            SetWeaponField(weapon, "_spread", 0f, zeroSpread);
            var speedField = weapon.GetType().GetField("_projSpeed", WeaponFlags);
            if (speedField != null)
            {
                if (!originalWeaponValues.ContainsKey("_projSpeed")) originalWeaponValues["_projSpeed"] = speedField.GetValue(weapon);
                speedField.SetValue(weapon, fastProjectiles ? (float)originalWeaponValues["_projSpeed"] * 3f : originalWeaponValues["_projSpeed"]);
            }
            SetWeaponField(weapon, "_recoilKnockback", 0, noKick);
            SetWeaponField(weapon, "_noShootingDuringShootAnim", false, noCooldown);
            if (noCooldown) weapon.GetType().GetField("_hasCoolDown", WeaponFlags)?.SetValue(weapon, false);
        }

        // Host only: attachment/bullet levels are server SyncVars, so this writes them directly.
        public string FullUpgrade()
        {
            if (!IsHost) return "host only";
            if (!(HeldWeapon() is global::Weapon weapon) || weapon.Attachments == null) return "hold a gun";
            var a = weapon.Attachments;
            int bullets = (a.GetType().GetField("_bulletUpgrades", WeaponFlags)?.GetValue(a) as Array)?.Length ?? 0;
            int barrels = (a.GetType().GetField("_barrelAttachments", WeaponFlags)?.GetValue(a) as System.Collections.ICollection)?.Count ?? 0;
            if (bullets > 0) a._syncedBulletIndex.Value = (byte)(bullets - 1);
            if (barrels > 0) a._syncedBarrelAttachment.Value = (byte)(barrels - 1);
            a._syncedExtendedMag.Value = true;
            a._syncedLaserSight.Value = true;
            weapon.SetRecoilSprings();
            RefillMagazine();
            return null;
        }
        public string CycleSight()
        {
            if (!IsHost) return "host only";
            if (!(HeldWeapon() is global::Weapon weapon) || weapon.Attachments == null) return "hold a gun";
            var a = weapon.Attachments;
            int sights = (a.GetType().GetField("_sights", WeaponFlags)?.GetValue(a) as System.Collections.ICollection)?.Count ?? 0;
            if (sights == 0) return "no sights on this gun";
            a._syncedSight.Value = (byte)((a._syncedSight.Value + 1) % sights);
            return null;
        }
        public bool OneShotLobby
        {
            get => global::ServerSettings.Instance != null && global::ServerSettings.OneShotEnabled;
            set { if (IsHost && global::ServerSettings.Instance != null) global::ServerSettings.Instance._useOneShot.Value = value; }
        }
        public void InstantReload()
        {
            if (!(HeldWeapon() is global::Weapon weapon)) return;
            var f = weapon.GetType().GetField("_isReloading", WeaponFlags);
            if (f != null && (bool)f.GetValue(weapon)) { RefillMagazine(); f.SetValue(weapon, false); weapon.GetType().GetField("_queueReload", WeaponFlags)?.SetValue(weapon, false); }
        }
    }
}

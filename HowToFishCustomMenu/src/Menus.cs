using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HowToFishCustomMenu
{
    // Every page of the menu. Rows whose hook needs host authority show a [HOST] tag and
    // explain why when pressed as a client instead of pretending to work.
    public sealed partial class Plugin
    {
        private const string NeedHost = "HOST ONLY - start the lobby yourself";
        private const string NeedPlayer = "LOAD INTO A WORLD FIRST";
        private bool InWorld() => Bridge.Ready;
        private bool IsHostNow() => Host;

        private Menu BuildMenus()
        {
            var m = new Menu("MAIN MENU");
            m.Sub("PLAYER MENU", PlayerMenu);
            m.Sub("FISHING MENU", FishingMenu);
            m.Sub("AIMBOT MENU", AimbotMenu);
            m.Sub("WEAPON & TOOL MENU", WeaponMenu);
            m.Sub("ITEM SPAWNER", ItemMenu);
            m.Sub("TELEPORT MENU", TeleportMenu);
            m.Sub("SKY BASE", SkyBaseMenu);
            m.Sub("VEHICLE MENU", VehicleMenu);
            m.Sub("KRAKEN WATER CAR", CarMenu);
            m.Sub("ISLAND TELEPORT", IslandMenu);
            m.Action("UNLOCK ALL [HOST]", () => Note(UnlockAll()), IsHostNow, NeedHost);
            m.Sub("PLAYERS", PlayerListMenu);
            m.Sub("TROLL MENU", TrollMenu);
            m.Sub("FUN MENU", FunMenu);
            m.Sub("LOBBY MENU", LobbyMenu);
            m.Sub("CASINO MENU", CasinoMenu);
            m.Sub("WORLD MENU", WorldMenu);
            m.Sub("ESP MENU", EspMenu);
            m.Sub("CHAOS MODE", ChaosMenu);
            m.Sub("SETTINGS", SettingsMenu);
            m.Action("RESET EVERYTHING", () => { ResetEverything(); Note("All effects reset"); });
            return m;
        }

        // ---------------- 01 PLAYER ----------------
        private Menu playerMenu;
        private Menu PlayerMenu()
        {
            if (playerMenu != null) return playerMenu;
            var m = playerMenu = new Menu("PLAYER MENU");
            m.Toggle("GOD MODE [HOST]", () => Patches.PersonalGod, v => Patches.PersonalGod = v, IsHostNow, NeedHost);
            m.Toggle("DEMI GOD (AUTO HEAL) [HOST]", () => demiGod, v => demiGod = v, IsHostNow, NeedHost);
            m.Toggle("NEVER HUNGRY [HOST]", () => neverHungry, v => neverHungry = v, IsHostNow, NeedHost);
            m.Toggle("FLY MODE", () => fly, v => { if (v) fly = true; else StopFly(); }, InWorld, NeedPlayer);
            m.Toggle("NO CLIP", () => noClip, v => { if (Bridge.SetNoClip(v)) noClip = v; else Note("Collider hook unavailable"); }, InWorld, NeedPlayer);
            m.Slider("SPEED MULTIPLIER", () => speedMult, v => { speedMult = v; Bridge.SetSpeed(v); }, .5f, 10f, .5f, "0.0x");
            m.Action("SUPER SPEED (4X)", () => { speedMult = 4f; Result("Super speed", Bridge.SetSpeed(4f)); }, InWorld, NeedPlayer);
            m.Action("SUPER SPRINT (8X)", () => { speedMult = 8f; Result("Super sprint", Bridge.SetSpeed(8f)); }, InWorld, NeedPlayer);
            m.Slider("JUMP MULTIPLIER", () => jumpMult, v => { jumpMult = v; Bridge.SetJumpMultiplier(v); }, 1f, 15f, .5f, "0.0x");
            m.Action("SUPER JUMP (5X)", () => { jumpMult = 5f; Result("Super jump", Bridge.SetJumpMultiplier(5f)); }, InWorld, NeedPlayer);
            m.Toggle("INFINITE JUMP", () => airJump, v => airJump = v);
            m.Slider("GRAVITY MULTIPLIER [HOST]", () => gravityMult, SetGravity, 0f, 3f, .1f, "0.0x");
            m.Action("MOON GRAVITY [HOST]", () => { SetGravity(.2f); Note("Moon gravity"); }, IsHostNow, NeedHost);
            m.Action("ZERO GRAVITY [HOST]", () => { SetGravity(0f); Note("Zero gravity"); }, IsHostNow, NeedHost);
            m.Action("NORMAL GRAVITY", () => { SetGravity(1f); Note("Gravity restored"); });
            m.Toggle("WALK ON WATER", () => walkOnWater, v => walkOnWater = v);
            m.Toggle("FREEZE IN MID-AIR", () => freezeAir, v => freezeAir = v);
            m.Toggle("SPINBOT", () => spinbot, v => spinbot = v);
            m.Choice("BODY SHAPE (LOCAL VIEW)", new[] { "NORMAL", "GIANT", "TINY", "SUPER SKINNY", "WIDE" }, () => bodyShape, SetBodyShape);
            m.Action("LAUNCH ME UP", () => Result("Launched", Bridge.LaunchLocal(25f)), InWorld, NeedPlayer);
            m.Action("SUICIDE / RESPAWN [HOST]", () => Result("Respawning", Bridge.KillPlayer(Bridge.LocalPlayerObject)), IsHostNow, NeedHost);
            m.Action("RESET CHARACTER", () =>
            {
                StopFly(); Bridge.SetNoClip(false); noClip = false; SetBodyShape(0);
                speedMult = jumpMult = 1f; Bridge.SetSpeed(1f); Bridge.SetJumpMultiplier(1f);
                airJump = spinbot = freezeAir = walkOnWater = demiGod = false; Patches.PersonalGod = false;
                Note("Character reset");
            });
            return m;
        }

        // ---------------- 02 FISHING ----------------
        private Menu fishingMenu;
        private Menu FishingMenu()
        {
            if (fishingMenu != null) return fishingMenu;
            var m = fishingMenu = new Menu("FISHING MENU");
            m.Action("INSTANT CATCH [HOST]", () => Result("Catch triggered", Bridge.InstantCatch(), "cast into water first"), IsHostNow, NeedHost);
            m.Toggle("AUTO FISH [HOST]", () => autoFish, v => autoFish = v, IsHostNow, NeedHost);
            m.Toggle("UNLIMITED BAIT [HOST]", () => Patches.UnlimitedBait, v => Patches.UnlimitedBait = v, IsHostNow, NeedHost);
            m.Slider("FISH SIZE (SPAWNED)", () => fishScale, v => fishScale = v, .2f, 6f, .2f, "0.0x");
            m.Action("SPAWN RANDOM FISH", () => Note("Spawned " + SpawnFishAround(Bridge.InFront(3f), 1, 0f, 1f) + " fish"), IsHostNow, NeedHost);
            m.Action("SPAWN GIANT FISH", () => Note("Spawned " + SpawnFishAround(Bridge.InFront(5f), 1, 0f, 4f) + " giant fish"), IsHostNow, NeedHost);
            m.Action("SPAWN TINY FISH (x10)", () => Note("Spawned " + SpawnFishAround(Bridge.InFront(3f), 10, 1.5f, .25f) + " tiny fish"), IsHostNow, NeedHost);
            m.Action("SPAWN EVERY FISH", () => { var all = Bridge.SpawnablesWith("Fish").ToList(); foreach (var f in all) QueueSpawn(f, 1, fishScale); Note("Spawning " + all.Count + " fish types"); }, IsHostNow, NeedHost);
            m.Action("FISH RAIN (25)", () => Note("Fish rain: " + SpawnFishAround(Bridge.Player.position + Vector3.up * 18f, 25, 8f, 1f)), IsHostNow, NeedHost);
            m.Toggle("FISH TORNADO", () => fishTornado, v => { fishTornado = v; if (v && Host) SpawnFishAround(Bridge.Player.position + Vector3.up * 2f, 30, 6f, 1f); }, IsHostNow, NeedHost);
            m.Action("FISH ARMY (20 AROUND YOU)", () => Note("Fish army: " + SpawnFishAround(Bridge.Player.position + Vector3.up, 20, 5f, 1.5f)), IsHostNow, NeedHost);
            m.Toggle("FISH MAGNET", () => fishMagnet, v => fishMagnet = v, IsHostNow, NeedHost);
            m.Toggle("FLYING FISH", () => flyingFish, v => flyingFish = v, IsHostNow, NeedHost);
            m.Action("EXPLODING FISH (NEAREST 5)", ExplodeNearbyFish, IsHostNow, NeedHost);
            m.Slider("FISH VALUE MULTIPLIER", () => valueMult, v => valueMult = v, 1f, 50f, 1f, "0x");
            m.Action("APPLY VALUE TO ALL FISH [HOST]", () => Note("Value x" + valueMult + " on " + Bridge.MultiplyWorldValues(valueMult, 1f, true) + " fish"), IsHostNow, NeedHost);
            m.Slider("FISH WEIGHT MULTIPLIER", () => weightMult, v => weightMult = v, .5f, 10f, .5f, "0.0x");
            m.Action("APPLY WEIGHT TO ALL FISH [HOST]", () => Note("Weight x" + weightMult + " on " + Bridge.MultiplyWorldValues(1f, weightMult, true) + " fish"), IsHostNow, NeedHost);
            m.Action("RESIZE ALL FISH (HOST VIEW)", () => Note("Resized " + Bridge.ScaleWorldItems(fishScale, true) + " fish"));
            m.Toggle("FISH CANNON (MIDDLE MOUSE)", () => fishCannon, v => fishCannon = v, IsHostNow, NeedHost);
            return m;
        }
        private void ExplodeNearbyFish()
        {
            var p = Bridge.Player.position;
            var fish = Bridge.World().Where(x => Bridge.IsCreature(x.Value)).OrderBy(x => (x.Key.position - p).sqrMagnitude).Take(5).Select(x => x.Key.position).ToList();
            foreach (var pos in fish) Bridge.ExplodeAt(pos);
            Note(fish.Count + " fish exploded");
        }

        // ---------------- AIMBOT ----------------
        private Menu aimMenu;
        private Menu AimbotMenu()
        {
            if (aimMenu != null) return aimMenu;
            var m = aimMenu = new Menu("AIMBOT MENU");
            m.Toggle("AIMBOT", () => aimEnabled, v => aimEnabled = v);
            m.Toggle("ADS ONLY", () => aimAdsOnly, v => aimAdsOnly = v);
            m.Choice("AIM STYLE", new[] { "SNAP", "SMOOTH" }, () => aimSmooth ? 1 : 0, v => aimSmooth = v == 1);
            m.Toggle("360 AIMBOT", () => aimWide, v => aimWide = v);
            m.Toggle("TARGET PREDICTION", () => aimPrediction, v => aimPrediction = v);
            m.Toggle("SILENT AIM", () => Patches.SilentAim, v => Patches.SilentAim = v);
            m.Toggle("TRIGGERBOT", () => triggerbot, v => triggerbot = v);
            m.Toggle("EXPLOSIVE BULLETS [HOST]", () => Patches.ExplosiveBullets, v => Patches.ExplosiveBullets = v, IsHostNow, NeedHost);
            m.Action("UNFAIR AIMBOT PRESET", () => { aimEnabled = aimWide = aimPrediction = triggerbot = true; aimAdsOnly = aimSmooth = false; Note("UNFAIR AIMBOT ENGAGED"); });
            m.Label("Targets fish & sea creatures only.");
            return m;
        }

        // ---------------- 03 WEAPONS ----------------
        private Menu weaponMenu;
        private Menu WeaponMenu()
        {
            if (weaponMenu != null) return weaponMenu;
            var m = weaponMenu = new Menu("WEAPON & TOOL MENU");
            Func<bool> held = () => Bridge.Ready && Bridge.HasHeldWeapon;
            m.Action("REFILL MAGAZINE", () => Result("Magazine refilled", Bridge.RefillMagazine()), held, "HOLD A WEAPON");
            m.Action("FULL UPGRADE HELD GUN [HOST]", () => { var err = Bridge.FullUpgrade(); Note(err == null ? "Gun fully upgraded: max bullets, best barrel, extended mag, laser" : "Full upgrade // " + err); }, IsHostNow, NeedHost);
            m.Action("CYCLE SIGHT [HOST]", () => { var err = Bridge.CycleSight(); Note(err == null ? "Sight changed" : "Sight // " + err); }, IsHostNow, NeedHost);
            m.Toggle("NO RECOIL", () => noRecoil, v => noRecoil = v);
            m.Slider("DAMAGE MULTIPLIER", () => damageMult, v => damageMult = v, 1f, 50f, 1f, "0x");
            m.Toggle("ONE SHOT KILL LOBBY [HOST]", () => Bridge.OneShotLobby, v => Bridge.OneShotLobby = v, IsHostNow, NeedHost);
            m.Toggle("UNLIMITED AMMO", () => infiniteAmmo, v => infiniteAmmo = v);
            m.Toggle("INSTANT RELOAD", () => instantReload, v => instantReload = v);
            m.Toggle("RAPID FIRE", () => rapidFire, v => rapidFire = v);
            m.Toggle("NO COOLDOWN", () => noCooldown, v => noCooldown = v);
            m.Toggle("ZERO SPREAD", () => zeroSpread, v => zeroSpread = v);
            m.Toggle("FAST PROJECTILES (3X)", () => fastProjectiles, v => fastProjectiles = v);
            m.Toggle("NO SELF KNOCKBACK", () => noWeaponKick, v => noWeaponKick = v);
            m.Toggle("EXPLOSIVE BULLETS [HOST]", () => Patches.ExplosiveBullets, v => Patches.ExplosiveBullets = v, IsHostNow, NeedHost);
            m.Action("GOD GUN (EVERYTHING ON)", () => { infiniteAmmo = rapidFire = noCooldown = zeroSpread = noRecoil = instantReload = true; damageMult = 10f; if (Host) Bridge.FullUpgrade(); Note("GOD GUN ENABLED"); });
            m.Toggle("FISH LAUNCHER (MIDDLE MOUSE)", () => fishCannon, v => fishCannon = v, IsHostNow, NeedHost);
            m.Action("OBJECT CANNON (LAUNCH NEARBY)", () => LaunchNearby(25f), IsHostNow, NeedHost);
            m.Action("SUPER KNOCKBACK BLAST", () => LaunchNearby(60f), IsHostNow, NeedHost);
            m.Action("SPAWN EXPLOSION IN FRONT", () => Result("Boom", Bridge.ExplodeAt(Bridge.InFront(8f))), IsHostNow, NeedHost);
            m.Action("GIVE ALL WEAPONS", () => { var w = Bridge.SpawnablesWith("Weapon").ToList(); foreach (var p in w) QueueSpawn(p, 1); Note("Spawning " + w.Count + " weapons"); }, IsHostNow, NeedHost);
            return m;
        }

        // ---------------- 04 ITEMS ----------------
        private Menu itemMenu, itemList;
        private Menu ItemMenu()
        {
            if (itemMenu != null) return itemMenu;
            var m = itemMenu = new Menu("ITEM SPAWNER");
            m.Sub("SEARCHABLE ITEM LIST", ItemListMenu, IsHostNow, NeedHost);
            m.Choice("QUANTITY", new[] { "1", "10", "100" }, () => spawnQty == 1 ? 0 : spawnQty == 10 ? 1 : 2, v => spawnQty = v == 0 ? 1 : v == 1 ? 10 : 100);
            m.Input("ITEM NAME / ID", () => itemId, v => itemId = v);
            m.Action("SPAWN BY NAME / ID", SpawnById, IsHostNow, NeedHost);
            m.Action("SPAWN RANDOM ITEM", () => { var all = Bridge.Spawnables(); if (all.Count > 0) QueueSpawn(all[UnityEngine.Random.Range(0, all.Count)].Value, spawnQty); }, IsHostNow, NeedHost);
            m.Action("SPAWN ALL ITEMS", () => { var all = Bridge.Spawnables(); foreach (var p in all) QueueSpawn(p.Value, 1); Note("Spawning " + all.Count + " items"); }, IsHostNow, NeedHost);
            m.Action("GIVE ALL FISHING RODS", () => { var r = Bridge.SpawnablesWith("FishingRod").ToList(); foreach (var p in r) QueueSpawn(p, 1); Note("Spawning " + r.Count + " rods"); }, IsHostNow, NeedHost);
            m.Action("GIVE ALL EQUIPMENT", () => { var r = Bridge.SpawnablesWith("Tool").Concat(Bridge.SpawnablesWith("Weapon")).Concat(Bridge.SpawnablesWith("Melee")).Distinct().ToList(); foreach (var p in r) QueueSpawn(p, 1); Note("Spawning " + r.Count + " tools"); }, IsHostNow, NeedHost);
            m.Action("DUPLICATE HELD ITEM", () => Result("Duplicated", Bridge.DuplicateHeld(), "hold an item"), IsHostNow, NeedHost);
            m.Action("DELETE HELD ITEM", () => Result("Deleted", Bridge.DeleteHeld(), "hold an item"), IsHostNow, NeedHost);
            m.Action("DELETE NEARBY ITEMS (15M)", () => Note("Deleted " + Bridge.DeleteNearbyItems(15f) + " items"), IsHostNow, NeedHost);
            m.Slider("ITEM SIZE (SPAWNED)", () => itemScale, v => itemScale = v, .2f, 6f, .2f, "0.0x");
            m.Action("APPLY VALUE MULTIPLIER TO ALL ITEMS", () => Note("Value x" + valueMult + " on " + Bridge.MultiplyWorldValues(valueMult, 1f, false) + " items"), IsHostNow, NeedHost);
            m.Toggle("FLOATING ITEMS", () => floatingItems, v => floatingItems = v, IsHostNow, NeedHost);
            m.Action("ITEM RAIN (20)", () =>
            {
                var all = Bridge.Spawnables(); var p = Bridge.Player.position;
                for (int i = 0; i < 20; i++) { var pre = all[UnityEngine.Random.Range(0, all.Count)].Value; var pos = p + new Vector3(UnityEngine.Random.Range(-8f, 8f), 18f + i * .3f, UnityEngine.Random.Range(-8f, 8f)); spawnQueue.Enqueue(() => Bridge.SpawnPrefab(pre, pos, Quaternion.identity, itemScale)); }
                Note("ITEM RAIN");
            }, IsHostNow, NeedHost);
            return m;
        }
        private void SpawnById()
        {
            var all = Bridge.Spawnables();
            string key = itemId.Replace(" ", "").ToLowerInvariant();
            object prefab = all.FirstOrDefault(p => p.Key == key).Value ?? all.FirstOrDefault(p => p.Key.Contains(key)).Value;
            if (prefab == null && byte.TryParse(itemId, out byte id)) prefab = all.Select(p => p.Value).FirstOrDefault(v => v is Component c && Equals(c.GetType().GetProperty("ID")?.GetValue(c, null), id));
            if (prefab == null) { Note("No item matches '" + itemId + "'"); return; }
            QueueSpawn(prefab, spawnQty);
            Note("Spawning " + spawnQty + " x " + ((Component)prefab).name);
        }
        private Menu ItemListMenu()
        {
            if (itemList != null) return itemList;
            var m = itemList = new Menu("ITEM LIST");
            var search = new Option { Kind = OptionKind.Input, Name = "SEARCH", GetText = () => itemSearch, SetText = v => itemSearch = v };
            List<KeyValuePair<string, object>> cache = null;
            m.Dynamic = () =>
            {
                if (cache == null || cache.Count == 0) cache = Bridge.Spawnables();
                var rows = new List<Option> { search };
                string q = itemSearch.Replace(" ", "").ToLowerInvariant();
                foreach (var p in cache)
                {
                    if (q.Length > 0 && !p.Key.Contains(q)) continue;
                    var prefab = p.Value;
                    rows.Add(new Option { Kind = OptionKind.Action, Name = p.Key.ToUpperInvariant(), OnSelect = () => { QueueSpawn(prefab, spawnQty); Note("Spawning " + spawnQty + " x " + p.Key); } });
                }
                if (rows.Count == 1) rows.Add(new Option { Kind = OptionKind.Label, Name = cache.Count == 0 ? "Item list not loaded yet" : "No matches" });
                return rows;
            };
            m.Footer = "SPACE ON SEARCH TO TYPE";
            return m;
        }

        // ---------------- 05 TELEPORT ----------------
        private Menu teleportMenu;
        private Menu TeleportMenu()
        {
            if (teleportMenu != null) return teleportMenu;
            var m = teleportMenu = new Menu("TELEPORT MENU");
            m.Add(new Option { Kind = OptionKind.Action, NameFn = () => "SAVE LOCATION  [" + keySave.Value.ToString().ToUpper() + "]", OnSelect = SaveQuick, Available = InWorld, Requirement = NeedPlayer });
            m.Add(new Option { Kind = OptionKind.Action, NameFn = () => "LOAD LOCATION  [" + keyLoad.Value.ToString().ToUpper() + "]", OnSelect = LoadQuick, Available = InWorld, Requirement = NeedPlayer });
            m.Sub("SAVED LOCATIONS", LocationsMenu);
            m.Sub("SKY BASE", SkyBaseMenu);
            m.Sub("ISLAND TELEPORT", IslandMenu);
            m.Sub("RACE TRACK", TrackMenu);
            m.Action("TELEPORT TO SPAWN / ISLAND", () => Go(Bridge.IslandCentre.HasValue ? Bridge.Grounded(Bridge.IslandCentre.Value) : (Vector3?)null, "Island"), InWorld, NeedPlayer);
            m.Action("TELEPORT TO WATER", () => Go(Bridge.OpenWater(90f), "Open water"), InWorld, NeedPlayer);
            m.Action("TELEPORT TO FISHING SPOT", () => Go(Bridge.OpenWater(45f), "Fishing spot"), InWorld, NeedPlayer);
            m.Action("TELEPORT TO SHOP", () => Go(Bridge.Shop.HasValue ? Bridge.Shop.Value + Vector3.up * 1.5f + Vector3.forward * 2f : (Vector3?)null, "Shop"), InWorld, NeedPlayer);
            m.Action("TELEPORT TO CASINO", () => Go(Bridge.Casino.HasValue ? Bridge.Grounded(Bridge.Casino.Value + Vector3.forward * 3f) : (Vector3?)null, "Casino"), InWorld, NeedPlayer);
            m.Action("TELEPORT TO BOAT", () => Go(Bridge.BoatDriverSeat + Vector3.up, "Boat"), InWorld, NeedPlayer);
            m.Sub("TELEPORT TO NPC", NpcMenu, InWorld, NeedPlayer);
            m.Action("TELEPORT TO SELECTED PLAYER", () => Go(Bridge.PlayerTransform(target)?.position + Vector3.up, "Player"), () => target != null, "SELECT A PLAYER IN PLAYERS");
            m.Action("TELEPORT RANDOM LOCATION", () => { var c = Bridge.IslandCentre ?? Bridge.Player.position; Go(Bridge.Grounded(c + new Vector3(UnityEngine.Random.Range(-80f, 80f), 0, UnityEngine.Random.Range(-80f, 80f))), "Random"); }, InWorld, NeedPlayer);
            m.Action("TELEPORT 100M UP", () => Go(Bridge.Player.position + Vector3.up * 100f, "Sky"), InWorld, NeedPlayer);
            m.Action("TELEPORT UNDER MAP", () => Go(Bridge.Player.position + Vector3.down * 40f, "Under map"), InWorld, NeedPlayer);
            m.Action("BLINK FORWARD 20M", () => Go(Bridge.InFront(20f), "Blink"), InWorld, NeedPlayer);
            m.Input("X", () => coordX, v => coordX = v);
            m.Input("Y", () => coordY, v => coordY = v);
            m.Input("Z", () => coordZ, v => coordZ = v);
            m.Action("TELEPORT TO COORDINATES", () =>
            {
                if (float.TryParse(coordX, out float x) && float.TryParse(coordY, out float y) && float.TryParse(coordZ, out float z)) Go(new Vector3(x, y, z), "Coordinates");
                else Note("Invalid coordinates");
            }, InWorld, NeedPlayer);
            m.Action("SHOW MY COORDINATES", () => { var p = Bridge.Player.position; coordX = p.x.ToString("0"); coordY = p.y.ToString("0"); coordZ = p.z.ToString("0"); Note("You are at " + coordX + ", " + coordY + ", " + coordZ); }, InWorld, NeedPlayer);
            return m;
        }
        private void Go(Vector3? pos, string where)
        {
            if (!pos.HasValue) { Note(where + " not found on this island"); return; }
            if (driving) ExitCar(); // the car would otherwise pull you back into the seat
            Result("Teleported: " + where, Bridge.Teleport(pos.Value));
        }
        private Menu NpcMenu()
        {
            var m = new Menu("NPCS");
            m.Dynamic = () =>
            {
                var rows = new List<Option>();
                foreach (var n in Bridge.Npcs()) { var pos = n.Value; rows.Add(new Option { Kind = OptionKind.Action, Name = n.Key.ToUpperInvariant(), OnSelect = () => Go(pos + Vector3.forward * 1.5f + Vector3.up, n.Key) }); }
                if (rows.Count == 0) rows.Add(new Option { Kind = OptionKind.Label, Name = "No NPCs loaded here" });
                return rows;
            };
            return m;
        }

        // ---------------- 06 VEHICLE ----------------
        private Menu vehicleMenu;
        private Menu VehicleMenu()
        {
            if (vehicleMenu != null) return vehicleMenu;
            var m = vehicleMenu = new Menu("VEHICLE MENU");
            Func<bool> boat = () => Host && Bridge.BoatBody != null;
            const string needBoat = "HOST + BOAT MUST EXIST";
            m.Sub("KRAKEN WATER CAR", CarMenu);
            m.Toggle("SUPER BOAT SPEED (HOLD W)", () => boatBoost, v => boatBoost = v, boat, needBoat);
            m.Slider("BOAT SPEED MULTIPLIER", () => boatMult, v => boatMult = v, 1.5f, 12f, .5f, "0.0x");
            m.Action("BOAT SPEED BOOST (BURST)", () => Result("Boost!", Bridge.BoatImpulse(Bridge.BoatBody.transform.forward * 30f, Vector3.zero)), boat, needBoat);
            m.Toggle("FLYING BOAT (JUMP/CTRL)", () => boatFlying, v => { if (Bridge.SetBoatFlying(v)) boatFlying = v; }, boat, needBoat);
            m.Toggle("DRIVE ON LAND (HOLD W)", () => driveOnLand, v => driveOnLand = v, boat, needBoat);
            m.Action("BOAT JUMP", () => Result("Boat jump", Bridge.BoatImpulse(Vector3.up * 14f, Vector3.zero)), boat, needBoat);
            m.Action("LAUNCH BOAT", () => Result("Boat launched", Bridge.BoatImpulse(Vector3.up * 45f, Vector3.zero)), boat, needBoat);
            m.Action("FLIP BOAT", () => Result("Boat flipped", Bridge.BoatImpulse(Vector3.up * 8f, Bridge.BoatBody.transform.forward * 8f)), boat, needBoat);
            m.Toggle("BOAT SPIN", () => boatSpin, v => boatSpin = v, boat, needBoat);
            m.Action("TELEPORT BOAT TO ME", () => Result("Boat moved", Bridge.MoveBoat(Bridge.OpenWater(Vector3.Distance(Bridge.Player.position, Bridge.IslandCentre ?? Vector3.zero) + 6f), Quaternion.identity)), boat, needBoat);
            m.Action("TELEPORT TO BOAT", () => Go(Bridge.BoatDriverSeat + Vector3.up, "Boat"), InWorld, NeedPlayer);
            m.Toggle("RAINBOW BOAT (LOCAL VIEW)", () => rainbowBoat, v => rainbowBoat = v);
            m.Slider("BOAT SIZE (LOCAL VIEW)", () => boatScale, v => { boatScale = v; if (Bridge.BoatVisual != null) Bridge.BoatVisual.localScale = Vector3.one * v; }, .2f, 5f, .2f, "0.0x");
            m.Action("INVISIBLE BOAT (LOCAL VIEW)", () => { var b = Bridge.BoatVisual; if (b == null) return; foreach (var r in b.GetComponentsInChildren<Renderer>()) r.enabled = !r.enabled; });
            m.Label("Boat physics run on the host; clients see the result.");
            return m;
        }

        // ---------------- 07 PLAYER LIST ----------------
        private Menu PlayerListMenu()
        {
            var m = new Menu("PLAYERS");
            m.Dynamic = () =>
            {
                var rows = new List<Option>();
                foreach (var p in Bridge.Players())
                {
                    var player = p;
                    string tag = Bridge.IsLocal(p) ? " [YOU]" : "";
                    rows.Add(new Option { Kind = OptionKind.Submenu, Name = Bridge.PlayerName(p).ToUpperInvariant() + tag, Open = () => { target = player; return PlayerOptions(player); } });
                }
                if (rows.Count == 0) rows.Add(new Option { Kind = OptionKind.Label, Name = "No players - load into a lobby" });
                return rows;
            };
            m.Footer = "SELECT A PLAYER TO OPEN PLAYER MODS";
            return m;
        }
        private Menu PlayerOptions(object p)
        {
            var m = new Menu(Bridge.PlayerName(p).ToUpperInvariant());
            m.Add(new Option { Kind = OptionKind.Action, Name = "PLAYER INFORMATION", OnSelect = () =>
            {
                var t = Bridge.PlayerTransform(p);
                Note(Bridge.PlayerName(p) + " // HP " + Bridge.PlayerHealth(p) + (t != null ? " // " + Vector3.Distance(t.position, Bridge.Player.position).ToString("0") + "m away" : ""));
            } });
            AddPlayerActions(m, () => p);
            return m;
        }
        // Shared by the per-player page and the troll menu (which uses the selected target).
        private void AddPlayerActions(Menu m, Func<object> who)
        {
            Func<Vector3> at = () => Bridge.PlayerTransform(who())?.position ?? Vector3.zero;
            m.Action("TELEPORT TO PLAYER", () => Go(at() + Vector3.up + Vector3.right, "Player"), InWorld, NeedPlayer);
            m.Action("TELEPORT PLAYER TO ME [HOST]", () => Result("Brought player", Bridge.TeleportPlayer(who(), Bridge.InFront(2f))), IsHostNow, NeedHost);
            m.Action("HEAL PLAYER [HOST]", () => Result("Healed", Bridge.HealPlayer(who())), IsHostNow, NeedHost);
            m.Toggle("HEAL AURA [HOST]", () => healAura.Contains(who()), v => Set(healAura, who(), v), IsHostNow, NeedHost);
            m.Action("KILL PLAYER [HOST]", () => Result("Killed", Bridge.KillPlayer(who())), IsHostNow, NeedHost);
            m.Action("LAUNCH PLAYER", () => Result("Launched", Bridge.KnockPlayer(who(), Vector3.up * 30f)), IsHostNow, NeedHost);
            m.Action("LAUNCH INTO SPACE", () => Result("To space!", Bridge.KnockPlayer(who(), Vector3.up * 150f)), IsHostNow, NeedHost);
            m.Action("LAUNCH INTO WATER", () => Result("Splash", Bridge.TeleportPlayer(who(), Bridge.OpenWater(70f) + Vector3.up * 25f)), IsHostNow, NeedHost);
            m.Action("TELEPORT PLAYER INTO SKY", () => Result("Sky", Bridge.TeleportPlayer(who(), at() + Vector3.up * 80f)), IsHostNow, NeedHost);
            m.Action("RANDOM TELEPORT", () => { var c = Bridge.IslandCentre ?? at(); Result("Random tp", Bridge.TeleportPlayer(who(), Bridge.Grounded(c + new Vector3(UnityEngine.Random.Range(-70f, 70f), 0, UnityEngine.Random.Range(-70f, 70f))))); }, IsHostNow, NeedHost);
            m.Toggle("FREEZE PLAYER", () => freezePos.ContainsKey(who()), v => { if (v) freezePos[who()] = at(); else freezePos.Remove(who()); }, IsHostNow, NeedHost);
            m.Toggle("SPIN PLAYER", () => spinning.Contains(who()), v => Set(spinning, who(), v), IsHostNow, NeedHost);
            m.Toggle("MAKE PLAYER BOUNCE", () => bouncing.Contains(who()), v => Set(bouncing, who(), v), IsHostNow, NeedHost);
            m.Toggle("FISH RAIN ON PLAYER", () => rainOn.Contains(who()), v => Set(rainOn, who(), v), IsHostNow, NeedHost);
            m.Action("FISH PRISON", () => { var c = at(); SpawnFishAround(c, 10, 2.2f, 3.5f); SpawnFishAround(c + Vector3.up * 2.5f, 8, 2.2f, 3.5f); Note("FISH PRISON DEPLOYED"); }, IsHostNow, NeedHost);
            m.Action("SPAWN FISH AROUND PLAYER", () => Note("Spawned " + SpawnFishAround(at() + Vector3.up, 12, 3f, 1f)), IsHostNow, NeedHost);
            m.Action("GIANT FISH BEHIND PLAYER", () => { var t = Bridge.PlayerTransform(who()); if (t != null) SpawnFishAround(t.position - t.forward * 4f + Vector3.up * 2f, 1, 0f, 6f); }, IsHostNow, NeedHost);
            m.Action("FISH PILE (TP INTO FISH)", () => { var c = Bridge.Player.position + Bridge.Player.forward * 8f; SpawnFishAround(c + Vector3.up * 2f, 25, 1.2f, 1f); Bridge.TeleportPlayer(who(), c + Vector3.up * 4f); }, IsHostNow, NeedHost);
            m.Action("SPAWN OBJECTS AROUND PLAYER", () =>
            {
                var all = Bridge.Spawnables(); var c = at();
                for (int i = 0; i < 10; i++) { float a = i * .628f; Bridge.SpawnPrefab(all[UnityEngine.Random.Range(0, all.Count)].Value, c + new Vector3(Mathf.Cos(a) * 3f, 1.5f, Mathf.Sin(a) * 3f), Quaternion.identity); }
            }, IsHostNow, NeedHost);
            m.Action("EXPLOSION ON PLAYER", () => Result("Boom", Bridge.ExplodeAt(at())), IsHostNow, NeedHost);
            m.Action("CONFETTI / FISH EXPLOSION", () => { var c = at(); SpawnFishAround(c + Vector3.up * 3f, 16, .5f, .6f); foreach (var x in Bridge.World().Where(x => Bridge.IsCreature(x.Value) && (x.Key.position - c).sqrMagnitude < 25f)) x.Key.GetComponent<Rigidbody>()?.AddForce(UnityEngine.Random.onUnitSphere * 15f + Vector3.up * 10f, ForceMode.VelocityChange); }, IsHostNow, NeedHost);
            m.Action("GIVE PLAYER MONEY (SHARED WALLET)", () => Result("+$10,000", Bridge.AddMoney(10000)), IsHostNow, NeedHost);
            m.Action("GIVE PLAYER RANDOM ITEM", () => { var all = Bridge.Spawnables(); Bridge.SpawnPrefab(all[UnityEngine.Random.Range(0, all.Count)].Value, at() + Vector3.up * 2f, Quaternion.identity); Note("Item dropped on player"); }, IsHostNow, NeedHost);
            m.Action("CLEAR EFFECTS ON PLAYER", () => { var p = who(); freezePos.Remove(p); spinning.Remove(p); bouncing.Remove(p); rainOn.Remove(p); healAura.Remove(p); Note("Effects cleared"); });
        }
        private static void Set(HashSet<object> set, object p, bool on) { if (p == null) return; if (on) set.Add(p); else set.Remove(p); }

        // ---------------- 08 TROLL ----------------
        private Menu trollMenu;
        private Menu TrollMenu()
        {
            if (trollMenu != null) return trollMenu;
            var m = trollMenu = new Menu("TROLL MENU");
            m.Add(new Option { Kind = OptionKind.Action, NameFn = () => "TARGET: " + (target != null ? Bridge.PlayerName(target).ToUpperInvariant() : "NONE"), OnSelect = CycleTarget });
            AddPlayerActions(m, () => target);
            m.Footer = "SPACE ON TARGET TO CYCLE PLAYERS";
            return m;
        }
        private void CycleTarget()
        {
            var list = Bridge.Players();
            if (list.Count == 0) { Note("No players"); return; }
            int i = list.IndexOf(target);
            target = list[(i + 1) % list.Count];
            Note("Target: " + Bridge.PlayerName(target));
        }

        // ---------------- 09 FUN ----------------
        private Menu funMenu;
        private Menu FunMenu()
        {
            if (funMenu != null) return funMenu;
            var m = funMenu = new Menu("FUN MENU");
            m.Toggle("DISCO MODE", () => disco, v => disco = v);
            m.Toggle("RAINBOW WORLD", () => rainbowWorld, v => { SetFog(null, null); rainbowWorld = v; if (!v) RestoreWorldVisuals(); });
            m.Toggle("RAINBOW MOD MENU", () => rainbowMenu, v => rainbowMenu = v);
            m.Toggle("EVERYTHING FLOATS [HOST]", () => floatingItems, v => floatingItems = v, IsHostNow, NeedHost);
            m.Toggle("EVERYTHING SPINS [HOST]", () => spinningItems, v => spinningItems = v, IsHostNow, NeedHost);
            m.Toggle("EVERYTHING BOUNCES [HOST]", () => bouncingItems, v => bouncingItems = v, IsHostNow, NeedHost);
            m.Action("GIANT EVERYTHING (HOST VIEW)", () => Note("Scaled " + Bridge.ScaleWorldItems(3f, false)));
            m.Action("TINY EVERYTHING (HOST VIEW)", () => Note("Scaled " + Bridge.ScaleWorldItems(.3f, false)));
            m.Action("FISH APOCALYPSE", () => { var c = Bridge.Player.position; SpawnFishAround(c + Vector3.up * 3f, 20, 12f, 4f); SpawnFishAround(c + Vector3.up * 20f, 20, 8f, 1f); Note("FISH APOCALYPSE"); }, IsHostNow, NeedHost);
            m.Slider("TIME SCALE", () => timeScale, SetTimeScale, .1f, 3f, .1f, "0.0x");
            m.Action("SUPER SLOW MOTION", () => SetTimeScale(.2f));
            m.Action("FAST FORWARD", () => SetTimeScale(2.5f));
            m.Toggle("DRUNK CAMERA", () => drunkCam, v => drunkCam = v);
            m.Toggle("UPSIDE DOWN CAMERA", () => upsideDown, v => upsideDown = v);
            m.Toggle("SPINNING CAMERA", () => cameraSpin, v => cameraSpin = v);
            m.Action("FISHEYE CAMERA (FOV 140)", () => fov = 140f);
            m.Action("EXTREME ZOOM (FOV 20)", () => fov = 20f);
            m.Slider("FIELD OF VIEW", () => fov > 0 ? fov : 70f, v => fov = v, 20f, 160f, 5f, "0");
            m.Action("RESET CAMERA", () => { fov = -1f; if (fovCamera != null && originalFov > 0) fovCamera.fieldOfView = originalFov; drunkCam = upsideDown = cameraSpin = false; });
            return m;
        }

        // ---------------- 10 LOBBY ----------------
        private Menu lobbyMenu;
        private Menu LobbyMenu()
        {
            if (lobbyMenu != null) return lobbyMenu;
            var m = lobbyMenu = new Menu("LOBBY MENU");
            m.Toggle("GOD MODE LOBBY", () => Bridge.GodLobbyActive, v => Bridge.SetGodLobby(v), IsHostNow, NeedHost);
            m.Action("TELEPORT ALL PLAYERS TO ME", () => ForAll((p, i) => Bridge.TeleportPlayer(p, Bridge.Player.position + new Vector3(Mathf.Cos(i) * 2.5f, 1f, Mathf.Sin(i) * 2.5f))), IsHostNow, NeedHost);
            m.Action("HEAL LOBBY", () => ForAll((p, i) => Bridge.HealPlayer(p)), IsHostNow, NeedHost);
            m.Action("LAUNCH LOBBY", () => ForAll((p, i) => Bridge.KnockPlayer(p, Vector3.up * 35f)), IsHostNow, NeedHost);
            m.Action("MOON GRAVITY LOBBY (OBJECTS)", () => SetGravity(.2f), IsHostNow, NeedHost);
            m.Action("FISH RAIN LOBBY", () => ForAll((p, i) => SpawnFishAround(Bridge.PlayerTransform(p).position + Vector3.up * 16f, 8, 6f, 1f) > 0), IsHostNow, NeedHost);
            m.Action("GIANT FISH LOBBY", () => ForAll((p, i) => SpawnFishAround(Bridge.PlayerTransform(p).position + Vector3.up * 4f, 2, 5f, 5f) > 0), IsHostNow, NeedHost);
            m.Action("UNLIMITED MONEY LOBBY", () => { unlimitedMoney = true; Bridge.SetMoney(9999999); Note("Shared wallet maxed"); }, IsHostNow, NeedHost);
            m.Action("UNLIMITED ITEMS LOBBY", () => ForAll((p, i) => { var all = Bridge.Spawnables(); for (int k = 0; k < 5; k++) Bridge.SpawnPrefab(all[UnityEngine.Random.Range(0, all.Count)].Value, Bridge.PlayerTransform(p).position + Vector3.up * (2 + k * .4f), Quaternion.identity); return true; }), IsHostNow, NeedHost);
            m.Toggle("BOUNCE LOBBY", () => bouncing.Count > 0, v => ForAllSet(bouncing, v), IsHostNow, NeedHost);
            m.Toggle("FISH RAIN LOBBY (CONTINUOUS)", () => rainOn.Count > 0, v => ForAllSet(rainOn, v), IsHostNow, NeedHost);
            m.Action("RANDOM TELEPORT LOBBY", () => { var c = Bridge.IslandCentre ?? Bridge.Player.position; ForAll((p, i) => Bridge.TeleportPlayer(p, Bridge.Grounded(c + new Vector3(UnityEngine.Random.Range(-70f, 70f), 0, UnityEngine.Random.Range(-70f, 70f))))); }, IsHostNow, NeedHost);
            m.Action("FISH APOCALYPSE LOBBY", () => ForAll((p, i) => SpawnFishAround(Bridge.PlayerTransform(p).position + Vector3.up * 3f, 10, 9f, 3f) > 0), IsHostNow, NeedHost);
            m.Action("CHAOS LOBBY", () => { chaosOn = true; chaosPlayers = chaosFish = chaosWorld = true; nextChaos = 0; Note("CHAOS LOBBY STARTED"); }, IsHostNow, NeedHost);
            m.Action("RESET LOBBY EFFECTS", () => { freezePos.Clear(); spinning.Clear(); bouncing.Clear(); rainOn.Clear(); healAura.Clear(); unlimitedMoney = false; SetGravity(1f); Bridge.SetGodLobby(false); chaosOn = false; Note("Lobby effects reset"); });
            m.Label("Players need nothing installed for these.");
            return m;
        }
        private void ForAll(Func<object, int, bool> act)
        {
            int n = 0, i = 0;
            foreach (var p in Bridge.Players()) { try { if (act(p, i++)) n++; } catch (Exception ex) { Logger.LogWarning(ex.Message); } }
            Note("Applied to " + n + " player(s)");
        }
        private void ForAllSet(HashSet<object> set, bool on)
        {
            if (!on) { set.Clear(); return; }
            foreach (var p in Bridge.Players()) if (!Bridge.IsLocal(p)) set.Add(p);
        }

        // ---------------- 11 CASINO ----------------
        private Menu casinoMenu;
        private Menu CasinoMenu()
        {
            if (casinoMenu != null) return casinoMenu;
            var m = casinoMenu = new Menu("CASINO MENU");
            m.Input("AMOUNT", () => money, v => money = v);
            m.Action("SET MONEY", () => { bool ok = int.TryParse(money, out int a) && Bridge.SetMoney(a); Result("Money set: $" + Bridge.MoneyValue, ok, "enter a whole number / host only"); }, IsHostNow, NeedHost);
            m.Action("ADD MONEY", () => { bool ok = int.TryParse(money, out int a) && Bridge.AddMoney(a); Result("Money now: $" + Bridge.MoneyValue, ok, "enter a whole number / host only"); }, IsHostNow, NeedHost);
            m.Toggle("UNLIMITED MONEY", () => unlimitedMoney, v => unlimitedMoney = v, IsHostNow, NeedHost);
            m.Choice("FORCE ROULETTE", new[] { "OFF", "BLACK", "RED", "GREEN (35X)" }, () => Patches.ForcedRoulette + 1, v => Patches.ForcedRoulette = v - 1);
            m.Toggle("GUARANTEED WIN", () => Patches.GuaranteedWin, v => Patches.GuaranteedWin = v);
            m.Slider("MULTIPLY WINNINGS", () => Patches.WinningsMultiplier, v => Patches.WinningsMultiplier = v, 1f, 20f, 1f, "0x");
            m.Action("JACKPOT MODE", () => { Patches.ForcedRoulette = 2; Patches.WinningsMultiplier = 10f; Note("JACKPOT: bet GREEN"); });
            m.Action("CASINO CHAOS (RANDOM FORCE)", () => { Patches.ForcedRoulette = UnityEngine.Random.Range(0, 3); Note("Next result rigged: " + new[] { "BLACK", "RED", "GREEN" }[Patches.ForcedRoulette]); });
            m.Action("TELEPORT TO CASINO", () => Go(Bridge.Casino.HasValue ? Bridge.Grounded(Bridge.Casino.Value + Vector3.forward * 3f) : (Vector3?)null, "Casino"), InWorld, NeedPlayer);
            m.Label("Roulette rigging applies when YOU host.");
            return m;
        }

        // ---------------- 12 WORLD ----------------
        private Menu worldMenu;
        private Menu WorldMenu()
        {
            if (worldMenu != null) return worldMenu;
            var m = worldMenu = new Menu("WORLD MENU");
            m.Slider("WORLD GRAVITY [HOST]", () => gravityMult, SetGravity, 0f, 3f, .1f, "0.0x");
            m.Action("FREEZE TIME", () => SetTimeScale(timeScale > .01f ? 0f : 1f));
            m.Action("SPEED UP PHYSICS", () => SetTimeScale(2f));
            m.Action("NORMAL TIME", () => SetTimeScale(1f));
            m.Toggle("NO FOG", () => noFog, v => SetFog(v, null));
            m.Toggle("SUPER FOG", () => superFog, v => SetFog(null, v));
            m.Choice("TIME OF DAY (LOCAL)", new[] { "DEFAULT", "DAY", "SUNRISE", "SUNSET", "NIGHT" }, () => timeOfDay, SetTimeOfDay);
            m.Action("SPAWN RANDOM OBJECTS (10)", () => { var all = Bridge.Spawnables(); for (int i = 0; i < 10; i++) QueueSpawn(all[UnityEngine.Random.Range(0, all.Count)].Value, 1); }, IsHostNow, NeedHost);
            m.Action("DELETE NEARBY OBJECTS", () => Note("Deleted " + Bridge.DeleteNearbyItems(15f)), IsHostNow, NeedHost);
            m.Slider("OBJECT SIZE (SPAWNED)", () => itemScale, v => itemScale = v, .2f, 6f, .2f, "0.0x");
            m.Action("LAUNCH NEARBY OBJECTS", () => LaunchNearby(25f), IsHostNow, NeedHost);
            m.Action("RESET WORLD", () => { SetGravity(1f); SetTimeScale(1f); RestoreWorldVisuals(); SetTimeOfDay(0); });
            return m;
        }
        private int timeOfDay; private Light sun; private Quaternion sunRot; private float sunIntensity; private Color sunColour;
        private void SetTimeOfDay(int v)
        {
            if (sun == null)
            {
                sun = RenderSettings.sun ?? FindObjectsOfType<Light>().FirstOrDefault(l => l.type == LightType.Directional);
                if (sun == null) { Note("No sun light found"); return; }
                sunRot = sun.transform.rotation; sunIntensity = sun.intensity; sunColour = sun.color;
            }
            timeOfDay = v;
            float[] pitch = { 0, 60, 8, 8, -30 };
            Color[] col = { sunColour, Color.white, new Color(1f, .6f, .4f), new Color(1f, .45f, .25f), new Color(.3f, .35f, .6f) };
            if (v == 0) { sun.transform.rotation = sunRot; sun.intensity = sunIntensity; sun.color = sunColour; return; }
            sun.transform.rotation = Quaternion.Euler(pitch[v], v == 3 ? 250f : 70f, 0f);
            sun.intensity = v == 4 ? .08f : sunIntensity;
            sun.color = col[v];
        }

        // ---------------- 13 ESP ----------------
        private Menu espMenu;
        private Menu EspMenu()
        {
            if (espMenu != null) return espMenu;
            var m = espMenu = new Menu("ESP MENU");
            m.Toggle("PLAYER ESP / NAME TAGS", () => espPlayers, v => espPlayers = v);
            m.Toggle("FISH ESP", () => espFish, v => espFish = v);
            m.Toggle("ITEM ESP", () => espItems, v => espItems = v);
            m.Toggle("VALUABLE ESP ($100+)", () => espValuable, v => espValuable = v);
            m.Toggle("ESP BOXES", () => espBoxes, v => espBoxes = v);
            m.Toggle("ESP LINES (TRACERS)", () => espTracers, v => espTracers = v);
            m.Choice("LINE START", new[] { "BOTTOM", "CENTRE", "TOP" }, () => tracerOrigin, v => tracerOrigin = v);
            m.Toggle("DISTANCE", () => espDistance, v => espDistance = v);
            m.Toggle("RAINBOW ESP", () => espRainbow, v => espRainbow = v);
            m.Slider("ESP RANGE", () => espRange, v => espRange = v, 25f, 500f, 25f, "0m");
            return m;
        }

        // ---------------- 14 CHAOS ----------------
        private bool chaosOn, chaosPlayers = true, chaosWorld = true, chaosFish = true, chaosExtreme;
        private float chaosInterval = 10f, nextChaos;
        private int chaosCount;
        private string chaosEvent = "";
        private Menu chaosMenu;
        private Menu ChaosMenu()
        {
            if (chaosMenu != null) return chaosMenu;
            var m = chaosMenu = new Menu("CHAOS MODE");
            m.Action("START CHAOS", () => { chaosOn = true; nextChaos = 0; Note("CHAOS MODE STARTED"); });
            m.Action("STOP CHAOS", () => { chaosOn = false; ResetEverything(); Note("Chaos stopped"); });
            m.Slider("INTERVAL", () => chaosInterval, v => chaosInterval = v, 3f, 60f, 1f, "0 sec");
            m.Toggle("RANDOM PLAYER EFFECTS", () => chaosPlayers, v => chaosPlayers = v);
            m.Toggle("RANDOM WORLD EFFECTS", () => chaosWorld, v => chaosWorld = v);
            m.Toggle("RANDOM FISH EFFECTS", () => chaosFish, v => chaosFish = v);
            m.Toggle("EXTREME CHAOS", () => chaosExtreme, v => chaosExtreme = v);
            m.Action("RESET EVERYTHING", () => { ResetEverything(); Note("Everything reset"); });
            m.Footer = "WARNING: ABSOLUTE FISH MAYHEM";
            return m;
        }
        private void TickChaos()
        {
            nextChaos = Time.unscaledTime + chaosInterval;
            var events = new List<KeyValuePair<string, Action>>();
            bool host = Host;
            if (chaosFish && host)
            {
                events.Add(new KeyValuePair<string, Action>("FISH RAIN", () => ForAll((p, i) => SpawnFishAround(Bridge.PlayerTransform(p).position + Vector3.up * 16f, chaosExtreme ? 20 : 8, 7f, 1f) > 0)));
                events.Add(new KeyValuePair<string, Action>("FISH TORNADO", () => { fishTornado = true; SpawnFishAround(Bridge.Player.position + Vector3.up * 2f, 25, 6f, 1f); }));
                events.Add(new KeyValuePair<string, Action>("FISH APOCALYPSE", () => SpawnFishAround(Bridge.Player.position + Vector3.up * 3f, chaosExtreme ? 25 : 12, 12f, 4f)));
                events.Add(new KeyValuePair<string, Action>("FLYING FISH", () => flyingFish = true));
            }
            if (chaosPlayers && host)
            {
                events.Add(new KeyValuePair<string, Action>("MOON GRAVITY", () => { SetGravity(.2f); ForAll((p, i) => Bridge.KnockPlayer(p, Vector3.up * 12f)); }));
                events.Add(new KeyValuePair<string, Action>("LOBBY LAUNCH", () => ForAll((p, i) => Bridge.KnockPlayer(p, Vector3.up * (chaosExtreme ? 80f : 35f)))));
                events.Add(new KeyValuePair<string, Action>("RANDOM TELEPORT", () => { var c = Bridge.IslandCentre ?? Bridge.Player.position; ForAll((p, i) => Bridge.TeleportPlayer(p, Bridge.Grounded(c + new Vector3(UnityEngine.Random.Range(-60f, 60f), 0, UnityEngine.Random.Range(-60f, 60f))))); }));
                events.Add(new KeyValuePair<string, Action>("BOUNCE HOUSE", () => ForAllSet(bouncing, true)));
                events.Add(new KeyValuePair<string, Action>("BOAT LAUNCH", () => Bridge.BoatImpulse(Vector3.up * 40f, Vector3.zero)));
            }
            if (chaosPlayers)
            {
                events.Add(new KeyValuePair<string, Action>("SUPER SPEED", () => { speedMult = 4f; Bridge.SetSpeed(4f); }));
                events.Add(new KeyValuePair<string, Action>("SUPER JUMP", () => { jumpMult = 5f; Bridge.SetJumpMultiplier(5f); }));
                events.Add(new KeyValuePair<string, Action>("GIANT MODE", () => SetBodyShape(1)));
                events.Add(new KeyValuePair<string, Action>("TINY MODE", () => SetBodyShape(2)));
            }
            if (chaosWorld)
            {
                events.Add(new KeyValuePair<string, Action>("DISCO", () => disco = true));
                events.Add(new KeyValuePair<string, Action>("RAINBOW WORLD", () => { SetFog(null, null); rainbowWorld = true; }));
                events.Add(new KeyValuePair<string, Action>("DRUNK CAMERA", () => drunkCam = true));
                events.Add(new KeyValuePair<string, Action>("SLOW MOTION", () => SetTimeScale(.35f)));
                events.Add(new KeyValuePair<string, Action>("FISHEYE", () => fov = 140f));
                if (chaosExtreme) events.Add(new KeyValuePair<string, Action>("UPSIDE DOWN", () => upsideDown = true));
            }
            if (events.Count == 0) return;
            // Clear the previous round's temporary effects before starting the next.
            if (!chaosExtreme)
            {
                disco = rainbowWorld = drunkCam = upsideDown = flyingFish = fishTornado = false; fov = -1f;
                if (fovCamera != null && originalFov > 0) fovCamera.fieldOfView = originalFov;
                RestoreWorldVisuals(); SetTimeScale(1f); bouncing.Clear();
                if (gravitySaved) SetGravity(1f);
                if (bodyShape != 0) SetBodyShape(0);
                if (speedMult != 1f) { speedMult = 1f; Bridge.SetSpeed(1f); }
                if (jumpMult != 1f) { jumpMult = 1f; Bridge.SetJumpMultiplier(1f); }
            }
            var e = events[UnityEngine.Random.Range(0, events.Count)];
            chaosCount++;
            chaosEvent = "CHAOS EVENT #" + chaosCount.ToString("000") + "\n" + e.Key;
            chaosBannerUntil = Time.unscaledTime + 3f;
            try { e.Value(); } catch (Exception ex) { Logger.LogWarning("Chaos " + e.Key + ": " + ex.Message); }
            Note("CHAOS #" + chaosCount.ToString("000") + " // " + e.Key);
        }
        private float chaosBannerUntil;

        // ---------------- SETTINGS ----------------
        private Menu settingsMenu;
        private Menu SettingsMenu()
        {
            if (settingsMenu != null) return settingsMenu;
            var m = settingsMenu = new Menu("SETTINGS");
            m.Choice("MENU COLOUR", accentNames, () => accentIndex, v => accentIndex = v);
            m.Toggle("RAINBOW MENU", () => rainbowMenu, v => rainbowMenu = v);
            m.Choice("MENU POSITION", new[] { "RIGHT", "LEFT", "CENTRE" }, () => menuPos, v => menuPos = v);
            m.Slider("MENU SCALE", () => menuScale, v => menuScale = v, .7f, 1.6f, .1f, "0.0x");
            m.Slider("FLY SPEED", () => flySpeed.Value, v => flySpeed.Value = v, 4f, 60f, 2f, "0");
            m.Sub("KEYBINDS", KeybindMenu);
            m.Action("SAVE CONFIG", () => { Config.Save(); Note("Config saved"); });
            m.Label("Hooks: " + Patches.Report);
            m.Label("Controller: LB + D-PAD UP, A select, B back");
            return m;
        }
    }
}

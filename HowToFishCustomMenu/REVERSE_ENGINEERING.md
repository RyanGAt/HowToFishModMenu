# Game hooks used by KRAKEN

Source: the game's own `How to Fish_Data/Managed/Assembly-CSharp.dll` (How to Fish 1.0.12), decompiled for reading only. No game code is included in this repo. KRAKEN references the game DLLs at build time and calls into them at runtime.

The game uses **FishNet** networking and is **host-authoritative**. Anything that has to change for every player goes through a server path the game already has. Anything else is labelled as local in the menu.

## Harmony patches (`src/Patches.cs`)

| Feature | Patched method | What the patch does |
| --- | --- | --- |
| Personal god mode | `PlayerVitals.TakeDamage` (prefix) | Skips damage when the vitals belong to the local player (host only, because damage runs on the server). |
| Unlimited bait | `PlayerInventory.ServerOnBaitUsed` (prefix) | Skips the "bait lost" roll. |
| Force roulette / guaranteed win | `CasinoManager.ServerRouletteResult(BetColor)` (prefix, `ref winColor`) | Replaces the colour the ball landed on with the forced colour, or with the colour that was bet. The payout and effects follow the game's own code. |
| Multiply winnings | `Item.AddBetMultiplier(float)` (prefix) | Scales the payout multiplier on winning bets. |
| Explosive bullets | `ProjectileManager.Hit` (postfix) | Spawns an explosive item at the impact point and calls `Explosive.ForceExplode`. |
| Silent aim | `ProjectileManager.AddProjectile` / `AddProjectiles` (prefix) | The game deletes bullets 0.5m under the water, and most targets are fish, so redirecting the bullet isn't enough. The hit is applied straight to the target with `Item.LocalHit`, the same call `Weapon.Shoot` uses for point-blank hits, and the visible bullet is bent toward it. |
| No recoil | `PlayerCamera.Recoil`, `PlayerToolMovement.Recoil`, `Weapon.AddModelRecoil` (prefix) | Skips all three recoil sources. |
| No cooldown | `Weapon.HasCooldown` (prefix) | Returns false for the local player's gun. |
| Damage multiplier | `Attachments.get_Damage` (postfix) | Scales damage for the local player's gun. This covers bullets and point-blank hits. |

## Direct game calls

| Feature | Game code |
| --- | --- |
| Local player, movement, teleport | `Player.LocalPlayer`, `Player.Movement`, `PlayerMovement.Teleport`, the private fields `_walkSpeed`, `_sprintSpeed` and `_jumpForce`, and `Jump()` / `Knockback()` |
| Teleport / launch other players | `Player.RPCTeleport(NetworkConnection, Vector3, float)` and `PlayerMovement.RPCKnockback(NetworkConnection, Vector3)`, the game's TargetRpcs, sent by the host |
| Heal / kill / hunger | `PlayerVitals.Heal`, `RestoreFullness`, `TakeDamage(..., ignoreInvulnerability)` |
| God mode lobby | `PlayerManager.ToggleGodMode` (the game's own server-wide flag) |
| One-shot lobby | `ServerSettings._useOneShot` SyncVar |
| Spawning items, fish, bosses and birds | `GameInfo._nameToSpawnable` prefabs plus `ItemManager.SpawnNewItem` (networked spawn). Prefab members like `Item.Fish` are only set in `Awake`, so prefabs are matched by component type. |
| Item value / weight | `Item._killScoreMultiplier` and `_syncedRandomWeight` SyncVars, which feed `Item.TotalWorth` |
| Money | `MoneyManager.AddMoney` / `RemoveMoney` / `SellItem` (server) |
| Instant catch / auto fish | Advances the private `Bait.TimeUnderWater` past `RandomizedCatchTime` |
| Weapon mods / full upgrade | `Weapon` private fields (`_timeBetweenShots`, `_fullAuto`, `_spread`, `_projSpeed`, ...), and the `Attachments` SyncVars `_syncedBulletIndex`, `_syncedBarrelAttachment`, `_syncedSight`, `_syncedExtendedMag` and `_syncedLaserSight` |
| Boat | `BoatManager.Boat.HiddenPhysicsRig` (host physics), `BoatManager.TryMoveBoat` |
| Islands | `OnlineIslandManager.TpToSpecificIsland` (moves the whole lobby, like the game's own travel) and the `_maxIslandUnlocked` SyncVar |
| Unlock all | The islands SyncVar, `Boat.UnlockBoat` / `UnlockBoatRadar`, `NPCManager.UnlockGrill`, `PlayerInventory.UnlockExtraPocket`, `ServerBoughtBait` |
| Chat kill-feed | `OnlineChatManager.SendChatMessage(ulong, string)`, an ObserversRpc. Sent with the lobby id, it shows as `[Server]` for everyone. |
| ESP / camera | `GameInfo.CurCamera` (the game renders to a smaller target, so ESP uses viewport coordinates), `PlayerCamera._rot` / `SetRot` for aimbot and spinbot |
| Menu input blocking | Disables named actions in `GameInfo.Input.actions` (`PlayerJump`, `PlayerLook`, `PlayerLeftClick`, ...) while the menu or free cam is open |
| Sounds | `AudioManager.PlayGlobalClip` (`Hover`, `Click`, `PlayerSpawn`) |

## Things that can't be shared with everyone

- **Custom geometry** (sky base, water car, race track) can't be networked. FishNet can only spawn prefabs that are registered in the game, so these are built locally.
- **Standing on items:** players don't collide with loose items. A test platform made of frozen items (using `Item.ToggleInteractable(false)`, the casino's lock) was visible to everyone, but nobody could stand on it, so the feature was removed.
- **Remote player size / camera:** transforms and cameras belong to each client and aren't synced, so these only change your own view.

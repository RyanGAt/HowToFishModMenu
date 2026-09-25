# Verified game hooks and next steps

Source inspected: the installed `How to Fish_Data/Managed/Assembly-CSharp.dll`. These are assembly findings, not claims of successful in-game testing.

| Feature family | Verified game path | Implementation boundary |
| --- | --- | --- |
| Fish rain | `GameInfo._nameToSpawnable` holds item prefabs; `Item.Fish` identifies fish items; host `ItemManager.SpawnNewItem(Item, Vector3, Quaternion)` creates world items. | v0.6.0 spawns at most 12 per click. Verify visibility and cleanup in a private lobby. |
| Fish tornado | Spawned fish expose `Item.Rig` (`Rigidbody`). | v0.6.0 applies a bounded rising, tangential force to nearby fish on the host. Creature AI and physics syncing may counter the motion; test in game. |
| Player jump | `PlayerMovement.Jump()` sets its local `_rig.linearVelocity.y` from `_jumpForce`. | Local super jump works through a local field change. Persistent lobby super jump requires every participating modded client to receive and apply a setting. A host field change on a remote player copy is insufficient. |
| Lobby launch | `PlayerMovement.RPCKnockback(NetworkConnection, Vector3)` is a FishNet target RPC that calls `Knockback` on the receiving client. | v0.4.0 sends it to player owners. Live multiplayer testing is pending. |
| Health | `PlayerVitals` exposes `Health`, `Heal(int)`, `TakeDamage(...)`, `ServerResetVitals()`, and synced health. | Host authority and desired target scope must be checked before adding player list actions. |
| Roulette | `CasinoManager.ServerRouletteResult(BetColor)` resolves a result against `_curBetColor`; `LocalCasino.Instance.ServerStartRoulette()` starts the sequence. | Calling the result method at an arbitrary time would desynchronise the visible wheel. Trace its normal caller and patch the authoritative selection point before exposing a force-result toggle. |
| Weapon/projectiles | `Weapon.Shoot()` performs ammo/cooldown checks, raycasts, and creates projectiles through `ProjectileManager`; `ExplosionManager.ServerExplode` handles explosions. | Triggerbot can call `Shoot`. Silent aim and explosive bullets require a verified per-shot/projectile interception path. |

Use small spawn counts and test each networked action in a consensual private lobby. Capture BepInEx logs and note whether host and clients have FISH TOOL installed when reporting a failure.

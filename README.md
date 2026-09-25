# 🦑 KRAKEN v1.1.0 — Modded Lobby Edition

An old-school Xbox 360 **Call of Duty style mod menu** for ***How to Fish***.

It has a dark menu column with a blue header, a highlighted selection bar, ON/OFF tags, a scroll bar and a message feed in the bottom-left. You scroll with the arrow keys, select with **Space** and go back with **Backspace**, and you can keep walking around while it's open.

> Built for private lobbies with friends. It's a BepInEx 5 plugin for the Windows Mono build of the game.

---

## 📥 Installation

### 1. Install BepInEx 5
1. Download **BepInEx 5 (x64, Mono)** from the [BepInEx releases page](https://github.com/BepInEx/BepInEx/releases). Get the latest `BepInEx_win_x64_5.x.x.zip`.
2. Open your game folder. In Steam, right-click **How to Fish** → **Manage** → **Browse local files**. It looks like:
   ```
   ...\steamapps\common\How to Fish\How to Fish\
   ```
3. Extract the zip into that folder so `winhttp.dll` and the `BepInEx` folder sit next to `How to Fish.exe`.
4. Start the game once and close it. This creates `BepInEx\plugins`.

### 2. Install KRAKEN
1. Download [`HowToFishCustomMenu.dll`](HowToFishCustomMenu/bin/Release/netstandard2.1/HowToFishCustomMenu.dll) from this repo.
2. Copy it into:
   ```
   How to Fish\BepInEx\plugins\
   ```
3. Start the game. The top-left corner shows **KRAKEN v1.1.0 [INSERT]**.
4. Press **Insert** to open the menu.

To check it loaded, open `BepInEx\LogOutput.log`. It should contain:
```
Loading [KRAKEN - Modded Lobby Edition 1.1.0]
Harmony: 12/12 hooks active
```

### Uninstall
Delete `BepInEx\plugins\HowToFishCustomMenu.dll`.

---

## 🎮 Controls

| Key | Action |
| --- | --- |
| **Insert** | Open / close the menu |
| **↑ / ↓** | Scroll (hold to repeat) |
| **Space** | Select / toggle |
| **← / →** | Change sliders and choices |
| **Backspace** | Go back a page |
| **Escape** | Close the menu |

- While the menu is open you can still **walk around**. Jump, mouse look, shooting and hotbar scrolling are switched off so they don't trigger by accident.
- **Text rows** (money, item search, coordinates): press **Space** to start typing and **Enter** when you're done.
- **Fly mode:** WASD to move, Jump to go up, Left Ctrl to go down, hold Shift to go faster.
- **Fish cannon:** middle mouse button.

The menu key can be changed in `BepInEx\config\sunshineplunge.howtofish.custommenu.cfg`.

---

## 👑 Host vs client

How to Fish uses a host-authoritative network, so many effects only work when **you host the lobby**.

- **[HOST]** rows only work when you're the host. When you're not, they're greyed out, and pressing one tells you why.
- **LOCAL VIEW** rows only change what you see: body shape, boat colour and size, camera effects, fog and time of day.
- Friends **don't need the mod installed**. Host actions use the game's own network functions.

---

## 📋 Menus & features

### 01 · Player Menu
God Mode [HOST] · Demi God (auto heal) · Never Hungry · Fly Mode · No Clip · Speed Multiplier · Super Speed · Super Sprint · Jump Multiplier · Super Jump · Infinite Jump · Gravity Multiplier · Moon Gravity · Zero Gravity · Walk on Water · Freeze in Mid-Air · Spinbot · Body Shape (Giant / Tiny / Super Skinny / Wide) · Launch Me Up · Suicide / Respawn · Reset Character

### 02 · Fishing Menu
Instant Catch · Auto Fish · Unlimited Bait · Fish Size · Spawn Random / Giant / Tiny Fish · Spawn Every Fish · Fish Rain · **Fish Tornado** · Fish Army · Fish Magnet · Flying Fish · Exploding Fish · Fish Value Multiplier (affects the real sale price) · Fish Weight Multiplier · Resize All Fish · Fish Cannon

### Aimbot Menu
Aimbot · ADS Only · Snap / Smooth · 360 Aimbot · Target Prediction · **Silent Aim** · Triggerbot · **Explosive Bullets** · Unfair Aimbot preset
*Targets fish and sea creatures only.*

### 03 · Weapon & Tool Menu
**Full Upgrade Held Gun** (max bullets, best barrel, extended mag, laser) · Cycle Sight · **No Recoil** · **Damage Multiplier (up to 50x)** · One Shot Kill Lobby · Unlimited Ammo · Instant Reload · Rapid Fire · No Cooldown · Zero Spread · Fast Projectiles · No Self Knockback · Explosive Bullets · **God Gun (everything on)** · Fish Launcher · Object Cannon · Super Knockback Blast · Spawn Explosion · Give All Weapons

### 04 · Item Spawner
**Searchable item list** · Quantity 1 / 10 / 100 · Spawn by Name / ID · Spawn Random Item · Spawn All Items · Give All Fishing Rods · Give All Equipment · Duplicate Held Item · Delete Held Item · Delete Nearby Items · Item Size · Item Value Multiplier · Floating Items · Item Rain

### 05 · Teleport Menu
Save Position · Waypoints · Spawn / Island · Water · Fishing Spot · Shop · Casino · Boat · **Any NPC** · Selected Player · Random Location · 100m Up · Under Map · Blink Forward · Teleport to Coordinates · Show My Coordinates · Load Island
*Destinations come from the island you're currently on.*

### 06 · Vehicle Menu
Super Boat Speed · Boat Speed Multiplier · Speed Boost · Flying Boat · Drive on Land · Boat Jump · Launch Boat · Flip Boat · Boat Spin · Teleport Boat to Me · Teleport to Boat · Rainbow Boat · Boat Size · Invisible Boat

### 07 · Players (player list)
Pick a player to open their menu: Player Info · Teleport to / Bring to Me · Heal · Heal Aura · Kill · Launch · **Launch into Space** · Launch into Water · Sky Teleport · Random Teleport · Freeze · Spin · Bounce · Fish Rain on Player · **Fish Prison** · Spawn Fish Around · Giant Fish Behind · Fish Pile · Spawn Objects Around · Explosion on Player · Fish Explosion · Give Money · Give Item · Clear Effects

### 08 · Troll Menu
The same player actions, aimed at a **TARGET** you cycle through with Space.

### 09 · Fun Menu
Disco Mode · Rainbow World · Rainbow Mod Menu · Everything Floats / Spins / Bounces · Giant / Tiny Everything · **Fish Apocalypse** · Time Scale · Super Slow Motion · Fast Forward · Drunk Camera · Upside Down Camera · Spinning Camera · Fisheye · Extreme Zoom · FOV slider

### 10 · Lobby Menu
God Mode Lobby · Teleport All to Me · Heal Lobby · Launch Lobby · Moon Gravity · Fish Rain Lobby · Giant Fish Lobby · Unlimited Money · Unlimited Items · Bounce Lobby · Random Teleport Lobby · Fish Apocalypse Lobby · Chaos Lobby · Reset Lobby Effects

### 11 · Casino Menu
Set / Add Money · Unlimited Money · **Force Roulette (Black / Red / Green)** · **Guaranteed Win** · Multiply Winnings · Jackpot Mode · Casino Chaos · Teleport to Casino
*Roulette rigging works when you host.*

### 12 · World Menu
World Gravity · Freeze Time · Speed Up Physics · No Fog · Super Fog · Time of Day (Day / Sunrise / Sunset / Night) · Spawn / Delete Objects · Object Size · Launch Nearby Objects · Reset World

### 13 · ESP Menu
Player ESP / Name Tags · Fish ESP · Item ESP · Valuable ESP ($100+) · **ESP Boxes** (on/off) · **ESP Lines / Tracers** (on/off) · Line Start (Bottom / Centre / Top) · Distance · Rainbow ESP · ESP Range

Colours: 🔵 fish · 🟢 items · 🟡 valuables · 🔴 players

### 14 · Chaos Mode
START / STOP CHAOS · Interval · Random Player / World / Fish Effects · Extreme Chaos · Reset Everything
Every few seconds a random event fires, with a big **CHAOS EVENT #001** banner: fish rain, moon gravity, giant mode, boat launch, fish apocalypse, disco, drunk camera and more.
**WARNING: ABSOLUTE FISH MAYHEM**

### Settings
Menu Colour (COD Blue, Cyan, Green, Red, Purple, Orange, Pink) · Rainbow Menu · Menu Position (Right / Left / Centre) · Menu Scale · Fly Speed · Save Config

---

## 🔧 Building from source

Requirements: **.NET SDK 6+** and the game installed with BepInEx 5.

```powershell
cd HowToFishCustomMenu
dotnet build -c Release "-p:GameDir=C:\Program Files (x86)\Steam\steamapps\common\How to Fish\How to Fish"
```

Or use the helper script:

```powershell
.\HowToFishCustomMenu\build.ps1 -GameDir "C:\...\How to Fish\How to Fish"
```

The DLL is written to `HowToFishCustomMenu\bin\Release\netstandard2.1\HowToFishCustomMenu.dll`. The project references `Assembly-CSharp`, FishNet, Unity and Harmony from your game folder.

### Source layout
| File | Purpose |
| --- | --- |
| `src/Plugin.cs` | Plugin entry, key navigation, frame loop |
| `src/Menu.cs` | Menu / option model (toggle, slider, choice, submenu, input) |
| `src/Menus.cs` | Every menu page and option |
| `src/MenuRenderer.cs` | COD-style drawing, ESP, feed, chaos banner |
| `src/Features.cs` | Feature state and per-frame effects |
| `src/Patches.cs` | Harmony hooks: god mode, bait, roulette, silent aim, explosive bullets, recoil, cooldown, damage |
| `src/GameBridge*.cs`, `src/WeaponMods.cs` | Calls into the game's player, item, money, boat, weapon and network code |

See [`REVERSE_ENGINEERING.md`](HowToFishCustomMenu/REVERSE_ENGINEERING.md) for notes on the game hooks.

---

## ⚠️ Notes

- Tested against How to Fish **1.0.12**. Game updates can break hooks. If that happens, check `BepInEx\LogOutput.log` for `Hook missing` lines.
- Use it in **private lobbies with friends who are happy to play modded**.
- Spawning huge numbers of items can lag the lobby, so keep quantities sensible.
- Not affiliated with the developers of How to Fish.

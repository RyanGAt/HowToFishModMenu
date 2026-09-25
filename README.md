# 🦑 KRAKEN v1.2.0 — Modded Lobby Edition

An old-school Xbox 360 **Call of Duty-style mod menu** for ***How to Fish***.

It has a dark menu column with a blue header, a bright selection bar, ON/OFF tags, a scroll bar and a message feed in the bottom-left. It has menu sounds, a **KRAKEN LOADED** banner when you spawn, and chat announcements that everyone in the lobby can see. You scroll with the arrows, select with **Space**, go back with **Backspace**, and can keep walking around while it's open, just like the old modded lobbies.

> A BepInEx 5 plugin for the Windows (Mono) version of How to Fish. Made for **private lobbies with friends who are up for a modded game**.

---

## 📥 Installation

### 1. Install BepInEx 5
1. Download **BepInEx 5 for Windows x64** from the [BepInEx releases page](https://github.com/BepInEx/BepInEx/releases). You want `BepInEx_win_x64_5.x.x.zip`.
2. Open your game folder. In Steam, right-click **How to Fish** → **Manage** → **Browse local files**. It looks like:
   ```
   ...\steamapps\common\How to Fish\How to Fish\
   ```
3. Extract the zip into that folder so `winhttp.dll` and the `BepInEx` folder sit next to `How to Fish.exe`.
4. Start the game once and close it. This creates the `BepInEx\plugins` folder.

### 2. Install KRAKEN
1. Download **`HowToFishCustomMenu.dll`** from the [**Releases**](../../releases) page.
2. Put `HowToFishCustomMenu.dll` in:
   ```
   How to Fish\BepInEx\plugins\
   ```
3. Start the game and load a world. You'll see the **KRAKEN LOADED** banner.
4. Press **Insert** (or **LB + D-pad Up** on a controller) to open the menu.

To check it loaded, open `BepInEx\LogOutput.log`. It should contain:
```
Loading [KRAKEN - Modded Lobby Edition 1.2.0]
Harmony: 12/12 hooks active
```

### Uninstall
Delete `BepInEx\plugins\HowToFishCustomMenu.dll`. To remove BepInEx completely, also delete the `BepInEx` folder, `winhttp.dll` and `doorstop_config.ini`.

---

## 🎮 Controls

| Keyboard | Controller | Action |
| --- | --- | --- |
| **Insert** | **LB + D-pad Up** | Open / close the menu |
| **↑ / ↓** | **D-pad Up / Down** | Scroll (hold to repeat) |
| **Space** | **A** | Select / toggle |
| **← / →** | **D-pad Left / Right** | Change sliders and choices |
| **Backspace** | **B** | Go back (B on the main page closes the menu) |
| **Escape** | | Close the menu |

- You can still **walk around** with the menu open. Jump, mouse look, shooting and item buttons are switched off while it's open so nothing fires by accident.
- **Text rows** (money, item search, coordinates): press **Space** to start typing and **Enter** when you're done.

### ⌨️ Hotkeys
Rebind any of these in **Settings → Keybinds**: press Space on a row, then press the new key. Backspace unbinds it and Esc cancels.

| Default | Action |
| --- | --- |
| **F5** | Save location |
| **F6** | Load location |
| **F7** | Teleport to the Sky Base |
| **F8** | Toggle fly |
| **F9** | Toggle no clip |
| **F10** | Get in / out of the KRAKEN water car |
| **F11** | Load preset 1 |
| *(unbound)* | God mode, presets 2 and 3 |

**Fly:** WASD, Jump to go up, Left Ctrl to go down, Shift for speed. **Fish launcher:** middle mouse.
**Water car:** W/S gas and brake, A/D steer, Shift boost, Jump to hop. On a controller: RT/LT and the left stick.

---

## 👑 Host vs client

How to Fish is host-authoritative, so a lot of the fun only works when **you host the lobby**.

- **[HOST]** rows only work when you're the host. As a client they're greyed out, and pressing one tells you why.
- Host actions use the game's own networking, so **friends don't need the mod installed** to see and feel them.
- **LOCAL VIEW** rows only change what *you* see: body shape, boat colour and size, camera effects, fog and time of day.
- The **Sky Base**, **Water Car** and **Race Track** are built in your own game. Only KRAKEN users see them; everyone else just sees you flying or gliding over the water.

---

## 📋 Menus

**Main menu:** Player · Fishing · Aimbot · Weapon & Tool · Item Spawner · Teleport · Vehicle · Extras · Players · Troll · Fun · Lobby · Casino · World · ESP · Chaos Mode · Settings · Reset Everything

### Player
God Mode · Demi God (auto heal) · Never Hungry · Fly Mode · No Clip · Speed Multiplier · Super Speed · Super Sprint · Jump Multiplier · Super Jump · Infinite Jump · Moon / Zero / Normal Gravity · Walk on Water · Freeze in Mid-Air · Spinbot · Body Shape (Giant / Tiny / Super Skinny / Wide) · Launch Me Up · Suicide / Respawn · Reset Character

### Fishing
Instant Catch · Auto Fish · Unlimited Bait · Fish Size · Spawn Random / Giant / Tiny Fish · Spawn Every Fish · Fish Rain · **Fish Tornado** · Fish Army · Fish Magnet · Flying Fish · Exploding Fish · Fish Value Multiplier (changes the real sale price) · Fish Weight Multiplier · Resize All Fish

### Aimbot
Aimbot · ADS Only · Snap / Smooth · 360 Aimbot · Target Prediction · **Silent Aim** (lands hits on fish even underwater) · Triggerbot · Unfair Aimbot preset
*Targets fish and sea creatures only.*

### Weapon & Tool
**Full Upgrade Held Gun** (max bullets, best barrel, extended mag, laser) · Cycle Sight · **No Recoil** · **Damage Multiplier (up to 50x)** · One Shot Kill Lobby · Unlimited Ammo · Instant Reload · Rapid Fire · No Cooldown · Zero Spread · Fast Projectiles · No Self Knockback · **Explosive Bullets** · **God Gun** (everything on) · Fish Launcher · Object Cannon · Super Knockback Blast · Spawn Explosion · Give All Weapons

### Item Spawner
**Searchable item list** · Quantity 1 / 10 / 100 · Spawn by Name / ID · Spawn Random Item · Spawn All Items · Give All Fishing Rods · Give All Equipment · Duplicate / Delete Held Item · Delete Nearby Items · Item Size · Item Value Multiplier · Item Rain

### Teleport
Save / Load Location · **Saved Locations** (kept after restart) · **Sky Base** · **Island Teleport** (one button per island) · Spawn · Water · Fishing Spot · Shop · Casino · Boat · **Any NPC** · Selected Player · Random · 100m Up · Under Map · Blink Forward · Coordinates

#### ☁️ Sky Base
A floating island base 60m above the island. It has a wooden deck, railings with glowing lights, a lounge corner, lamps, a lookout tower with spiral stairs, a diving board, a rocky underside with a glowing crystal, and a KRAKEN sign that always faces you. Press **F7** to go there.

### Vehicle
**KRAKEN Water Car** (an open-top convertible that drives on water and land, with a speedometer) · **Race Track** · Super Boat Speed · Boat Speed Multiplier · Speed Boost · Flying Boat · Drive on Land · Boat Jump · Launch · Flip · Spin · Teleport Boat to Me · Rainbow Boat · Boat Size · Invisible Boat

#### 🏁 Race Track
A floating oval on the sea with red and white kerbs, glowing barriers, a chequered start/finish gantry, 3 checkpoint arches and a **jump ramp over a gap**. **Start Race** puts you in the car on the grid, and a lap timer tracks your time, checkpoints and best lap.

### Extras
- **Unlock All** [HOST]: every island, the boat and radar, the grill, all extra pockets, and 50 of every bait for everyone.
- **Boss Spawner:** Bowhead Whale, Mutated Whale, Giant Piranha, The Old Pike, Goblin Shark and Spider Crab. Spawn one next to you or on a target, or start **Boss Rush**, where the next boss appears when the last one dies.
- **Seagull Army / Albatross Squad:** a flock of item-stealing birds over a target.
- **Auto-sell:** sell your held item or everything within 10m, or auto-sell dead fish near you.
- **Kill aura:** hits every creature in range (2–20m).
- **Camera:** first person · **third person** (with a stand-in character) · **free cam** · **spectate target**.
- **Presets:** save every toggle and slider into 3 slots and load one with a key.
- **Chat kill-feed:** as host, KRAKEN posts classic modded-lobby messages in the game chat, and everyone sees them as **[Server]** messages: *"KRAKEN // HOST ENABLED GOD MODE"*, *"KRAKEN // Dave GOT FISH PRISON!"*, boss alerts and a welcome message.
- **Menu sounds** on/off.

### Players
Pick a player to open their menu: Info · Teleport to / Bring to Me · Heal · Heal Aura · Kill · Launch · **Launch into Space** · Launch into Water · Sky Teleport · Random Teleport · Freeze · Spin · Bounce · Fish Rain on Player · **Fish Prison** · Spawn Fish Around · Giant Fish Behind · Fish Pile · Spawn Objects Around · Explosion · Fish Explosion · Give Money · Give Item · Clear Effects

### Troll
The same player actions, aimed at a **TARGET** you cycle through with Space.

### Fun
Disco Mode · Rainbow World · Everything Floats / Spins / Bounces · Giant / Tiny Everything · **Fish Apocalypse** · Time Scale · Super Slow Motion · Fast Forward · Drunk Camera · Upside Down Camera · Spinning Camera · Fisheye · Extreme Zoom · FOV slider · Reset Camera

### Lobby
God Mode Lobby · Teleport All to Me · Heal Lobby · Launch Lobby · Fish Rain Lobby · Giant Fish Lobby · Unlimited Items Lobby · Bounce Lobby · Continuous Fish Rain · Random Teleport Lobby · Fish Apocalypse Lobby · Chaos Lobby · Reset Lobby Effects

### Casino
Set / Add Money · Unlimited Money · **Force Roulette (Black / Red / Green)** · **Guaranteed Win** · Multiply Winnings · Jackpot Mode · Casino Chaos

### World
World Gravity · Freeze Time · No Fog · Super Fog · Time of Day (Day / Sunrise / Sunset / Night) · Reset World

### ESP
Player ESP / Name Tags · Fish ESP · Item ESP · Valuable ESP ($100+) · **Boxes** · **Lines / Tracers** (start from the bottom, centre or top of the screen) · Distance · Rainbow ESP · Range
Colours: 🔵 fish · 🟢 items · 🟡 valuables · 🔴 players

### Chaos Mode
Start / Stop · Interval · Random Player / World / Fish Effects · Extreme Chaos · Reset Everything. Every few seconds something ridiculous happens, with a big **CHAOS EVENT #001** banner.
**WARNING: ABSOLUTE FISH MAYHEM**

### Settings
Menu Colour (COD Blue, Cyan, Green, Red, Purple, Orange, Pink) · Rainbow Menu · Menu Position (Right / Left / Centre) · Menu Scale · Fly Speed · **Keybinds** · Save Config

---

## 🔧 Building from source

You need the **.NET SDK 6 or newer**, and the game installed with BepInEx 5.

```powershell
cd HowToFishCustomMenu
dotnet build -c Release "-p:GameDir=C:\Program Files (x86)\Steam\steamapps\common\How to Fish\How to Fish"
```

Or run `.\HowToFishCustomMenu\build.ps1 -GameDir "<your game folder>"`.

The output is `HowToFishCustomMenu\bin\Release\netstandard2.1\HowToFishCustomMenu.dll`. The project references `Assembly-CSharp`, FishNet, Steamworks.NET, the Unity Input System, Unity and Harmony straight from your game folder, so nothing from the game is included in this repo.

### Source layout
| File | What it does |
| --- | --- |
| `src/Plugin.cs` | Plugin entry point, menu navigation and the frame loop |
| `src/Menu.cs` | Menu and row types (toggle, slider, choice, submenu, text input) |
| `src/Menus.cs` | The main menu pages and their rows |
| `src/MenuRenderer.cs` | COD-style drawing, ESP, feed and banners |
| `src/Features.cs` | Feature state and per-frame effects |
| `src/Patches.cs` | Harmony hooks: god mode, bait, roulette, winnings, silent aim, explosive bullets, recoil, cooldown, damage |
| `src/Extras.cs` | Controller input, keybinds, saved locations |
| `src/Additions.cs` | Chat kill-feed, bosses, seagulls, auto-sell, kill aura, cameras, presets, sounds, banner |
| `src/SkyBase.cs` · `src/WaterCar.cs` · `src/Unlocks.cs` | Sky base, water car, race track, island teleport, unlock all |
| `src/GameBridge*.cs` · `src/WeaponMods.cs` | Calls into the game's player, item, money, boat, weapon and network code |

See [`REVERSE_ENGINEERING.md`](HowToFishCustomMenu/REVERSE_ENGINEERING.md) for which game functions each feature uses.

---

## ⚠️ Notes

- Built and tested against How to Fish **1.0.12**. A game update can break hooks. If something stops working, check `BepInEx\LogOutput.log` for `Hook missing` lines.
- Use it in **private lobbies with friends who are happy to play modded**. Don't use it to ruin strangers' games.
- **Unlock All**, spawned items and money changes are saved into your world. Try them on a spare save first.
- Spawning huge numbers of items can lag the lobby, so keep quantities sensible.
- This is a fan-made mod. It is not affiliated with or endorsed by the developers of How to Fish.

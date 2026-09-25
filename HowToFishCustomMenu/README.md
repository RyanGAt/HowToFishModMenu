# FISH TOOL v0.2.3 — Modded Lobby Edition

Old COD-menu-inspired **in-game Unity IMGUI source project** for *How to Fish* (Windows x64 / Unity Mono). This is a fresh custom project, not a renamed third-party DLL. The name/visuals are a homage to 360-era mod menus.

## Status — please read

**This is a source prototype, not a fully working game cheat DLL.** The earlier v0.2.2 menu was successfully built and loaded in-game by the project owner, but this v0.2.3 keyboard-navigation change has not yet been built or verified on their PC. No finished DLL is included. Don't mistake the menu entries for working cheats.

- **In-game menu implementation:** 11 categories, click or keyboard navigation, drag window, black / neon-green theme, FISH TOOL header and persistent config.
- **Implemented local presentation effects:** camera FOV/zoom, camera spin (can be overridden by FPS camera scripts), rainbow menu, disco overlay, local Time.timeScale slow motion, small HUD, random-effects chaos mode. Local slow motion can interfere with simulation and isn't a supported server-wide mode.
- **Partially implemented after a player/boat hook is connected:** fly, save-position teleport, boat boost, fish/item ESP. Multiplayer movement can be corrected by server authority.
- **NOT IMPLEMENTED / disabled or explicitly unbound:** instant catch, money editing, item spawning, god mode, roulette manipulation, other-player trolling and shared lobby effects. These need game-build-specific, correctly authorised methods. The current `UnboundGameBridge` will not pretend to execute them.

## Controls

- **Insert**: open / close.
- **Up / Down + Enter**: navigate / activate the green-highlighted row (v0.2.3 fixes keyboard focus activating Player instead of the selection).
- **Left / Right**: cycle pages.
- **Backspace / Escape**: main page / close.
- **Mouse**: click entries, drag by the header.
- **Fly (when connected):** WASD, Space up, Left Ctrl down.

## Build it on Windows

1. Install BepInEx 5 **Windows x64 Mono** in the directory that contains `How to Fish.exe`. Launch game once. Use only one mod loader.
2. Install .NET SDK 6+.
3. In PowerShell from this project folder run:

```powershell
dotnet build -c Release "-p:GameDir=I:\SteamLibrary\steamapps\common\How to Fish\How to Fish"
```

4. Copy `bin\Release\netstandard2.1\HowToFishCustomMenu.dll` to `How to Fish\BepInEx\plugins\HowToFishCustomMenu.dll`. Launch and press Insert.

If the build fails looking for Unity or BepInEx assemblies, check the **inner game folder** location and correct `GameDir`. This project references the UnityEngine.dll compatibility assembly plus UnityEngine CoreModule, IMGUIModule, TextRenderingModule, InputLegacyModule and PhysicsModule from `How to Fish_Data\Managed` and `BepInEx\core\BepInEx.dll` from the installation.

## Connect the actual game

`src/GameBridge.cs` defines the per-build integration contract. It intentionally defaults to `UnboundGameBridge`. Inspect the matching installed `How to Fish_Data\Managed\Assembly-CSharp.dll` in ILSpy/dnSpyEx. Implement the player/boat/entity discovery and actual authoritative fishing, inventory, health and roulette methods, then assign your concrete bridge inside `Plugin.Awake()`. Verify host authority and normal game state, and test in solo / private consensual lobbies. Do **not** guess internal class/method names or blindly write other players' state.

Community source references for the general architecture and reverse engineering (check respective MIT terms before reusing code):
- https://github.com/e8xl/HowToFish-Trainer
- https://github.com/orqz/HowToFishTrainer

Back up your saves. Game patches may break private hooks. If you're already running other mod menus, hotkeys and patches can conflict.

## Fix for CS0012 MonoBehaviour missing UnityEngine

Version 0.2.1 adds an explicit UnityEngine.dll reference to resolve the missing legacy UnityEngine assembly dependency reported during the first build. If you already extracted v0.2, either replace the project with this one, or add the following reference inside `<ItemGroup>` in `HowToFishCustomMenu.csproj`:

```xml
<Reference Include="UnityEngine"><HintPath>$(GameDir)\How to Fish_Data\Managed\UnityEngine.dll</HintPath><Private>false</Private></Reference>
```

Make sure the game actually has `How to Fish_Data\Managed\UnityEngine.dll` and run `dotnet build -c Release "-p:GameDir=I:\SteamLibrary\steamapps\common\How to Fish\How to Fish"` from the project directory.

## Fix for v0.2.1 CS0012 FontStyle/TextAnchor missing

v0.2.2 adds `UnityEngine.TextRenderingModule.dll` to `HowToFishCustomMenu.csproj` to fix both the CS0012 TextRenderingModule and CS0103 FontStyle/TextAnchor errors. The `Rigidbody.velocity` obsolete warning is not a build failure.

```xml
<Reference Include="UnityEngine.TextRenderingModule"><HintPath>$(GameDir)\How to Fish_Data\Managed\UnityEngine.TextRenderingModule.dll</HintPath><Private>false</Private></Reference>
```

This source has not been compiled against your local game DLLs in this environment; if the game version differs, other references may still need adjusting.

## v0.2.3 navigation fix

Unity IMGUI buttons can retain keyboard focus after the selection bar moves. The old implementation therefore sometimes opened Player when Enter was pressed on Fishing or another tab. The new menu renders passive rows and handles mouse selection and Enter explicitly against the highlighted row. Rebuild and copy the new DLL into `BepInEx\plugins`, replacing the previous one; no BepInEx reinstall is needed. This fixes navigation only; game-specific cheat hooks are still unbound.

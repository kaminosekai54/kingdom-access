# Kingdom Access

A mod that makes **Kingdom Two Crowns** playable by blind and visually impaired players.
It speaks through your screen reader (NVDA, JAWS and others), narrates the menus, describes
the world around you, lets you browse and walk to everything on the island, and warns you
with sounds when danger approaches.

*[Version française](README.fr.md)*

> Status: **beta (0.10.0)**. Developed and tested by a blind player with NVDA on Windows, on the
> Steam version of the game, mostly in the Norse Lands campaign. Feedback and bug reports are welcome.

---

## Contents

- [What it does](#what-it-does)
- [What it does not do (yet)](#what-it-does-not-do-yet)
- [Keys](#keys)
- [Installation](#installation)
- [Configuration](#configuration)
- [Languages](#languages)
- [Still to be tested](#still-to-be-tested)
- [Building from source](#building-from-source)
- [Credits](#credits)
- [License](#license)

---

## What it does

**Speech**
- Speaks through your screen reader using Tolk (NVDA, JAWS, SuperNova, ZoomText...), with the
  Windows voice as a fallback. Braille works through your screen reader.
- Follows the game language automatically, and switches when you change it in the game options.
  All 11 languages of the game are included: English, French, German, Spanish, Italian, Portuguese,
  Russian, Japanese, Korean, Simplified and Traditional Chinese (see [Languages](#languages)).
- History of the last 50 messages (repeat, previous, next).
- Keyboard and gamepad: every shortcut exists on both, and all of them are configurable.

**Menus and screens**
- Standard menus: selected element, its type (checkbox, slider), state, position ("2 of 5"),
  and the text of windows that open. Icon-only buttons get a name.
- New game screen (world, difficulty, monarch), map and timeline (including the Call of Olympus
  world map: oracle, temples, quest islands, Mount Olympus, and what unlocks them), blazon editor (with
  descriptions of every background and emblem, and colour names), end-of-island summary
  (read in full, then browsable line by line).
- Tutorial: the ghost's hints are announced (action expected and where the ghost is).
- On-screen texts (notifications, information bubbles) are read when they appear, and can be
  collected in a browsable list.

**Around you**
- The object selected by the game (the point where you can pay) is announced with its level
  (walls, towers, castle), price, action (with the target level for an upgrade), and what is
  missing: a requirement (stone or iron technology, castle level, hermit, time of day...) or the
  coins you lack. Moving to another object interrupts the previous announcement.
- While galloping (the game selects nothing then), every useful object you ride past is announced:
  castle, shops, merchant, mounts, statues, puzzles, portals, chests, forest-edge trees...
- Entering and leaving the kingdom and vagrant camps; direction of the base camp when you land.
- Reports on demand: coins and gems, day / season / time / hours until night, mount, relic and
  abilities (with what they do), compass and danger, troop census (with coins carried).

**Finding things**
- **Scanner** by categories: buildable, upgradable, walls, towers, trees, camps, troops, shops,
  buildings, mounts, statues, characters, treasure (including chests), travel (boat, wharf),
  puzzles and relics, cave and bomb, enemies, others. Items are sorted by distance and give
  their side and distance.
- **Radar**: the nearest interesting object of each kind on each side.
- **Auto-walk and auto-run** to the selected item, to the castle, to your lost crown, or just
  behind the farthest wall on either side. Pressing the opposite direction takes back control.
- By default, only what you have already explored is listed (configurable).

**Alerts**
- Enemies approaching: three levels (20, 10 and 5 by default), each with its own sound, played in
  the ear the enemy comes from, plus the number of enemies and the distance.
- Lost crown: alert with position, repeated until you pick it up; a key runs to it.
- Dawn, day, evening and night: a sound and an announcement.
- A short chime when the object in front of you can be paid right now.
- An ability (item of power, ruler, mount) being ready again.

**DLC content**
- Norse Lands puzzles: Heimdall (time of day of each pillar, mount required, horn), Thor (current
  rune of each pillar, whether it is right, how many are right), Hel (what each sconce is waiting
  for), Loki (how to proceed and the risks). Solved puzzles are announced.
- What every relic and artifact does (Thor's hammer, Hel's trophy, Heimdall's horn, Loki's staff,
  Athena's shield, Hermes's staff, Hephaestus's hammer, Artemis's bow), the Dead Lands monarchs'
  powers, and every mount's special ability (with the key that triggers it).
- Call of Olympus objects: oracle, shipyard, border stones, mounts to buy; distinct names for the
  boat parts (wreck, construction, set-sail point, boat).
- Bomb expedition to the Greed cave: every stage is announced (escort, entrance, crossing,
  guardian, detonation, exit), and while you are beyond the cliff portal the scanner and radar
  only show that area. Inside the cave, a heartbeat guides you to the bomb, where you have to
  act: faster and louder when closer, in the ear of the side where it is. The bomb's description
  gives its current position, then the cliff portal's.

## What it does not do (yet)

- **Single player focus.** Only player 1 is followed. Local and online co-op are not supported
  by the mod, and the Steam invite overlay cannot be read by any mod.
- **Mouse-only screens** that are not standard menus may still be silent. Please report them.
- **Olympus (Call of Olympus)** is only partly covered: its puzzles and quests have not been
  tested yet, and several artifact and mount descriptions are missing on purpose (no reliable
  source).
> **Note:** all the mod's features should work in multiplayer, except perhaps interactions with the
> other player (their position, buying a new crown). This has not been tested yet.


## Keys

Defaults, all configurable (keyboard and gamepad). Keyboard defaults avoid the game's own keys:
player 1 uses WASD, the arrows and Shift (Left Shift also triggers mount abilities, so no mod
shortcut uses Shift); player 2 uses G, H, J, K, L, I and Right Shift (G and J also start
split-screen co-op). Press **F1** in game for the list of your current shortcuts.

### Keyboard

| Key | Action |
|---|---|
| F1 | Help: list of all shortcuts, with their gamepad buttons |
| F4 | Repeat the current tutorial hint |
| F11 / F9 / F10 | Repeat last message / previous / next in the history |
| F2 | Read the current screen (window and selected element; blazon summary) |
| F3 | Put every on-screen text in a browsable list |
| O | Coins and gems ("or" = gold) |
| T | Day, season, time of day, hours until night or dawn |
| M | Mount: tired or not, its ability and what it does |
| R | Relic and abilities, with what they do |
| C | Compass: facing, time of day, danger, nearest wall |
| P | Population: troop census |
| V | Radar |
| X | Details of the object in front of you |
| Home / Ctrl+Home | Scanner: next / previous category |
| Page Up / Page Down | Previous / next item of the last list (scanner, radar, census, help, summary...) |
| E | Reread the selected item with its current distance |
| End / Ctrl+End | Walk / run to the selected item (press again to stop) |
| B | Run to the base (castle or campfire) |
| Ctrl+C | Run to your lost crown |
| Ctrl+Left / Ctrl+Right | Run just behind (inside) the farthest wall on that side |
| Ctrl+F3 / Alt+F3 | Development: write the object in front of you / the whole island to the log |

### Gamepad

Xbox layout (PlayStation: A = Cross, B = Circle, X = Square, Y = Triangle). Any pad that Windows
or Steam Input presents as an Xbox pad works. The mod uses two **layers**: hold **LB** for
navigation or **RB** for reports, then press a button. While LB or RB is held, the game ignores
the pad, so a mod shortcut never drops a coin or moves the monarch.

| Hold LB + | Action | Hold RB + | Action |
|---|---|---|---|
| D-pad up / down | Previous / next category | A | Coins and gems |
| D-pad left / right | Previous / next item | B | Mount |
| A | Walk to the item | X | Time |
| RT | Run to the item | Y | Relic and abilities |
| LT | Run to the base | D-pad up | Compass |
| X | Reread the item | D-pad down | Census |
| Y | Radar | D-pad left / right | Behind the left / right wall |
| B | Object in front of you | RS (click) | Run to the lost crown |
| View | Repeat last message | View | Help |
| RS (click) | Read the screen | LS (click) | Tutorial hint |
| LS (click) | Screen texts list | LT / RT | Previous / next message |

## Installation

1. **BepInEx 6 (IL2CPP) and its patches.** The game needs BepInEx 6 bleeding edge for IL2CPP, plus
   two patches made for this game by [abevol/KingdomMod](https://github.com/abevol/KingdomMod#install)
   (recent game updates broke BepInEx without them):
   - [BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.753](https://builds.bepinex.dev/projects/bepinex_be/753/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.753%2B0d275a4.zip): extract it into the game folder, so that
     the `BepInEx` folder and `winhttp.dll` are next to `KingdomTwoCrowns.exe`.
   - [Cpp2IL.Patch](https://github.com/abevol/KingdomMod/releases/download/2.4.0/Cpp2IL.Patch.zip) and [Il2CppInterop.Patch](https://github.com/abevol/KingdomMod/releases/download/2.4.3/Il2CppInterop.Patch.zip): extract both into the game folder
     too, replacing the files with the same name.

   Launch the game once so BepInEx generates its files (the first launch takes longer).
2. **The mod.** Extract the release archive `KingdomAccess-<version>.zip` into the game folder (the
   one containing `KingdomTwoCrowns.exe`). You should get `BepInEx\plugins\KingdomAccess\` containing `KingdomAccess.BepInEx.dll`, `KingdomAccess.Core.dll`,
   `Tolk.dll`, `nvdaControllerClient64.dll`, and the `Lang` and `Sounds` folders.
3. **NVDA users: install the NVDA add-on** `kingdomAccessKeys` (attached to each release; open
   the `.nvda-addon` file with NVDA running). NVDA normally stops speaking at every key press,
   which cuts the mod's announcements all the time in a game. While Kingdom Two Crowns has the
   focus, the add-on stops key presses from interrupting speech; the mod interrupts speech itself
   when it has something new to say. Do not use NVDA's sleep mode in the game: it also silences
   the mod.
4. **Start the game** with your screen reader running. After a few seconds you should hear
   "Kingdom Access version ... loaded".

## Configuration

`BepInEx\config\kingdom.access.cfg` is created on the first launch. Every setting and every
shortcut is documented in the file. Sections:

1. **General**: on/off, forced language, Windows voice fallback, history size.
2. **Announcements**: object selected by the game, objects passed while galloping, kingdom and
   camp zones, on-screen texts and tutorial, abilities ready.
3. **Radar and scanner**: ranges, explored-area-only mode and its margin, destroyed portals.
4. **Alerts and sounds**: enemy alert and its distance, crown alert, day phases, sounds.
5. **Menus**: menu narration, menu logging (development).
6. **Keys**: every shortcut, e.g. `Wallet = O`, `Radar = V`, `TargetDetails = X`. Key names
   are Unity key names (`F5`, `PageDown`, `LeftArrow`...); modifiers are `Ctrl`, `Shift`, `Alt`.
   Leave a value empty to disable a shortcut. Old defaults that used Shift are updated automatically.
7. **Gamepad**: gamepad on/off, blocking the game while a layer button is held, the layer buttons,
   and every gamepad shortcut (e.g. `PadWallet = RB+A`).

Restart the game after editing the file. Sounds are WAV files in `plugins\KingdomAccess\Sounds`:
replace one with your own file of the same name to change it.

## Languages

The mod's texts are in [`Localization/`](Localization/README.md), one JSON file per language.
English and French were written by hand; the other languages were translated with AI assistance
and have not been reviewed by native speakers yet: corrections are very welcome.
Adding a language means copying `en.json`, translating the values, and checking it with
`python tools/check_localization.py`. See the [localization guide](Localization/README.md).

## Still to be tested

These parts work in principle but have not been confirmed in game, or only partly:

- Gamepad: check every layer shortcut with a real pad, and that the game gets the pad back when
  LB or RB is released. LB and RB are assumed unused by the game.
- Announcements while galloping: may need tuning (too many or too few).
- On-screen text reading: may be too talkative in some places (can be turned off).
- Multiplayer features.
- Greed cave heartbeat: works, but it is not distinct, loud or regular enough yet; it will be
  reworked (it can be turned off with `CaveBeacon = false`).

When something is wrong, `Alt+F3` next to it writes the whole island to
`BepInEx\LogOutput.log`: attach that file to your report.

## Building from source

Requirements: Windows, the .NET SDK (6 or later), the game with BepInEx 6 IL2CPP installed and
launched once (the build references the interop assemblies BepInEx generates).

1. Put `Tolk.dll` and `nvdaControllerClient64.dll` (x64) in `lib/native/` (see
   [lib/native/README.md](lib/native/README.md)).
2. If the game is not in the default Steam folder, edit `GameDir` in `Directory.Build.props`.
3. `dotnet build KingdomAccess.slnx -c Release` builds and installs the mod into the game
   (close the game first). Add `-p:NoDeploy=true` to build without installing.
4. `python tools/make_release.py` builds the release files into `dist/`: the zip to extract into
   the game folder, and the NVDA add-on.

Project layout:

```
src/KingdomAccess.Core      the mod itself, independent of the mod loader
  Speech/                 screen reader output (Tolk), history, sounds
  Localization/           language handling (follows the game language)
  Game/                   access to game objects, unit cache, object names, DLC puzzles
  Features/               announcements, scanner, radar, reports, auto-walk, menus, alerts...
  Patches/                Harmony patches (world map, blazon editor)
src/KingdomAccess.BepInEx   BepInEx 6 IL2CPP adapter (entry point, configuration file)
nvda-addon/                 NVDA add-on: game keys do not interrupt speech (build: python tools/build_nvda_addon.py)
Localization/             mod texts, one file per language
tools/                    sound generator, localization checks, NVDA add-on build, research helpers
```


## Credits

- [Tolk](https://github.com/dkager/tolk) (screen reader abstraction) and the NVDA controller
  client by NV Access.
- [abevol/KingdomMod](https://github.com/abevol/KingdomMod) for the BepInEx setup and as a
  reference on the game's structure.
- This mod was inspired by the accessibility features of the KingdomEnhanced mod (speech
  hotkeys and radar). No code was reused.
- The [Kingdom Wiki](https://kingdomthegame.fandom.com/) for puzzle and item descriptions.
- Kingdom Two Crowns is published by Raw Fury. This project is not affiliated with the game's
  developers or publisher. It ships no game code or assets.

## License

[MIT](LICENSE). You may use, modify and redistribute this code, including in your own mods,
as long as you keep the copyright notice, which credits the author: Alexis (kaminosekai54).
Third-party libraries (Tolk, NVDA controller client) keep their own licenses.

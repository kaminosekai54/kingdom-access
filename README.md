# Kingdom Access

A mod that makes **Kingdom Two Crowns** playable by blind and visually impaired players.
It speaks through your screen reader (NVDA, JAWS and others), narrates the menus, describes
the world around you, lets you browse and walk to everything on the island, and warns you
with sounds when danger approaches.

*[Version française](README.fr.md)*

> Status: **beta (0.7.0)**. Developed and tested by a blind player with NVDA on Windows, on the
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
  French and English are included; other languages fall back to English (see [Languages](#languages)).
- History of the last 50 messages (repeat, previous, next).

**Menus and screens**
- Standard menus: selected element, its type (checkbox, slider), state, position ("2 of 5"),
  and the text of windows that open. Icon-only buttons get a name.
- New game screen (world, difficulty, monarch), map and timeline, blazon editor (with
  descriptions of every background and emblem, and colour names), end-of-island summary
  (read in full, then browsable line by line).
- Tutorial: the ghost's hints are announced (action expected and where the ghost is).
- On-screen texts (notifications, information bubbles) are read when they appear, and can be
  collected in a browsable list.

**Around you**
- The object selected by the game (the point where you can pay) is announced with its price,
  action, or the reason why it is locked.
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
- An ability (item of power, ruler, mount) being ready again.

**DLC content**
- Norse Lands puzzles: Heimdall (time of day of each pillar, mount required, horn), Thor (current
  rune of each pillar, whether it is right, how many are right), Hel (what each sconce is waiting
  for), Loki (how to proceed and the risks). Solved puzzles are announced.
- Descriptions of the Norse relics (Thor's hammer, Hel's trophy, Heimdall's horn, Loki's staff),
  some Olympus artifacts and many mounts.
- Bomb expedition to the Greed cave: every stage is announced (escort, entrance, crossing,
  guardian, detonation, exit), and while you are beyond the cliff portal the scanner and radar
  only show that area.

## What it does not do (yet)

- **Keyboard only.** Shortcuts are keyboard keys; there is no gamepad mapping for the mod.
- **Single player focus.** Only player 1 is followed. Local and online co-op are not supported
  by the mod, and the Steam invite overlay cannot be read by any mod.
- **Mouse-only screens** that are not standard menus may still be silent. Please report them.
- **Olympus (Call of Olympus)** is only partly covered: its puzzles and quests have not been
  tested yet, and several artifact and mount descriptions are missing on purpose (no reliable
  source).
- **Other game languages** get the game's own texts but the mod's texts in English until a
  translation is added.
- **MelonLoader** is not supported yet (the core is loader-independent; only a BepInEx adapter
  exists). The Mono version of the game has not been tested.

## Keys

Defaults, all configurable. They avoid the game's own keys: player 1 uses WASD, the arrows and
Shift; player 2 uses G, H, J, K, L, I and Right Shift (G and J also start split-screen co-op).
Press **F1** in game for the list of your current shortcuts.

| Key | Action |
|---|---|
| F1 | Help: list of all shortcuts (browse with Page Up / Page Down) |
| Shift+F1 | Repeat the current tutorial hint |
| F11 / Shift+F11 / Ctrl+F11 | Repeat last message / previous / next in the history |
| F2 | Read the current screen (window and selected element; blazon summary) |
| F3 | Put every on-screen text in a browsable list |
| O | Coins and gems ("or" = gold) |
| T | Day, season, time of day, hours until night or dawn |
| M | Mount and whether it is tired |
| R | Relic and abilities, with what they do |
| C | Compass: facing, time of day, danger, nearest wall |
| P | Population: troop census (browse with Page Up / Page Down) |
| V | Radar (browse with Page Up / Page Down) |
| Shift+V | Details of the object in front of you |
| Home / Shift+Home | Scanner: next / previous category |
| Page Up / Page Down | Previous / next item of the last list (scanner, radar, census, help, summary...) |
| Ctrl+Home | Reread the selected item with its current distance |
| End / Shift+End | Walk / run to the selected item (press again to stop) |
| B | Run to the base (castle or campfire) |
| Shift+C | Run to your lost crown |
| Ctrl+Left / Ctrl+Right | Run just behind (inside) the farthest wall on that side |
| Shift+F3 / Ctrl+Shift+F3 | Development: write the object in front of you / the whole island to the log |

## Installation

1. **BepInEx 6 (IL2CPP).** The game needs BepInEx 6 bleeding edge for IL2CPP, with the patches
   that make it work with this game. Follow the install steps of
   [abevol/KingdomMod](https://github.com/abevol/KingdomMod#install) (BepInEx build and the
   Cpp2IL / Il2CppInterop patches). Launch the game once so BepInEx generates its files.
2. **The mod.** Extract the release archive into `Kingdom Two Crowns\BepInEx\plugins`. You should
   get `BepInEx\plugins\KingdomAccess\` containing `KingdomAccess.BepInEx.dll`, `KingdomAccess.Core.dll`,
   `Tolk.dll`, `nvdaControllerClient64.dll`, and the `Lang` and `Sounds` folders.
3. **NVDA users: install the NVDA add-on** `kingdomAccessSleep` (attached to each release; open
   the `.nvda-addon` file with NVDA running). NVDA normally stops speaking at every key press,
   which cuts the mod's announcements all the time in a game; the add-on puts NVDA in sleep
   mode while Kingdom Two Crowns has the focus, so only new announcements interrupt speech.
   NVDA+Shift+S still toggles sleep mode. Without the add-on, press NVDA+Shift+S in the game.
4. **Start the game** with your screen reader running. After a few seconds you should hear
   "Kingdom Access version ... loaded".

## Configuration

`BepInEx\config\kingdom.access.cfg` is created on the first launch. Every setting and every
shortcut is documented in the file. Sections:

1. **General**: on/off, forced language, Windows voice fallback, history size.
2. **Announcements**: object selected by the game, kingdom and camp zones, on-screen texts and
   tutorial, abilities ready.
3. **Radar and scanner**: ranges, explored-area-only mode and its margin, destroyed portals.
4. **Alerts and sounds**: enemy alert and its distance, crown alert, day phases, sounds.
5. **Menus**: menu narration, menu logging (development).
6. **Keys**: every shortcut, e.g. `Wallet = O`, `Radar = V`, `TargetDetails = Shift+V`. Key names
   are Unity key names (`F5`, `PageDown`, `LeftArrow`...); modifiers are `Ctrl`, `Shift`, `Alt`.
   Leave a value empty to disable a shortcut.

Restart the game after editing the file. Sounds are WAV files in `plugins\KingdomAccess\Sounds`:
replace one with your own file of the same name to change it.

## Languages

The mod's texts are in [`Localization/`](Localization/README.md), one JSON file per language.
Adding a language means copying `en.json`, translating the values, and checking it with
`python tools/check_localization.py`. See the [localization guide](Localization/README.md).

## Still to be tested

These parts work in principle but have not been confirmed in game, or only partly:

- Auto-**run** (Shift+End, B, Shift+C, Ctrl+arrows): the mod asks the game to gallop; confirm the
  monarch really runs with every mount.
- Heimdall puzzle: the mount requirement (day-night horse) is deduced from game files.
- Thor puzzle: what makes the pillars active is unknown; the mod reports "inactive".
- Hel and Loki puzzles: announcements written from the game code, not played through yet.
- Call of Olympus: puzzles (Cerberus, Chariot), quests, Hermes staff, mounts.
- Greed cave: "temporary portals" are assumed to be the Greed nests; the distances to the cave
  entrance and detonation point may be wrong.
- Lost crown alert: make sure the worn crown never triggers a false alert.
- On-screen text reading: may be too talkative in some places (can be turned off).

When something is wrong, `Ctrl+Shift+F3` next to it writes the whole island to
`BepInEx\LogOutput.log`: attach that file to your report.

## Building from source

Requirements: Windows, the .NET SDK (6 or later), the game with BepInEx 6 IL2CPP installed and
launched once (the build references the interop assemblies BepInEx generates).

1. Put `Tolk.dll` and `nvdaControllerClient64.dll` (x64) in `lib/native/` (see
   [lib/native/README.md](lib/native/README.md)).
2. If the game is not in the default Steam folder, edit `GameDir` in `Directory.Build.props`.
3. `dotnet build KingdomAccess.slnx -c Release` builds and installs the mod into the game
   (close the game first). Add `-p:NoDeploy=true` to build without installing.

Project layout:

```
src/KingdomAccess.Core      the mod itself, independent of the mod loader
  Speech/                 screen reader output (Tolk), history, sounds
  Localization/           language handling (follows the game language)
  Game/                   access to game objects, unit cache, object names, DLC puzzles
  Features/               announcements, scanner, radar, reports, auto-walk, menus, alerts...
  Patches/                Harmony patches (world map, blazon editor)
src/KingdomAccess.BepInEx   BepInEx 6 IL2CPP adapter (entry point, configuration file)
nvda-addon/                 NVDA add-on: sleep mode for the game (build: python tools/build_nvda_addon.py)
Localization/             mod texts, one file per language
tools/                    sound generator, localization checks, research helpers
```

Development notes: never ask the game for "all objects of type X" with a DLC type that may not
be loaded in the current world (it can crash the IL2CPP runtime); never patch methods that other
mods commonly patch (unit `Awake`); prefer polling to patching virtual methods.

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

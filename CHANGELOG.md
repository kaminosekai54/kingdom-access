# Changelog

## 0.10.0

- **All the game's languages**: German, Spanish, Italian, Portuguese, Russian, Japanese, Korean,
  Simplified and Traditional Chinese, in addition to English and French. The new translations
  are AI-assisted and still need review by native speakers.
- **Call of Olympus world map**: the world view announces each island (oracle, god temples, quest
  islands, Mount Olympus), whether it is locked and which quest unlocks it; the island view (Enter)
  announces each island as left and right change it. The raw internal button names are no longer read.
- Startup: the title screen ("press a key"), the save warning, loading and the intro animation are
  announced, so a new player knows what is happening and when it is their turn.
- Greed cave (first version, to be reworked): a heartbeat guides you to the bomb inside the cave (faster and louder when closer, on
  the side where it is); the bomb's description gives its position, then the cliff portal's; when
  the bomb waits for its fuse, its action is "light the fuse, then get out of the cave fast".
- A short chime when the object in front of you can be paid right now (setting `PaySound`).
- DLC presentation window (new game): which DLC it presents and what Play, Buy and Cancel do.
- Peaceful difficulty warning: what it means (no Greed attacks, achievements disabled).
- The mod log lists the game's languages and their codes, for translators.
- Travel category: the boat bell and the gem chest are no longer named "boat".
- The set-sail point is no longer reported as unavailable when it is not; "unavailable right now" is only said
  in front of an object, where the game's answer is reliable.

## 0.9.0 — first public release

First public version of Kingdom Access, an accessibility mod that makes Kingdom Two Crowns
playable by blind and visually impaired players.

**Speech and controls**
- Speech through the screen reader (Tolk: NVDA, JAWS and others) with the Windows voice as a
  fallback; follows the game language (French and English included).
- Every shortcut on the keyboard and the gamepad, all configurable. Keyboard defaults avoid the
  game's keys and never use Shift (Left Shift triggers mount abilities). Gamepad: hold LB for
  navigation, RB for reports; the game ignores the pad while a layer is held.
- NVDA add-on (`kingdomAccessKeys`): game keys no longer interrupt speech while the game has the
  focus.

**Menus and screens**
- Standard menus, new game screen, map and timeline, blazon editor (every background and emblem
  described), end-of-island summary, tutorial ghost hints, on-screen texts.

**Around you**
- Object selected by the game: level, price, action (with target level), requirements or missing
  coins; moving to another object interrupts the previous announcement.
- While galloping: every useful object ridden past is announced.
- Kingdom and camp zones, day phases, reports (coins, time, mount, relic and abilities, compass,
  census).

**Finding things**
- Scanner by categories (only forest-edge trees in the trees category), radar, auto-walk and
  auto-run to any item, to the base, to the lost crown or behind the outer walls.

**Alerts**
- Enemies in three levels with directional sounds, lost crown, abilities ready.

**DLC content**
- Norse Lands puzzles (Heimdall, Thor, Hel, Loki), bomb expedition to the Greed cave, Call of
  Olympus objects, descriptions of every relic, artifact, Dead Lands monarch power and mount
  ability.

See the README for what still needs testing.

# Localization

Every text spoken by the mod comes from these files. Texts coming from the game itself
(menus, difficulty names, statistics labels...) are read as the game shows them, so they are
already in the game language.

```
Localization/
  Core/     texts of the mod -> installed as BepInEx/plugins/KingdomAccess/Lang/
```

The folder contains one JSON file per language, named after the game's language code
(`en.json`, `fr.json`...). English is the reference.

## How the language is chosen

- The mod follows the game language automatically, and switches when you change it in the
  game options (checked once per second).
- A missing text falls back to English, then to the key itself.
- The `Language` setting in `BepInEx/config/kingdom.access.cfg` forces a language (file name
  without `.json`), whatever the game language.

## Adding a language

1. Copy `Core/en.json` to `Core/xx.json`, where `xx` is the language code the game uses
   (the mod log prints it: `[Language] Mod language: ... (game: xx)`). A code like `pt-BR`
   also matches a `pt.json` file.
2. Translate the values only, never the keys.
3. Keep every `{0}`, `{1}`... placeholder: they are replaced by numbers, names or directions.
4. Check your file: `python tools/check_localization.py` lists missing keys, unknown keys
   and placeholder mismatches.
5. Rebuild, or copy the file into the installed `Lang` folder, and restart the game.

## Conventions

- Texts are spoken, not displayed: keep them short, front-load the important word
  ("Enemy, 12 left" rather than "There is an enemy 12 units to your left").
- `key.*` entries are spoken key names used by the help list and some messages.
- `keyhelp.*` entries describe each shortcut in the help list.
- `blazon.*` entries describe the blazon editor images; they are interpretations of small
  pixel-art images and can be improved.

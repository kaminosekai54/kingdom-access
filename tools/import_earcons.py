# Builds the object sounds (earcons) from Kenney's CC0 sound packs.
#
#   1. Download and extract these packs (CC0, https://kenney.nl) into one folder:
#      Impact Sounds, RPG Audio, Interface Sounds, Casino Audio.
#   2. python tools/import_earcons.py <that folder>
#
# Each sound is converted to mono 16-bit 44.1 kHz WAV, its leading silence removed, cut to at
# most MAX_SECONDS with a short fade-out and normalised, then written to
# src/KingdomAccess.Core/Sounds/Earcons/<family>.wav. Change CHOICES to pick other sounds; any
# WAV file put there by hand works too (the mod falls back to its generated sound when missing).
import os, sys
import numpy as np
import soundfile as sf

CHOICES = {
    "castle":        "impactBell_heavy_000",   # big bell
    "wall":          "impactMining_000",       # pickaxe on stone
    "tower":         "impactMetal_light_002",  # metal ting
    "farm":          "footstep_grass_001",     # rustling crops
    "tree":          "chop",                   # axe chop
    "camp":          "cloth1",                 # tent cloth
    "character":     "question_001",           # someone to talk to
    "statue":        "impactGlass_medium_000", # glass chime
    "treasure":      "metalLatch",             # chest latch
    "danger":        "error_006",              # warning
    "boat":          "creak2",                 # creaking wood
    "puzzle":        "bong_001",               # mysterious bong
    "bomb":          "tick_001",               # ticking (played three times)
    "building":      "impactPlank_medium_000", # wooden plank
    "merchant":      "handleCoins",            # coins
    # Shops: what they sell.
    "shop_bow":      "pluck_001",              # bowstring
    "shop_hammer":   "impactWood_heavy_000",   # hammer on wood
    "shop_scythe":   "knifeSlice",             # blade cutting
    "shop_pike":     "drawKnife1",             # blade drawn
    "shop_shield":   "impactPlate_medium_000", # metal plate struck
    "shop_forge":    "impactMetal_heavy_000",  # anvil
    "shop_ninja":    "impactPunch_medium_000", # martial arts strike
    "shop_workshop": "doorClose_2",            # heavy wooden thud (catapult)
}
# Each family also gets "<family>_build" (lower pitch) and "<family>_upgrade" (higher pitch).
VARIANTS = {"build": 0.72, "upgrade": 1.4}
NO_VARIANTS = {"danger", "character", "puzzle", "bomb", "treasure", "statue", "camp", "tree"}
RATE = 44100
MAX_SECONDS = 0.5
FADE_SECONDS = 0.03
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
OUT = os.path.join(ROOT, "src", "KingdomAccess.Core", "Sounds", "Earcons")


def find(folder, name):
    for dp, _, files in os.walk(folder):
        for f in files:
            if os.path.splitext(f)[0] == name and f.lower().endswith((".ogg", ".wav")):
                return os.path.join(dp, f)
    return None


def prepare(path, pitch=1.0):
    data, rate = sf.read(path, dtype="float32", always_2d=True)
    mono = data.mean(axis=1)
    rate = rate * pitch  # resampling to RATE shifts the pitch by this factor
    if rate != RATE:  # linear resampling is enough for short effects
        n = int(len(mono) * RATE / rate)
        mono = np.interp(np.linspace(0, len(mono) - 1, n), np.arange(len(mono)), mono).astype(np.float32)
    start = np.argmax(np.abs(mono) > 0.02) if np.any(np.abs(mono) > 0.02) else 0
    mono = mono[start:start + int(RATE * MAX_SECONDS)]
    fade = min(len(mono), int(RATE * FADE_SECONDS))
    if fade > 0 and len(mono) >= int(RATE * MAX_SECONDS):
        mono[-fade:] *= np.linspace(1, 0, fade, dtype=np.float32)
    peak = float(np.max(np.abs(mono))) or 1.0
    return mono * (0.9 / peak)


def main():
    if len(sys.argv) < 2:
        sys.exit(__doc__ if __doc__ else "usage: import_earcons.py <kenney folder>")
    src = sys.argv[1]
    os.makedirs(OUT, exist_ok=True)
    missing = []
    for family, name in CHOICES.items():
        path = find(src, name)
        if path is None:
            missing.append(name)
            continue
        sf.write(os.path.join(OUT, family + ".wav"), prepare(path), RATE, subtype="PCM_16")
        print(f"{family:14} <- {os.path.relpath(path, src)}")
        if family not in NO_VARIANTS:
            for state, pitch in VARIANTS.items():
                sf.write(os.path.join(OUT, f"{family}_{state}.wav"), prepare(path, pitch), RATE, subtype="PCM_16")
    with open(os.path.join(OUT, "CREDITS.txt"), "w", encoding="utf-8") as f:
        f.write("Object sounds: Kenney (https://kenney.nl), packs Impact Sounds, RPG Audio, Interface Sounds\n"
                "and Casino Audio, Creative Commons CC0 (public domain). Trimmed and normalised for Kingdom Access.\n")
    if missing:
        sys.exit("Not found: " + ", ".join(missing))


main()

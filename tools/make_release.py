# Builds the release files into dist/:
#   KingdomAccess-<version>.zip          extract into the game folder (contains BepInEx/plugins/KingdomAccess/)
# Run after "dotnet build KingdomAccess.slnx -c Release". Needs lib/native/Tolk.dll and
# lib/native/nvdaControllerClient64.dll (see lib/native/README.md).
import os, re, subprocess, sys, zipfile

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
OUT = os.path.join(ROOT, "src", "KingdomAccess.BepInEx", "bin", "Release")
DIST = os.path.join(ROOT, "dist")
os.makedirs(DIST, exist_ok=True)

version = re.search(r'Version = "([^"]+)"', open(os.path.join(ROOT, "src", "KingdomAccess.Core", "AccessMod.cs"), encoding="utf-8").read()).group(1)
target = "BepInEx/plugins/KingdomAccess/"

files = {
    "KingdomAccess.BepInEx.dll": os.path.join(OUT, "KingdomAccess.BepInEx.dll"),
    "KingdomAccess.Core.dll": os.path.join(OUT, "KingdomAccess.Core.dll"),
    "NAudio.Core.dll": os.path.join(OUT, "NAudio.Core.dll"),
    "NAudio.WinMM.dll": os.path.join(OUT, "NAudio.WinMM.dll"),
    "Tolk.dll": os.path.join(ROOT, "lib", "native", "Tolk.dll"),
    "nvdaControllerClient64.dll": os.path.join(ROOT, "lib", "native", "nvdaControllerClient64.dll"),
}
for folder in ("Lang", "Sounds"):
    base = os.path.join(OUT, folder)
    for dp, _, names in os.walk(base):
        for f in names:
            rel = os.path.relpath(os.path.join(dp, f), base).replace(os.sep, "/")
            files[f"{folder}/{rel}"] = os.path.join(dp, f)

missing = [p for p in files.values() if not os.path.exists(p)]
if missing:
    sys.exit("Missing files (build first, and fill lib/native):\n" + "\n".join(missing))

NOTICES = """Kingdom Access - third-party software

Tolk.dll - Tolk screen reader abstraction library, https://github.com/dkager/tolk
  License: GNU Lesser General Public License v3.0.
nvdaControllerClient64.dll - NVDA controller client, NV Access, https://www.nvaccess.org
  License: GNU Lesser General Public License v2.1.

NAudio.Core.dll, NAudio.WinMM.dll - NAudio, Mark Heath and contributors, https://github.com/naudio/NAudio
  License: MIT.
Sounds/Earcons/*.wav - sounds from Kenney (https://kenney.nl): Impact Sounds, RPG Audio,
  Interface Sounds and Casino Audio packs. License: Creative Commons CC0 (public domain).
  Trimmed and normalised.

These libraries are distributed unmodified. Their source code is available at the addresses above.
"""

zip_path = os.path.join(DIST, f"KingdomAccess-{version}.zip")
with zipfile.ZipFile(zip_path, "w", zipfile.ZIP_DEFLATED) as z:
    for rel, src in files.items():
        z.write(src, target + rel)
    z.write(os.path.join(ROOT, "LICENSE"), target + "LICENSE.txt")
    z.writestr(target + "THIRD-PARTY-NOTICES.txt", NOTICES)
    z.write(os.path.join(ROOT, "README.md"), target + "README.md")
    z.write(os.path.join(ROOT, "README.fr.md"), target + "README.fr.md")
print(zip_path)


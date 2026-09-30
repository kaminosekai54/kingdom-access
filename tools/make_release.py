# Builds the release files into dist/:
#   KingdomAccess-<version>.zip          extract into the game folder (contains BepInEx/plugins/KingdomAccess/)
#   kingdomAccessKeys-<version>.nvda-addon  NVDA add-on
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
    "Tolk.dll": os.path.join(ROOT, "lib", "native", "Tolk.dll"),
    "nvdaControllerClient64.dll": os.path.join(ROOT, "lib", "native", "nvdaControllerClient64.dll"),
}
for folder in ("Lang", "Sounds"):
    for f in os.listdir(os.path.join(OUT, folder)):
        files[f"{folder}/{f}"] = os.path.join(OUT, folder, f)

missing = [p for p in files.values() if not os.path.exists(p)]
if missing:
    sys.exit("Missing files (build first, and fill lib/native):\n" + "\n".join(missing))

NOTICES = """Kingdom Access - third-party software

Tolk.dll - Tolk screen reader abstraction library, https://github.com/dkager/tolk
  License: GNU Lesser General Public License v3.0.
nvdaControllerClient64.dll - NVDA controller client, NV Access, https://www.nvaccess.org
  License: GNU Lesser General Public License v2.1.

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

# NVDA add-on, built next to the zip.
subprocess.run([sys.executable, os.path.join(ROOT, "tools", "build_nvda_addon.py")], check=True)
for f in os.listdir(ROOT):
    if f.endswith(".nvda-addon"):
        os.replace(os.path.join(ROOT, f), os.path.join(DIST, f))
        print(os.path.join(DIST, f))

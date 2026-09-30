# Builds the NVDA add-on (nvda-addon/ -> kingdomAccessSleep-<version>.nvda-addon).
# An .nvda-addon file is a zip archive; open it with NVDA running to install it.
import os, re, zipfile

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..")
SRC = os.path.join(ROOT, "nvda-addon")
manifest = open(os.path.join(SRC, "manifest.ini"), encoding="utf-8").read()
version = re.search(r"^version\s*=\s*(\S+)", manifest, re.M).group(1)
name = re.search(r"^name\s*=\s*(\S+)", manifest, re.M).group(1)
out = os.path.join(ROOT, f"{name}-{version}.nvda-addon")
with zipfile.ZipFile(out, "w", zipfile.ZIP_DEFLATED) as z:
    for dp, _, files in os.walk(SRC):
        for f in files:
            if f.endswith((".pyc",)):
                continue
            p = os.path.join(dp, f)
            z.write(p, os.path.relpath(p, SRC).replace(os.sep, "/"))
print(out)

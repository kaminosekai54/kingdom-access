# Builds numbered, enlarged sheets of the blazon backgrounds and emblems, per series, to describe them.
import os, re
from PIL import Image, ImageDraw

HERE = os.path.dirname(__file__)
REF = os.path.join(HERE, "blazon_ref")
files = os.listdir(REF)

def series(kind, biome):
    mid = f"{kind}_{biome}_" if biome else f"{kind}_"
    rx = re.compile(rf"^banner_1_{mid}(\d+)\.png$")
    return sorted((int(m.group(1)), f) for f in files for m in [rx.match(f)] if m)

def sheet(kind, biome):
    items = series(kind, biome)
    if not items:
        return
    scale, per_row = 4, 10
    w, h = 32 * scale, 96 * scale
    rows = (len(items) + per_row - 1) // per_row
    out = Image.new("RGB", (per_row * (w + 16), rows * (h + 34)), (128, 128, 128))
    d = ImageDraw.Draw(out)
    for i, (n, f) in enumerate(items):
        img = Image.open(os.path.join(REF, f)).convert("RGBA").resize((w, h), Image.NEAREST)
        x, y = (i % per_row) * (w + 16) + 8, (i // per_row) * (h + 34) + 26
        out.paste(img, (x, y), img)
        d.text((x, y - 20), str(n), fill=(255, 0, 0))
    name = f"sheet_{kind}_{biome or 'base'}.png"
    out.save(os.path.join(HERE, name))
    print(name, len(items), [n for n, _ in items][:1], "...", [n for n, _ in items][-1:])

biomes = sorted({m.group(2) for f in files for m in [re.match(r"^banner_1_(pattern|emblem)_([a-z]+)_\d+\.png$", f)] if m})
print("biomes:", biomes)
for kind in ("pattern", "emblem"):
    for b in biomes:
        sheet(kind, b)

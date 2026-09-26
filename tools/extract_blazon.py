# Extracts the blazon background and emblem images from your own copy of the game (local use only,
# to write the spoken descriptions; never redistribute them). Output: tools\blazon_ref\
import os, re, sys
import UnityPy

GAME = r"C:\Program Files (x86)\Steam\steamapps\common\Kingdom Two Crowns\KingdomTwoCrowns_Data"
OUT = os.path.join(os.path.dirname(__file__), "blazon_ref")
os.makedirs(OUT, exist_ok=True)

pattern = re.compile(r"banner.*(pattern|emblem)", re.I)
found = {}
for name in ["resources.assets", "sharedassets0.assets"] + [f"level{i}" for i in range(0, 6)]:
    path = os.path.join(GAME, name)
    if not os.path.exists(path):
        continue
    env = UnityPy.load(path)
    for obj in env.objects:
        if obj.type.name != "Sprite":
            continue
        try:
            data = obj.read()
            n = data.m_Name
        except Exception:
            continue
        if not pattern.search(n) or n in found:
            continue
        try:
            img = data.image
            safe = re.sub(r"[^A-Za-z0-9_ -]", "_", n)
            img.save(os.path.join(OUT, safe + ".png"))
            found[n] = img.size
        except Exception as e:
            print("failed", n, e)
for n in sorted(found, key=lambda s: [int(t) if t.isdigit() else t for t in re.split(r"(\d+)", s)]):
    print(n, found[n])
print("total", len(found))

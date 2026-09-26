# Lists Loc.T("key") / Loc.TryT("key") literals used in the code that are missing from en.json.
import json, os, re
HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, "..", "src")
LOC = os.path.join(HERE, "..", "Localization")
keys = set()
for folder in os.listdir(LOC):
    p = os.path.join(LOC, folder, "en.json")
    if os.path.exists(p):
        keys |= set(json.load(open(p, encoding="utf-8")))
rx = re.compile(r'Loc\.T(?:ryT)?\(\s*"([^"]+)"')
missing = set()
for dp, _, fs in os.walk(SRC):
    if "\\bin" in dp or "\\obj" in dp:
        continue
    for f in fs:
        if f.endswith(".cs"):
            for k in rx.findall(open(os.path.join(dp, f), encoding="utf-8").read()):
                if k not in keys and not k.endswith("."):
                    missing.add((f, k))
for f, k in sorted(missing):
    print("missing:", f, k)
print(len(missing), "missing")

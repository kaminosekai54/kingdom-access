# -*- coding: utf-8 -*-
"""
Checks the translation files in Localization/.

English (en.json) is the reference. For every other language file of the same folder, reports:
  - keys missing from the translation (they fall back to English in game),
  - keys that do not exist in English (probably typos or leftovers),
  - {0}, {1}... placeholders that differ from English (would show raw braces or lose data),
  - empty values.
Exit code 1 if anything is wrong, so it can run in CI.

Usage: python tools/check_localization.py
"""
import json, os, re, sys

ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Localization")
PLACEHOLDER = re.compile(r"\{\d+\}")

def load(path):
    with open(path, encoding="utf-8") as f:
        return json.load(f)

problems = 0
for folder in sorted(os.listdir(ROOT)):
    d = os.path.join(ROOT, folder)
    if not os.path.isdir(d):
        continue
    ref_path = os.path.join(d, "en.json")
    if not os.path.exists(ref_path):
        print(f"[{folder}] en.json missing"); problems += 1; continue
    ref = load(ref_path)
    for k, v in ref.items():
        if not str(v).strip():
            print(f"[{folder}/en] empty value: {k}"); problems += 1
    for name in sorted(os.listdir(d)):
        if not name.endswith(".json") or name == "en.json":
            continue
        lang = name[:-5]
        tr = load(os.path.join(d, name))
        missing = [k for k in ref if k not in tr]
        extra = [k for k in tr if k not in ref]
        for k in missing:
            print(f"[{folder}/{lang}] missing: {k}")
        for k in extra:
            print(f"[{folder}/{lang}] not in English: {k}")
        for k in ref:
            if k in tr:
                if sorted(PLACEHOLDER.findall(ref[k])) != sorted(PLACEHOLDER.findall(tr[k])):
                    print(f"[{folder}/{lang}] placeholders differ: {k}  en={PLACEHOLDER.findall(ref[k])} {lang}={PLACEHOLDER.findall(tr[k])}")
                    problems += 1
                if not str(tr[k]).strip():
                    print(f"[{folder}/{lang}] empty value: {k}"); problems += 1
        problems += len(missing) + len(extra)
        print(f"[{folder}/{lang}] {len(tr)} keys checked against {len(ref)} English keys.")

print("OK" if problems == 0 else f"{problems} problem(s)")
sys.exit(1 if problems else 0)

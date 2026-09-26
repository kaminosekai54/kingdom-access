# Native screen reader libraries

The mod speaks through the user's screen reader using [Tolk](https://github.com/dkager/tolk).
Two 64-bit DLLs must be placed in this folder before building (they are copied next to the mod):

- `Tolk.dll` (x64) — Tolk has no official binary release; build it from the repository above,
  or take the x64 `Tolk.dll` shipped with another accessibility mod.
- `nvdaControllerClient64.dll` — the NVDA controller client, available from the NVDA project
  (NV Access). A recent version is recommended.

These files are third-party software under their own licenses (Tolk: LGPL-3.0,
NVDA controller client: LGPL-2.1) and are not committed to this repository.

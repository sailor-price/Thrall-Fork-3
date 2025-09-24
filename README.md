# Thrall Fork 3

A Valheim companion mod, forked and updated from the original **Thrall** (0.218).  
This repo targets **Valheim 0.221.4** with **BepInEx 5.4.2333** and .NET Framework 4.7.2.

## Structure
- `src/` → Source code for the plugin (C# .NET 4.7.2 Class Library).
- `references/` → DLLs from Valheim `/Managed/`, BepInEx `/core/`, Jotunn, etc.
- `docs/` → Decompiled Thrall sources, ILSpy dumps, notes.

## Build
1. Open `ThrallForked.csproj` in Visual Studio 2022.
2. Make sure `.NET Framework 4.7.2` dev pack is installed.
3. Update `<HintPath>` references in the `.csproj` to point into `/references/`.
4. Build → copy DLL from `bin/Debug` into `Valheim/BepInEx/plugins/ThrallForked/`.

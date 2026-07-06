# DTE10T_WPF — Agent Quick Reference

## Setup & Build Order

1. **First time**: `dotnet restore` (needs `.slnx`) → verify all projects load
2. **Build command**: `dotnet build DTE10T_WPF.csproj -c Release`  *(not solution file at root)*
3. **Run for testing/simulation mode**: 
   ```powershell
   dotnet run --project DTE10T_WPF.csproj
   ```
4. **Single-file deploy (for hardware)**: `dotnet publish .\DTE10T_WPF.csproj -c Release /p:PublishProfile=FolderProfile`

## Project Structure Summary

| Path | Purpose | Owner/Note |
|------|---------|------------|
| `/Models/*.cs` *(14 files)* | Domain models (alarm, PID, output, program step, temp card) — one file per model type (`PVSVModel`, `CommParamModel`, etc.) | Keep 1:1 mapping; no generated code here |
| `/Services/ModbusService.cs` | Modbus RTU over serial. Default is **SIMULATION mode** (returns mock values). To enable real devices, uncomment the NModbus wiring & remove simulation returns. Requires RS-485→USB converter and COM port setup in Windows Device Manager first. |
| `/App.xaml/cs`, `/MainWindow.xaml/cs` | WPF entry UI shell + tab navigation; timing loop for poll at 70Hz (62ms interval) drives all chart refreshes |
| `Program.cs` | Minimal host bootstrapper |

## Key Framework / Tooling Notes

- **Framework**: .NET 8.0, WPF targeting Windows 10+ (`net8.0-windows10.0`). Use `/p:PublishProfile=FolderProfile` in `.csproj`; no global project has one by default.
- **Logging**: `log4net`. Config at root is copied to output directory; changes need app restart unless watched via external config tool (not implemented here).
  - Root config file `log4net.config`, always-copy-to-output: yes
- **Modbus NuGets pinned versions** in `.csproj`: 
  - NModbus v3.0.*  
  - NModbus.Serial v* — use /p:PublishReadyToRun=true for faster cold start (optional, not required locally).

## Testing Workflow

This repo currently has no unit/integration test projects or CI-driven tests. To add them later:
1. Create `/tests/` directory with `.csproj`, target .NET 8 Test SDKs  
2. Use `MockSerialPort` abstraction in `ModbusService` for offline testing; keep a concrete wrapper wired into DI at startup to swap implementations without changing the codebase.

## Hardware Requirements & Caveats (Important!)

1. **WPF native deps**: When publishing single-file, these DLLs live alongside your exe and cannot be bundled:
   - `PresentationNative_cor3.dll`  
   - `libSkiaSharp.*` — needed for rendering; Windows system copies them to System32 in newer releases
2. **COM port must exist before first run**. Connect hardware → Device Manager (Ports) notes COM number, baud 9600 default, or set inside the app via Settings tab. Reboot device after changing comms parameters on target unit—not only software side but also physical DTE10T controller too!
3. **CT vs EVENT**: AUX slot reused; enable one mode and disable other to avoid conflicts in config model (bit flags share same bit).

## Directory Roles & Ownership Quick Map

| Path | Role |
|------|------|
| `/Models/` + `*.csproj`, `.slnx`, `.md`, README* | Library entrypoints, packaging metadata; no UI or business logic here. 14 model files maintain domain representation of DTE registers/maps |
| `/Services/*.cs` | Hardware layer: Modbus wiring for serial port access and error retry/backoff policies. Only `ModbusService.cs` currently present |

## Common Pitfalls to Avoid

- **Don't publish without native deps**: Always copy the native DLLs alongside exe or use folder profile publishing which auto-detects them (if you must single-file).
- **Simulation ≠ deployed behavior until wired**. Default simulation return statements remain in place unless manually uncommented; no CI gate enforces this yet. Keep `ModbusService` comment block intact while developing UI logic to avoid stray serial accesses on a dead COM port or wrong station address/parity settings!

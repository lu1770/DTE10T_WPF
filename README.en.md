# Keyence Station 20.TempController1 — Delta DTE10T WPF Monitoring & Configuration Tool

> 🌐 **Languages / 多语言 / 多言語 / 다국어**
> [English](README.en.md) | [简体中文](README.md) | [繁體中文](README.zh-TW.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

[![CI](https://github.com/lu1770/DTE10T_WPF/actions/workflows/dotnet.yml/badge.svg)](https://github.com/lu1770/DTE10T_WPF/actions/workflows/dotnet.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)

If you find this project helpful, please give it a ⭐ Star!

## Overview

A WPF (.NET 8) host computer software for Delta DTE10T series temperature controllers. It communicates with devices via the **Modbus RTU** protocol, supporting real-time temperature monitoring, parameter reading, and writing.

## Feature Modules

| Tab | Feature | Description |
|-----|---------|-------------|
| 🌡 Real-time Temp | Card-style temperature monitoring | 8-channel PV/SV real-time display, channel visibility toggle |
| 📊 PV/SV Setting | Temperature setpoint & range | View/modify SV target temperature, range limits, sensor type |
| 🔧 PID Parameters | PID tuning | Pb/Ti/Td parameter R/W, manual output, AT auto-tuning trigger |
| 🔔 Alarm Settings | Alarm management | 13 alarm modes, upper/lower limit settings, delay configuration |
| ⚡ Output Config | Output management | OUT1/OUT2 function assignment, output limit, control cycle, reverse output |
| 📐 Advanced Features | Extended features | Ramp control, input compensation, CT current detection, EVENT input, hot runner control |
| 🔌 Comm Parameters | Communication config | Baud rate / parity / data bits / stop bits / protocol / station address |
| 📅 Program Control | Programmable temp control | 8 patterns × 8 steps programmable temperature curves |

## Build & Run

### Prerequisites
- .NET 8 SDK (Windows)
- Visual Studio 2022+ or VS Code + C# Dev Kit

### Build
```bash
dotnet build DTE10T_WPF.csproj -c Release
```

### Run
```bash
dotnet run --project DTE10T_WPF.csproj
```

### Publish Single-File EXE
```bash
dotnet publish DTE10T_WPF.csproj -c Release -r win-x86 --self-contained true -p:PublishSingleFile=true
```

**Publish Notes:**
- Output directory: `bin\Release\net8.0-windows\publish\win-x86\`
- Main executable: `DTE10T_WPF.exe`
- **Note:** WPF applications include some native dependency DLLs (`libSkiaSharp.dll`, `PresentationNative_cor3.dll`, etc.) that cannot be bundled into the exe and must be distributed together
- No .NET runtime installation required (self-contained publish includes it)

**Using Publish Profile (Recommended):**
```bash
dotnet publish DTE10T_WPF.csproj -c Release /p:PublishProfile=FolderProfile
```

## Connect to Real Hardware

1. Connect the DTE10T RS-485 interface to your PC using an RS-485 to USB cable
2. Confirm the COM port number (Device Manager → Ports)
3. Open the software, set the COM port and baud rate (default 9600)
4. Click **Connect**

## Enable Real Modbus Communication

Open `ModbusService.cs` and uncomment the following code:

```csharp
// 1. NuGet install: dotnet add package NModbus
// 2. Uncomment all commented code in ModbusService.cs
// 3. Remove the simulation mode return statement
```

## Communication Parameters (Factory Defaults)

| Parameter | Default | Configurable Range |
|-----------|---------|--------------------|
| Baud Rate | 9600 bps | 2400~115200 |
| Data Bits | 8 bit | 7 / 8 |
| Parity | Even | None / Odd / Even |
| Stop Bits | 1 | 1 / 2 |
| Protocol | RTU | RTU / ASCII |
| Station Address | 1 | 1~247 |

## Project Structure

```
DTE10T_WPF/
├── App.xaml              # Application entry + resource references
├── App.xaml.cs           # Application startup logic
├── Styles.xaml           # Global styles (colors/fonts/control templates)
├── MainWindow.xaml       # Main window UI layout (8 tabs)
├── MainWindow.xaml.cs    # Main window interaction logic + polling timer
├── Models.cs             # All data models (13 Model classes)
├── ModbusService.cs      # Modbus RTU communication service wrapper
├── Program.cs            # Program entry point
└── DTE10T_WPF.csproj    # Project file
```

## Important Notes

1. The DTE10T is an **open-type device** and must be installed in a dust-proof and moisture-proof enclosure
2. Thermocouple compensation cables must match the corresponding type
3. Keep power lines and signal lines separate to avoid interference
4. Communication parameter changes require a **device restart** to take effect
5. CT and EVENT functions are **mutually exclusive** (share the AUX slot)
6. Hot runner control requires enabling ramp control (Bit5 + Bit6)

## Disclaimer

For use alongside the Delta DTE series operation manual, for technical reference only.

## Release

Please refer to [GitHub Releases](https://github.com/lu1770/DTE10T_WPF/releases) for version history.

## License

This project is open-sourced under the MIT License.

```
MIT License

Copyright (c) 2024 Zheng Yao

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## Contact

If you have any questions or suggestions, please reach out via:

- 🐛 [Issues](https://github.com/lu1770/DTE10T_WPF/issues) - Report bugs or request features
- 💡 [Discussions](https://github.com/lu1770/DTE10T_WPF/discussions) - Q&A and general discussion

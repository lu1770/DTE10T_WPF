# 温控器1 — 台达 DTE10T WPF 监控配置工具

> 🌐 **多语言 / Languages / 多言語 / 다국어**
> [简体中文](README.md) | [繁體中文](README.zh-TW.md) | [English](README.en.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

[![CI](https://github.com/lu1770/DTE10T_WPF/actions/workflows/dotnet.yml/badge.svg)](https://github.com/lu1770/DTE10T_WPF/actions/workflows/dotnet.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)

如果你觉得这个项目对你有帮助，欢迎 ⭐ Star！

## 概述

基于 WPF (.NET 8) 开发的台达 DTE10T 系列温度控制器上位机软件，通过 **Modbus RTU** 协议与设备进行通讯，支持实时温度监控、参数读取与写入。

## 功能模块

| Tab | 功能 | 说明 |
|-----|------|------|
| 🌡 实时温度 | 卡片式温度监控 | 8 通道 PV/SV 实时显示，通道可选显示 |
| 📊 PV/SV 设定 | 温度设定与量程 | 查看/修改 SV 目标温度、量程上下限、传感器类型 |
| 🔧 PID 参数 | PID 整定 | Pb/Ti/Td 参数读写，手动输出，AT 自整定触发 |
| 🔔 警报设定 | 警报管理 | 13 种警报模式选择，上下限设定，延迟配置 |
| ⚡ 输出配置 | 输出管理 | OUT1/OUT2 功能分配，输出限幅，控制周期，反向输出 |
| 📐 高级功能 | 扩展功能 | 斜率控制、输入补偿、CT 电流检知、EVENT 输入、热流道控制 |
| 🔌 通讯参数 | 通讯配置 | 波特率/校验/数据位/停止位/协议/站号等系统参数 |
| 📅 可程控 | 程序控温 | 8 样式 × 8 步骤可编程温控曲线 |

## 编译与运行

### 环境要求
- .NET 8 SDK（Windows）
- Visual Studio 2022+ 或 VS Code + C# Dev Kit

### 编译
```bash
dotnet build DTE10T_WPF.csproj -c Release
```

### 运行
```bash
dotnet run --project DTE10T_WPF.csproj
```

### 发布单文件 exe
```bash
dotnet publish DTE10T_WPF.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## 连接真实硬件

1. 用 RS-485 转 USB 线缆连接 DTE10T 的 RS-485 接口
2. 确认 COM 口编号（设备管理器 → 端口）
3. 打开软件，设置 COM 口和波特率（默认 9600）
4. 点击 **连接**

## 启用真实 Modbus 通讯

打开 `ModbusService.cs`，取消以下代码的注释：

```csharp
// 1. NuGet 安装: dotnet add package NModbus
// 2. 取消 ModbusService.cs 中所有注释掉的代码
// 3. 删除模拟模式的 return 语句
```

## 通讯参数（出厂默认）

| 参数 | 默认值 | 可修改范围 |
|------|--------|------------|
| 波特率 | 9600 bps | 2400~115200 |
| 数据位 | 8 bit | 7 / 8 |
| 校验位 | Even | None / Odd / Even |
| 停止位 | 1 | 1 / 2 |
| 协议 | RTU | RTU / ASCII |
| 站号 | 1 | 1~247 |

## 项目结构

```
DTE10T_WPF/
├── App.xaml              # 应用入口 + 资源引用
├── App.xaml.cs           # 应用启动逻辑
├── Styles.xaml           # 全局样式（颜色/字体/控件模板）
├── MainWindow.xaml       # 主窗口 UI 布局（8 个 Tab）
├── MainWindow.xaml.cs    # 主窗口交互逻辑 + 定时轮询
├── Models.cs             # 所有数据模型（13 个 Model 类）
├── ModbusService.cs      # Modbus RTU 通讯服务封装
├── Program.cs            # 程序入口点
└── DTE10T_WPF.csproj    # 项目文件
```

## 注意事项

1. DTE10T 为 **开放型装置**，必须安装在防尘防潮配电箱内
2. 热电偶补偿导线必须使用对应类型
3. 电源线与信号线分开布线，避免干扰
4. 修改通讯参数后需 **重启设备** 生效
5. CT 和 EVENT 功能 **只能二选一**（共享 AUX 插槽）
6. 热流道控制需同时启用斜率控制（Bit5 + Bit6）

## 授权

与台达 DTE 系列操作手册配套使用，仅供技术参考。

## Release

请参阅 [GitHub Releases](https://github.com/lu1770/DTE10T_WPF/releases) 查看版本更新历史。

## License

本项目基于 MIT License 开源。

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

## 联系方式

如果你有任何问题或建议，请通过以下方式联系：

- 🐛 [Issues](https://github.com/lu1770/DTE10T_WPF/issues) - 报告问题或功能请求
- 💡 [Discussions](https://github.com/lu1770/DTE10T_WPF/discussions) - 讨论与问答

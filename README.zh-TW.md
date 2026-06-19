# 基恩士20站.溫控器1 — 台達 DTE10T WPF 監控設定工具

> 🌐 **多語言 / Languages / 多言語 / 다국어**
> [繁體中文](README.zh-TW.md) | [简体中文](README.md) | [English](README.en.md) | [日本語](README.ja.md) | [한국어](README.ko.md)

[![CI](https://github.com/lu1770/DTE10T_WPF/actions/workflows/dotnet.yml/badge.svg)](https://github.com/lu1770/DTE10T_WPF/actions/workflows/dotnet.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)

如果你覺得這個專案對你有幫助，歡迎 ⭐ Star！

## 概述

基於 WPF (.NET 8) 開發的台達 DTE10T 系列溫度控制器上位機軟體，透過 **Modbus RTU** 協定與設備進行通訊，支援即時溫度監控、參數讀取與寫入。

## 功能模組

| 分頁 | 功能 | 說明 |
|-----|------|------|
| 🌡 即時溫度 | 卡片式溫度監控 | 8 通道 PV/SV 即時顯示，通道可選顯示 |
| 📊 PV/SV 設定 | 溫度設定與量程 | 檢視/修改 SV 目標溫度、量程上下限、感測器類型 |
| 🔧 PID 參數 | PID 整定 | Pb/Ti/Td 參數讀寫，手動輸出，AT 自整定觸發 |
| 🔔 警報設定 | 警報管理 | 13 種警報模式選擇，上下限設定，延遲配置 |
| ⚡ 輸出配置 | 輸出管理 | OUT1/OUT2 功能分配，輸出限幅，控制週期，反向輸出 |
| 📐 進階功能 | 擴充功能 | 斜率控制、輸入補償、CT 電流檢知、EVENT 輸入、熱流道控制 |
| 🔌 通訊參數 | 通訊設定 | 鮑率/同位位元/資料位元/停止位元/協定/站號等系統參數 |
| 📅 可程控 | 程序控溫 | 8 樣式 × 8 步驟可程式溫控曲線 |

## 編譯與執行

### 環境需求
- .NET 8 SDK（Windows）
- Visual Studio 2022+ 或 VS Code + C# Dev Kit

### 編譯
```bash
dotnet build DTE10T_WPF.csproj -c Release
```

### 執行
```bash
dotnet run --project DTE10T_WPF.csproj
```

### 發布單一檔案 exe
```bash
dotnet publish DTE10T_WPF.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## 連接真實硬體

1. 使用 RS-485 轉 USB 線纜連接 DTE10T 的 RS-485 介面
2. 確認 COM 埠編號（裝置管理員 → 連接埠）
3. 開啟軟體，設定 COM 埠和鮑率（預設 9600）
4. 點擊 **連接**

## 啟用真實 Modbus 通訊

開啟 `ModbusService.cs`，取消以下程式碼的註解：

```csharp
// 1. NuGet 安裝: dotnet add package NModbus
// 2. 取消 ModbusService.cs 中所有註解掉的程式碼
// 3. 刪除模擬模式的 return 陳述式
```

## 通訊參數（出廠預設）

| 參數 | 預設值 | 可修改範圍 |
|------|--------|------------|
| 鮑率 | 9600 bps | 2400~115200 |
| 資料位元 | 8 bit | 7 / 8 |
| 同位位元 | Even | None / Odd / Even |
| 停止位元 | 1 | 1 / 2 |
| 協定 | RTU | RTU / ASCII |
| 站號 | 1 | 1~247 |

## 專案結構

```
DTE10T_WPF/
├── App.xaml              # 應用程式入口 + 資源引用
├── App.xaml.cs           # 應用程式啟動邏輯
├── Styles.xaml           # 全域樣式（顏色/字體/控制項範本）
├── MainWindow.xaml       # 主視窗 UI 佈局（8 個分頁）
├── MainWindow.xaml.cs    # 主視窗互動邏輯 + 定時輪詢
├── Models.cs             # 所有資料模型（13 個 Model 類別）
├── ModbusService.cs      # Modbus RTU 通訊服務封裝
├── Program.cs            # 程式進入點
└── DTE10T_WPF.csproj    # 專案檔案
```

## 注意事項

1. DTE10T 為 **開放型裝置**，必須安裝在防塵防潮配電箱內
2. 熱電偶補償導線必須使用對應類型
3. 電源線與訊號線分開佈線，避免干擾
4. 修改通訊參數後需 **重新啟動設備** 生效
5. CT 和 EVENT 功能 **只能二選一**（共享 AUX 插槽）
6. 熱流道控制需同時啟用斜率控制（Bit5 + Bit6）

## 授權

與台達 DTE 系列操作手冊配套使用，僅供技術參考。

## Release

請參閱 [GitHub Releases](https://github.com/lu1770/DTE10T_WPF/releases) 查看版本更新歷史。

## License

本專案基於 MIT License 開源。

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

## 聯絡方式

如果你有任何問題或建議，請透過以下方式聯繫：

- 🐛 [Issues](https://github.com/lu1770/DTE10T_WPF/issues) - 報告問題或功能請求
- 💡 [Discussions](https://github.com/lu1770/DTE10T_WPF/discussions) - 討論與問答

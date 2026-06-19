# 基恩士20站.温度調節器1 — デルタ DTE10T WPF モニタリング・設定ツール

> 🌐 **多言語 / Languages / 多语言 / 다국어**
> [日本語](README.ja.md) | [简体中文](README.md) | [繁體中文](README.zh-TW.md) | [English](README.en.md) | [한국어](README.ko.md)

[![CI](https://github.com/lu1770/DTE10T_WPF/actions/workflows/dotnet.yml/badge.svg)](https://github.com/lu1770/DTE10T_WPF/actions/workflows/dotnet.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)

このプロジェクトが役に立ったら、ぜひ ⭐ Star をお願いします！

## 概要

WPF (.NET 8) で開発したデルタ DTE10T シリーズ温度調節器用のパソコンソフトウェアです。**Modbus RTU** プロトコルで機器と通信し、リアルタイム温度モニタリング、パラメータの読み書きに対応しています。

## 機能モジュール

| タブ | 機能 | 説明 |
|-----|------|------|
| 🌡 リアルタイム温度 | カード型温度モニタリング | 8 チャンネル PV/SV リアルタイム表示、チャンネル表示の切り替え |
| 📊 PV/SV 設定 | 温度設定とレンジ | SV 目標温度、レンジ上下限、センサー種類の確認・変更 |
| 🔧 PID パラメータ | PID 整定 | Pb/Ti/Td パラメータ読み書き、手動出力、AT 自動整定トリガー |
| 🔔 アラーム設定 | アラーム管理 | 13 種類のアラームモード選択、上下限設定、遅延設定 |
| ⚡ 出力設定 | 出力管理 | OUT1/OUT2 機能割り当て、出力リミット、制御周期、逆出力 |
| 📐 高度機能 | 拡張機能 | ランプ制御、入力補償、CT 電流検知、EVENT 入力、ホットランナー制御 |
| 🔌 通信パラメータ | 通信設定 | ボーレート/パリティ/データ長/ストップビット/プロトコル/局番など |
| 📅 プログラム制御 | プログラム温度制御 | 8 パターン × 8 ステップのプログラマブル温度プロファイル |

## ビルドと実行

### 動作環境
- .NET 8 SDK（Windows）
- Visual Studio 2022+ または VS Code + C# Dev Kit

### ビルド
```bash
dotnet build DTE10T_WPF.csproj -c Release
```

### 実行
```bash
dotnet run --project DTE10T_WPF.csproj
```

### 単一ファイル EXE の発行
```bash
dotnet publish DTE10T_WPF.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## 実機への接続

1. RS-485 変換 USB ケーブルで DTE10T の RS-485 インターフェースに接続
2. COM ポート番号を確認（デバイスマネージャー → ポート）
3. ソフトウェアを開き、COM ポートとボーレート（デフォルト 9600）を設定
4. **接続** をクリック

## Modbus 通信の有効化

`ModbusService.cs` を開き、以下のコードのコメントを解除してください：

```csharp
// 1. NuGet インストール: dotnet add package NModbus
// 2. ModbusService.cs 内のすべてのコメントアウトされたコードを解除
// 3. シミュレーションモードの return 文を削除
```

## 通信パラメータ（出荷時デフォルト）

| パラメータ | デフォルト | 設定範囲 |
|-----------|---------|---------|
| ボーレート | 9600 bps | 2400~115200 |
| データ長 | 8 bit | 7 / 8 |
| パリティ | Even | None / Odd / Even |
| ストップビット | 1 | 1 / 2 |
| プロトコル | RTU | RTU / ASCII |
| 局番 | 1 | 1~247 |

## プロジェクト構成

```
DTE10T_WPF/
├── App.xaml              # アプリケーションエントリ + リソース参照
├── App.xaml.cs           # アプリケーション起動ロジック
├── Styles.xaml           # グローバルスタイル（色/フォント/コントロールテンプレート）
├── MainWindow.xaml       # メインウィンドウ UI レイアウト（8 タブ）
├── MainWindow.xaml.cs    # メインウィンドウインタラクションロジック + 定期ポーリング
├── Models.cs             # すべてのデータモデル（13 個の Model クラス）
├── ModbusService.cs      # Modbus RTU 通信サービスラッパー
├── Program.cs            # プログラムエントリポイント
└── DTE10T_WPF.csproj    # プロジェクトファイル
```

## 注意事項

1. DTE10T は **開放型装置** です。防塵・防湿の配電盤内に設置してください
2. 熱電対の補償導線は対応する種類を使用してください
3. 電源線と信号線は分離して配線し、干渉を避けてください
4. 通信パラメータの変更後は **機器の再起動** が必要です
5. CT と EVENT 機能は **どちらか一方のみ** 使用可能（AUX スロットを共有）
6. ホットランナー制御にはランプ制御の同時有効化が必要（Bit5 + Bit6）

## 免責事項

デルタ DTE シリーズ取扱説明書と併用し、技術参考用です。

## Release

バージョン履歴は [GitHub Releases](https://github.com/lu1770/DTE10T_WPF/releases) をご覧ください。

## License

本プロジェクトは MIT License のもとでオープンソースとして公開されています。

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

## お問い合わせ

ご質問やご提案がございましたら、以下よりお問い合わせください：

- 🐛 [Issues](https://github.com/lu1770/DTE10T_WPF/issues) - 不具合報告や機能リクエスト
- 💡 [Discussions](https://github.com/lu1770/DTE10T_WPF/discussions) - 議論と Q&A

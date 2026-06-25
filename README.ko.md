# 온도조절기1 — 델타 DTE10T WPF 모니터링 및 설정 도구

> 🌐 **다국어 / Languages / 多语言 / 多言語**
> [한국어](README.ko.md) | [简体中文](README.md) | [繁體中文](README.zh-TW.md) | [English](README.en.md) | [日本語](README.ja.md)

[![CI](https://github.com/lu1770/DTE10T_WPF/actions/workflows/dotnet.yml/badge.svg)](https://github.com/lu1770/DTE10T_WPF/actions/workflows/dotnet.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/)

이 프로젝트가 도움이 되었다면 ⭐ Star를 눌러주세요!

## 개요

WPF (.NET 8)로 개발된 델타 DTE10T 시리즈 온도조절기 상위 기기 소프트웨어입니다. **Modbus RTU** 프로토콜을 통해 장비와 통신하며, 실시간 온도 모니터링 및 매개변수 읽기/쓰기를 지원합니다.

## 기능 모듈

| 탭 | 기능 | 설명 |
|-----|------|------|
| 🌡 실시간 온도 | 카드형 온도 모니터링 | 8채널 PV/SV 실시간 표시, 채널 표시 전환 |
| 📊 PV/SV 설정 | 온도 설정 및 범위 | SV 목표 온도, 범위 상하한, 센서 유형 확인/수정 |
| 🔧 PID 매개변수 | PID 정정 | Pb/Ti/Td 매개변수 읽기/쓰기, 수동 출력, AT 자동 정정 트리거 |
| 🔔 알람 설정 | 알람 관리 | 13가지 알람 모드 선택, 상하한 설정, 지연 구성 |
| ⚡ 출력 구성 | 출력 관리 | OUT1/OUT2 기능 할당, 출력 제한, 제어 주기, 역 출력 |
| 📐 고급 기능 | 확장 기능 | 램프 제어, 입력 보상, CT 전류 감지, EVENT 입력, 핫러너 제어 |
| 🔌 통신 매개변수 | 통신 구성 | 보드레이트/패리티/데이터 비트/정지 비트/프로토콜/국번 등 |
| 📅 프로그램 제어 | 프로그램 온도 제어 | 8 패턴 × 8 단계 프로그래머블 온도 곡선 |

## 빌드 및 실행

### 환경 요구사항
- .NET 8 SDK（Windows）
- Visual Studio 2022+ 또는 VS Code + C# Dev Kit

### 빌드
```bash
dotnet build DTE10T_WPF.csproj -c Release
```

### 실행
```bash
dotnet run --project DTE10T_WPF.csproj
```

### 단일 파일 exe 게시
```bash
dotnet publish DTE10T_WPF.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## 실제 하드웨어 연결

1. RS-485 to USB 케이블로 DTE10T의 RS-485 인터페이스에 연결
2. COM 포트 번호 확인（장치 관리자 → 포트）
3. 소프트웨어를 열고 COM 포트와 보드레이트（기본값 9600）설정
4. **연결** 클릭

## 실제 Modbus 통신 활성화

`ModbusService.cs`를 열고 다음 코드의 주석을 해제하세요:

```csharp
// 1. NuGet 설치: dotnet add package NModbus
// 2. ModbusService.cs의 모든 주석 처리된 코드 해제
// 3. 시뮬레이션 모드의 return 문 삭제
```

## 통신 매개변수（출고 기본값）

| 매개변수 | 기본값 | 변경 가능 범위 |
|------|--------|------------|
| 보드레이트 | 9600 bps | 2400~115200 |
| 데이터 비트 | 8 bit | 7 / 8 |
| 패리티 | Even | None / Odd / Even |
| 정지 비트 | 1 | 1 / 2 |
| 프로토콜 | RTU | RTU / ASCII |
| 국번 | 1 | 1~247 |

## 프로젝트 구조

```
DTE10T_WPF/
├── App.xaml              # 애플리케이션 진입점 + 리소스 참조
├── App.xaml.cs           # 애플리케이션 시작 로직
├── Styles.xaml           # 전역 스타일（색상/글꼴/컨트롤 템플릿）
├── MainWindow.xaml       # 메인 창 UI 레이아웃（8개 탭）
├── MainWindow.xaml.cs    # 메인 창 상호작용 로직 + 주기적 폴링
├── Models.cs             # 모든 데이터 모델（13개 Model 클래스）
├── ModbusService.cs      # Modbus RTU 통신 서비스 래퍼
├── Program.cs            # 프로그램 진입점
└── DTE10T_WPF.csproj    # 프로젝트 파일
```

## 주의사항

1. DTE10T은 **개방형 장치**이므로 방진·방습 배전반 내에 설치해야 합니다
2. 열전대 보상 도선은 해당 유형을 사용해야 합니다
3. 전원선과 신호선은 분리 배선하여 간섭을 피하세요
4. 통신 매개변수 변경 후 **장비 재시작**이 필요합니다
5. CT와 EVENT 기능은 **둘 중 하나만** 사용 가능（AUX 슬롯 공유）
6. 핫러너 제어에는 램프 제어를 동시에 활성화해야 합니다（Bit5 + Bit6）

## 면책 조항

델타 DTE 시리즈 조작 설명서와 함께 사용하며, 기술 참고용입니다.

## Release

버전 업데이트 내역은 [GitHub Releases](https://github.com/lu1770/DTE10T_WPF/releases)를 참조하세요.

## License

본 프로젝트는 MIT License 하에 오픈소스로 공개되어 있습니다.

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

## 연락처

질문이나 제안이 있으시면 다음을 통해 연락해 주세요:

- 🐛 [Issues](https://github.com/lu1770/DTE10T_WPF/issues) - 문제 보고 또는 기능 요청
- 💡 [Discussions](https://github.com/lu1770/DTE10T_WPF/discussions) - 토론 및 Q&A

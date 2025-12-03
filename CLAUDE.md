# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 建置與執行指令

```bash
# 建置專案
dotnet build UrlProxy.sln

# 執行應用程式
dotnet run --project UrlProxy/UrlProxy.csproj

# 發佈 Release 版本
dotnet publish UrlProxy/UrlProxy.csproj -c Release
```

## 專案概述

UrlProxy 是一個 Windows WPF 應用程式（.NET 8.0），用於執行 HTTPS 反向代理伺服器。讓行動裝置可以透過電腦的 Wi-Fi IP 位址連線到本機 API 伺服器。

**主要功能：**
- 執行基於 Kestrel 的 HTTPS 代理伺服器，將請求轉發到可設定的目標 URL
- 產生包含所有本機 IP 的自簽憑證（含 SAN）
- 自動管理 Windows 防火牆規則
- 顯示 QR Code 方便行動裝置連線
- 最小化至系統匣並顯示狀態圖示

## 架構

**MVVM 模式：**
- `MainWindow.xaml.cs` - View，包含系統匣整合（Hardcodet.NotifyIcon.Wpf）
- `ViewModels/MainViewModel.cs` - 管理伺服器生命週期、設定持久化及 QR Code 產生
- `Models/Settings.cs` - JSON 格式設定檔，儲存於 `%APPDATA%/UrlProxy/settings.json`

**Services：**
- `ProxyServer.cs` - ASP.NET Core Kestrel 伺服器，設定為 HTTPS 並處理完整的請求/回應代理
- `CertificateGenerator.cs` - 建立自簽 X.509 憑證，SAN 包含 localhost、127.0.0.1、10.0.2.2（Android 模擬器）及所有本機 IP
- `FirewallManager.cs` - 使用 `netsh` 新增/移除防火牆規則（規則名稱："AATest"）
- `NetworkHelper.cs` - 偵測 Wi-Fi 及其他網路介面的 IPv4 位址

## 主要相依套件

- **QRCoder** - QR Code 產生
- **Hardcodet.NotifyIcon.Wpf** - 系統匣圖示支援
- **System.Drawing.Common** - 系統匣圖示動態繪製
- **Microsoft.AspNetCore.App** - Kestrel 網頁伺服器，用於 HTTPS 代理

## 備註

- 應用程式需要系統管理員權限才能管理防火牆規則
- 代理伺服器接受目標的所有 SSL 憑證（略過驗證）
- CORS 設定為完全開放（允許任何來源、方法、標頭）

# UrlProxy

一個 Windows 桌面應用程式，讓行動裝置可以透過 Wi-Fi 連線到本機開發中的 API 伺服器。

## 解決的問題

在開發 API 時，經常需要用手機測試。但 IIS Express 或其他本機開發伺服器通常只綁定 `localhost`，手機無法直接連線。

傳統解決方案需要：
- 修改 `applicationhost.config`
- 執行 `netsh` 註冊 URL
- 手動設定防火牆規則
- 處理 HTTPS 憑證問題

**UrlProxy 讓這一切變得簡單** — 只需設定目標 API 網址，點擊啟動即可。

## 功能

- 自動偵測本機 Wi-Fi IP
- 自動產生包含 SAN 的自簽 HTTPS 憑證
- 自動管理 Windows 防火牆規則
- QR Code 快速連線
- 支援 HTTP/HTTPS 切換
- 系統匣最小化運行
- 連線記錄即時顯示

## 截圖

```
┌─────────────────────────────────────────────┐
│  UrlProxy                                   │
├─────────────────────────────────────────────┤
│  ┌─────────┐  ● 運行中                      │
│  │ QR Code │                                │
│  │         │  代理伺服器（手機連線用）        │
│  └─────────┘  本機 IP    192.168.1.100      │
│  [  啟動  ]   連線網址   https://192.168... │
│                                             │
│               ▸ 設定                        │
├─────────────────────────────────────────────┤
│  連線記錄                          [清除]   │
│  ┌─────────────────────────────────────┐   │
│  │ 12:34:56 伺服器已啟動: https://...  │   │
│  │ 12:34:56 代理目標: http://localho...│   │
│  │ 12:34:55 防火牆規則已開啟: AAProxy..│   │
│  └─────────────────────────────────────┘   │
└─────────────────────────────────────────────┘
```

## 使用方式

1. 啟動你的本機 API 伺服器（例如 IIS Express）
2. 開啟 UrlProxy
3. 在「設定 → 轉發目標」輸入本機 API 網址（例如 `http://localhost:5059`）
4. 點擊「啟動」
5. 手機掃描 QR Code 或輸入連線網址即可存取 API

## 系統需求

- Windows 10/11
- .NET 8.0 Runtime
- 系統管理員權限（用於防火牆規則）

## 安裝

### 方式一：下載 Release

從 [Releases](../../releases) 頁面下載最新版本。

### 方式二：從原始碼建置

```bash
git clone https://github.com/yourusername/UrlProxy.git
cd UrlProxy
dotnet build
dotnet run --project UrlProxy/UrlProxy.csproj
```

## 設定說明

| 設定項目 | 說明 |
|---------|------|
| 監聽 Port | 代理伺服器的監聽埠（預設 3000） |
| 使用 HTTPS | 是否啟用 HTTPS（預設開啟） |
| 防火牆規則 | Windows 防火牆規則名稱 |
| API 網址 | 要轉發的本機 API 位址 |

## 運作原理

```
手機/客戶端                    UrlProxy                      本機 API
     │                           │                              │
     │  GET /api/users           │                              │
     │ ────────────────────────► │                              │
     │  https://192.168.1.x:3000 │   GET /api/users             │
     │                           │ ───────────────────────────► │
     │                           │   http://localhost:5059      │
     │                           │                              │
     │                           │   200 OK                     │
     │   200 OK                  │ ◄─────────────────────────── │
     │ ◄──────────────────────── │                              │
     │                           │                              │
```

## 技術細節

- **框架**: WPF (.NET 8.0)
- **代理伺服器**: ASP.NET Core Kestrel
- **憑證**: System.Security.Cryptography（產生含 SAN 的 X.509 憑證）
- **防火牆**: netsh advfirewall
- **QR Code**: QRCoder
- **系統匣**: Hardcodet.NotifyIcon.Wpf

## 注意事項

- 代理伺服器會接受目標的所有 SSL 憑證（略過驗證），僅適合開發測試使用
- CORS 設定為完全開放
- 自簽憑證在手機上會顯示不安全警告，需手動信任或略過

## License

MIT License

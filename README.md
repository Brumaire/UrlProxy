# UrlProxy

A Windows desktop application that allows mobile devices to connect to local development API servers via Wi-Fi.

[中文版 README](README.zh-TW.md)

## Problem Solved

When developing APIs, you often need to test on mobile devices. However, IIS Express and other local development servers typically only bind to `localhost`, making them inaccessible from mobile devices.

Traditional solutions require:
- Modifying `applicationhost.config`
- Running `netsh` to register URLs
- Manually configuring firewall rules
- Handling HTTPS certificate issues

**UrlProxy makes this simple** — just set the target API URL and click Start.

## Features

- Auto-detect local Wi-Fi IP
- Auto-generate self-signed HTTPS certificates with SAN
- Auto-manage Windows firewall rules
- QR Code for quick connection
- HTTP/HTTPS toggle support
- System tray minimization
- Real-time connection logs

## Screenshot

```
┌─────────────────────────────────────────────┐
│  UrlProxy                                   │
├─────────────────────────────────────────────┤
│  ┌─────────┐  ● Running                     │
│  │ QR Code │                                │
│  │         │  Proxy Server (for mobile)     │
│  └─────────┘  Local IP   192.168.1.100      │
│  [  Start  ]  URL        https://192.168... │
│                                             │
│               ▸ Settings                    │
├─────────────────────────────────────────────┤
│  Logs                              [Clear]  │
│  ┌─────────────────────────────────────┐   │
│  │ 12:34:56 Server started: https://...│   │
│  │ 12:34:56 Forward target: http://... │   │
│  │ 12:34:55 Firewall rule enabled: ... │   │
│  └─────────────────────────────────────┘   │
└─────────────────────────────────────────────┘
```

## Usage

1. Start your local API server (e.g., IIS Express)
2. Open UrlProxy
3. In "Settings → Forward Target", enter your local API URL (e.g., `http://localhost:5059`)
4. Click "Start"
5. Scan the QR Code or enter the URL on your mobile device to access the API

## System Requirements

- Windows 10/11
- .NET 8.0 Runtime
- Administrator privileges (for firewall rules)

## Installation

### Option 1: Download Release

Download the latest version from the [Releases](../../releases) page.

### Option 2: Build from Source

```bash
git clone https://github.com/yourusername/UrlProxy.git
cd UrlProxy
dotnet build
dotnet run --project UrlProxy/UrlProxy.csproj
```

## Settings

| Setting | Description |
|---------|-------------|
| Listen Port | Proxy server listening port (default: 3000) |
| Use HTTPS | Enable HTTPS (default: on) |
| Firewall Rule | Windows firewall rule name |
| API URL | Local API address to forward to |

## How It Works

```
Mobile/Client                      UrlProxy                        Local API
     │                                │                                │
     │  GET /api/users                │                                │
     │ ─────────────────────────────► │                                │
     │  https://192.168.1.x:3000      │   GET /api/users               │
     │                                │ ─────────────────────────────► │
     │                                │   http://localhost:5059        │
     │                                │                                │
     │                                │   200 OK                       │
     │   200 OK                       │ ◄───────────────────────────── │
     │ ◄───────────────────────────── │                                │
     │                                │                                │
```

## Technical Details

- **Framework**: WPF (.NET 8.0)
- **Proxy Server**: ASP.NET Core Kestrel
- **Certificate**: System.Security.Cryptography (X.509 with SAN)
- **Firewall**: netsh advfirewall
- **QR Code**: QRCoder
- **System Tray**: Hardcodet.NotifyIcon.Wpf

## Notes

- The proxy server accepts all SSL certificates from the target (bypasses validation) — for development use only
- CORS is fully open
- Self-signed certificates will show security warnings on mobile devices; manually trust or bypass as needed

## License

MIT License

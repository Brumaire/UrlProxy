using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using UrlProxy.Models;
using UrlProxy.Services;
using QRCoder;

namespace UrlProxy.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private ProxyServer? _server;
    private readonly Settings _settings;

    public MainViewModel()
    {
        _settings = Settings.Load();
        Port = _settings.Port;
        TargetUrl = _settings.TargetUrl;
        FirewallRuleName = _settings.FirewallRuleName;
        UseHttps = _settings.UseHttps;
        UpdateWiFiIP();
    }

    // Properties
    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        set
        {
            if (_isRunning != value)
            {
                _isRunning = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(StatusColor));
                OnPropertyChanged(nameof(ButtonText));
                OnPropertyChanged(nameof(CanEditSettings));
            }
        }
    }

    private int _port;
    public int Port
    {
        get => _port;
        set
        {
            if (_port != value)
            {
                _port = value;
                OnPropertyChanged();
                UpdateConnectionUrl();
            }
        }
    }

    private string _targetUrl = "http://localhost:5059";
    public string TargetUrl
    {
        get => _targetUrl;
        set
        {
            if (_targetUrl != value)
            {
                _targetUrl = value;
                OnPropertyChanged();
            }
        }
    }

    private string _firewallRuleName = "AAProxyRule";
    public string FirewallRuleName
    {
        get => _firewallRuleName;
        set
        {
            if (_firewallRuleName != value)
            {
                _firewallRuleName = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _useHttps = true;
    public bool UseHttps
    {
        get => _useHttps;
        set
        {
            if (_useHttps != value)
            {
                _useHttps = value;
                OnPropertyChanged();
                UpdateConnectionUrl();
            }
        }
    }

    private string? _wifiIP;
    public string? WiFiIP
    {
        get => _wifiIP;
        set
        {
            if (_wifiIP != value)
            {
                _wifiIP = value;
                OnPropertyChanged();
                UpdateConnectionUrl();
            }
        }
    }

    private string _connectionUrl = "";
    public string ConnectionUrl
    {
        get => _connectionUrl;
        set
        {
            if (_connectionUrl != value)
            {
                _connectionUrl = value;
                OnPropertyChanged();
                UpdateQRCode();
            }
        }
    }

    private ImageSource? _qrCodeImage;
    public ImageSource? QRCodeImage
    {
        get => _qrCodeImage;
        set
        {
            _qrCodeImage = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<string> Logs { get; } = new();

    public string StatusText => IsRunning ? "運行中" : "已停止";
    public Brush StatusColor => IsRunning ? Brushes.LimeGreen : Brushes.Red;
    public string ButtonText => IsRunning ? "停止" : "啟動";
    public bool CanEditSettings => !IsRunning;

    // Methods
    public void UpdateWiFiIP()
    {
        WiFiIP = NetworkHelper.GetWiFiIP() ?? "找不到 Wi-Fi";
    }

    private void UpdateConnectionUrl()
    {
        var protocol = UseHttps ? "https" : "http";
        if (!string.IsNullOrEmpty(WiFiIP) && WiFiIP != "找不到 Wi-Fi")
        {
            ConnectionUrl = $"{protocol}://{WiFiIP}:{Port}";
        }
        else
        {
            ConnectionUrl = $"{protocol}://localhost:{Port}";
        }
    }

    private void UpdateQRCode()
    {
        try
        {
            using var qrGenerator = new QRCodeGenerator();
            var qrCodeData = qrGenerator.CreateQrCode(ConnectionUrl, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            var qrCodeBytes = qrCode.GetGraphic(10);

            using var ms = new MemoryStream(qrCodeBytes);
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = ms;
            bitmap.EndInit();
            bitmap.Freeze();

            QRCodeImage = bitmap;
        }
        catch
        {
            QRCodeImage = null;
        }
    }

    public void ToggleServer()
    {
        if (IsRunning)
        {
            StopServer();
        }
        else
        {
            StartServer();
        }
    }

    private void StartServer()
    {
        try
        {
            // 儲存設定
            _settings.Port = Port;
            _settings.TargetUrl = TargetUrl;
            _settings.FirewallRuleName = FirewallRuleName;
            _settings.UseHttps = UseHttps;
            _settings.Save();

            // 取得所有 IP
            var allIPs = NetworkHelper.GetAllIPs().Select(x => x.IP).ToList();

            // 產生憑證 (僅在使用 HTTPS 時)
            X509Certificate2? cert = null;
            if (UseHttps)
            {
                cert = CertificateGenerator.GenerateSelfSignedCertificate(allIPs);
            }

            // 新增防火牆規則
            if (FirewallManager.AddRule(Port, FirewallRuleName))
            {
                AddLog($"防火牆規則已開啟: {FirewallRuleName}");
            }
            else
            {
                AddLog("警告: 無法新增防火牆規則");
            }

            // 建立並啟動伺服器
            _server = new ProxyServer(Port, TargetUrl, cert, UseHttps);
            _server.OnRequest += entry =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Logs.Insert(0, entry.ToString());
                    if (Logs.Count > 100) Logs.RemoveAt(Logs.Count - 1);
                });
            };
            _server.Start();

            IsRunning = true;
            AddLog($"代理目標: {TargetUrl}");
            AddLog($"伺服器已啟動: {ConnectionUrl}");
        }
        catch (Exception ex)
        {
            AddLog($"錯誤: {ex.Message}");
            MessageBox.Show($"啟動失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void StopServer()
    {
        try
        {
            _server?.Stop();
            _server?.Dispose();
            _server = null;

            // 移除防火牆規則
            if (FirewallManager.RemoveRule())
            {
                AddLog("防火牆規則已關閉");
            }

            IsRunning = false;
            AddLog("伺服器已停止");
        }
        catch (Exception ex)
        {
            AddLog($"錯誤: {ex.Message}");
        }
    }

    public void AddLog(string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Logs.Insert(0, $"{DateTime.Now:HH:mm:ss} {message}");
            if (Logs.Count > 100) Logs.RemoveAt(Logs.Count - 1);
        });
    }

    public void ClearLogs()
    {
        Logs.Clear();
    }

    public void CopyToClipboard(string text)
    {
        try
        {
            Clipboard.SetText(text);
        }
        catch
        {
            // 忽略錯誤
        }
    }

    public void Cleanup()
    {
        StopServer();
    }

    // INotifyPropertyChanged
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

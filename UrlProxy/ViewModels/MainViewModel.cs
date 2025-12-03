using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QRCoder;
using UrlProxy.Models;
using UrlProxy.Services;

namespace UrlProxy.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly Settings _settings;
    private ProxyServer? _server;

    #region Constructor

    public MainViewModel()
    {
        _settings = Settings.Load();
        _port = _settings.Port;
        _targetUrl = _settings.TargetUrl;
        _firewallRuleName = _settings.FirewallRuleName;
        _useHttps = _settings.UseHttps;
        UpdateWiFiIP();
    }

    #endregion

    #region Properties - Server State

    private bool _isRunning;
    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (_isRunning == value) return;
            _isRunning = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(StatusText));
            OnPropertyChanged(nameof(StatusColor));
            OnPropertyChanged(nameof(ButtonText));
            OnPropertyChanged(nameof(CanEditSettings));
        }
    }

    public string StatusText => IsRunning ? "Running" : "Stopped";
    public Brush StatusColor => IsRunning ? Brushes.LimeGreen : Brushes.Red;
    public string ButtonText => IsRunning ? "Stop" : "Start";
    public bool CanEditSettings => !IsRunning;

    #endregion

    #region Properties - Settings

    private int _port;
    public int Port
    {
        get => _port;
        set
        {
            if (_port == value) return;
            _port = value;
            OnPropertyChanged();
            UpdateConnectionUrl();
        }
    }

    private string _targetUrl = "http://localhost:5059";
    public string TargetUrl
    {
        get => _targetUrl;
        set
        {
            if (_targetUrl == value) return;
            _targetUrl = value;
            OnPropertyChanged();
        }
    }

    private string _firewallRuleName = "AAProxyRule";
    public string FirewallRuleName
    {
        get => _firewallRuleName;
        set
        {
            if (_firewallRuleName == value) return;
            _firewallRuleName = value;
            OnPropertyChanged();
        }
    }

    private bool _useHttps = true;
    public bool UseHttps
    {
        get => _useHttps;
        set
        {
            if (_useHttps == value) return;
            _useHttps = value;
            OnPropertyChanged();
            UpdateConnectionUrl();
        }
    }

    #endregion

    #region Properties - Connection Info

    private string? _wifiIP;
    public string? WiFiIP
    {
        get => _wifiIP;
        private set
        {
            if (_wifiIP == value) return;
            _wifiIP = value;
            OnPropertyChanged();
            UpdateConnectionUrl();
        }
    }

    private string _connectionUrl = "";
    public string ConnectionUrl
    {
        get => _connectionUrl;
        private set
        {
            if (_connectionUrl == value) return;
            _connectionUrl = value;
            OnPropertyChanged();
            UpdateQRCode();
        }
    }

    private ImageSource? _qrCodeImage;
    public ImageSource? QRCodeImage
    {
        get => _qrCodeImage;
        private set
        {
            if (_qrCodeImage == value) return;
            _qrCodeImage = value;
            OnPropertyChanged();
        }
    }

    #endregion

    #region Properties - Logs

    public ObservableCollection<string> Logs { get; } = new();

    #endregion

    #region Public Methods

    public void UpdateWiFiIP()
    {
        WiFiIP = NetworkHelper.GetWiFiIP() ?? "No Wi-Fi found";
    }

    public void ToggleServer()
    {
        if (IsRunning)
            StopServer();
        else
            StartServer();
    }

    public void AddLog(string message)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Logs.Insert(0, $"{DateTime.Now:HH:mm:ss} {message}");
            if (Logs.Count > 100)
                Logs.RemoveAt(Logs.Count - 1);
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
            // Ignore clipboard errors
        }
    }

    public void Cleanup()
    {
        StopServer();
    }

    #endregion

    #region Private Methods

    private void UpdateConnectionUrl()
    {
        var protocol = UseHttps ? "https" : "http";
        var host = !string.IsNullOrEmpty(WiFiIP) && WiFiIP != "No Wi-Fi found"
            ? WiFiIP
            : "localhost";
        ConnectionUrl = $"{protocol}://{host}:{Port}";
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

    private void StartServer()
    {
        try
        {
            SaveSettings();

            var allIPs = NetworkHelper.GetAllIPs().Select(x => x.IP).ToList();

            // Generate certificate (HTTPS only)
            X509Certificate2? cert = null;
            if (UseHttps)
                cert = CertificateGenerator.GenerateSelfSignedCertificate(allIPs);

            // Add firewall rule
            if (FirewallManager.AddRule(Port, FirewallRuleName))
                AddLog($"Firewall rule enabled: {FirewallRuleName}");
            else
                AddLog("Warning: Failed to add firewall rule");

            // Create and start server
            _server = new ProxyServer(Port, TargetUrl, cert, UseHttps);
            _server.OnRequest += OnServerRequest;
            _server.Start();

            IsRunning = true;
            AddLog($"Forward target: {TargetUrl}");
            AddLog($"Server started: {ConnectionUrl}");
        }
        catch (Exception ex)
        {
            AddLog($"Error: {ex.Message}");
            MessageBox.Show($"Failed to start: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void StopServer()
    {
        try
        {
            _server?.Stop();
            _server?.Dispose();
            _server = null;

            // Remove firewall rule
            if (FirewallManager.RemoveRule())
                AddLog("Firewall rule disabled");

            IsRunning = false;
            AddLog("Server stopped");
        }
        catch (Exception ex)
        {
            AddLog($"Error: {ex.Message}");
        }
    }

    private void SaveSettings()
    {
        _settings.Port = Port;
        _settings.TargetUrl = TargetUrl;
        _settings.FirewallRuleName = FirewallRuleName;
        _settings.UseHttps = UseHttps;
        _settings.Save();
    }

    private void OnServerRequest(LogEntry entry)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            Logs.Insert(0, entry.ToString());
            if (Logs.Count > 100)
                Logs.RemoveAt(Logs.Count - 1);
        });
    }

    #endregion

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}

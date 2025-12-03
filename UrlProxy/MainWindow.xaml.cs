using System.Windows;
using System.Drawing;
using UrlProxy.ViewModels;

namespace UrlProxy;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private bool _isExiting = false;

    public MainWindow()
    {
        InitializeComponent();
        _viewModel = new MainViewModel();
        DataContext = _viewModel;

        // 設定系統匣圖示
        UpdateTrayIcon();
        _viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(_viewModel.IsRunning))
            {
                UpdateTrayIcon();
            }
        };
    }

    private void UpdateTrayIcon()
    {
        // 使用程式碼產生圖示
        TrayIcon.Icon = CreateIcon(_viewModel.IsRunning ? System.Drawing.Color.LimeGreen : System.Drawing.Color.Gray);
    }

    private System.Drawing.Icon CreateIcon(System.Drawing.Color color)
    {
        using var bitmap = new Bitmap(16, 16);
        using var g = Graphics.FromImage(bitmap);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.Clear(System.Drawing.Color.Transparent);
        using var brush = new SolidBrush(color);
        g.FillEllipse(brush, 2, 2, 12, 12);
        return System.Drawing.Icon.FromHandle(bitmap.GetHicon());
    }

    private void ToggleServer_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ToggleServer();
    }

    private void CopyIP_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_viewModel.WiFiIP))
        {
            _viewModel.CopyToClipboard(_viewModel.WiFiIP);
            _viewModel.AddLog("已複製 IP 到剪貼簿");
        }
    }

    private void CopyUrl_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_viewModel.ConnectionUrl))
        {
            _viewModel.CopyToClipboard(_viewModel.ConnectionUrl);
            _viewModel.AddLog("已複製網址到剪貼簿");
        }
    }

    private void ClearLogs_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ClearLogs();
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide();
            TrayIcon.ShowBalloonTip("UrlProxy", "程式已最小化到系統匣", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (!_isExiting)
        {
            e.Cancel = true;
            Hide();
            TrayIcon.ShowBalloonTip("UrlProxy", "程式已最小化到系統匣", Hardcodet.Wpf.TaskbarNotification.BalloonIcon.Info);
        }
        else
        {
            _viewModel.Cleanup();
            TrayIcon.Dispose();
        }
    }

    private void TrayIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
    {
        ShowMainWindow();
    }

    private void ShowWindow_Click(object sender, RoutedEventArgs e)
    {
        ShowMainWindow();
    }

    private void ShowMainWindow()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        _isExiting = true;
        _viewModel.Cleanup();
        TrayIcon.Dispose();
        Application.Current.Shutdown();
    }
}

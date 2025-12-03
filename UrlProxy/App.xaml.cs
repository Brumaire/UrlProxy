using System.Windows;
using UrlProxy.Services;

namespace UrlProxy;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // 確保關閉時移除防火牆規則
        FirewallManager.RemoveRule();
        base.OnExit(e);
    }
}

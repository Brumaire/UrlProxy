using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace UrlProxy.Services;

public class ProxyServer : IDisposable
{
    private WebApplication? _app;
    private readonly X509Certificate2? _certificate;
    private Task? _runTask;

    public int Port { get; }
    public string TargetUrl { get; }
    public bool UseHttps { get; }
    public bool IsRunning { get; private set; }

    public event Action<LogEntry>? OnRequest;

    public ProxyServer(int port, string targetUrl, X509Certificate2? certificate, bool useHttps = true)
    {
        Port = port;
        TargetUrl = targetUrl.TrimEnd('/');
        _certificate = certificate;
        UseHttps = useHttps;
    }

    public void Start()
    {
        if (IsRunning) return;

        try
        {
            var builder = WebApplication.CreateBuilder();

            // 設定 Kestrel
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Listen(IPAddress.Any, Port, listenOptions =>
                {
                    if (UseHttps && _certificate != null)
                    {
                        listenOptions.UseHttps(_certificate);
                    }
                });
            });

            // 關閉預設 logging
            builder.Logging.ClearProviders();

            // 加入 HttpClient
            builder.Services.AddHttpClient("proxy", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
            }).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            });

            // 加入 CORS
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            _app = builder.Build();

            _app.UseCors();

            // 處理所有請求
            _app.Map("/{**path}", async (HttpContext context, IHttpClientFactory httpClientFactory) =>
            {
                await HandleRequestAsync(context, httpClientFactory);
            });

            _runTask = Task.Run(async () =>
            {
                try
                {
                    await _app.RunAsync();
                }
                catch
                {
                    // 忽略關閉時的錯誤
                }
            });

            IsRunning = true;
        }
        catch (Exception ex)
        {
            IsRunning = false;
            throw new Exception($"無法啟動伺服器: {ex.Message}", ex);
        }
    }

    public void Stop()
    {
        if (!IsRunning) return;

        try
        {
            _app?.StopAsync().Wait(TimeSpan.FromSeconds(5));
            _app?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(5));
        }
        catch
        {
            // 忽略錯誤
        }

        _app = null;
        IsRunning = false;
    }

    private async Task HandleRequestAsync(HttpContext context, IHttpClientFactory httpClientFactory)
    {
        var startTime = DateTime.Now;
        int statusCode = 500;
        var path = context.Request.Path + context.Request.QueryString;

        try
        {
            var client = httpClientFactory.CreateClient("proxy");

            // 建立目標 URL
            var targetUri = new Uri(TargetUrl + path);

            // 建立請求
            var requestMessage = new HttpRequestMessage
            {
                Method = new HttpMethod(context.Request.Method),
                RequestUri = targetUri
            };

            // 複製 Headers
            foreach (var header in context.Request.Headers)
            {
                if (!IsRestrictedHeader(header.Key))
                {
                    requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            // 複製 Body
            if (context.Request.ContentLength > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding"))
            {
                var ms = new MemoryStream();
                await context.Request.Body.CopyToAsync(ms);
                ms.Position = 0;
                requestMessage.Content = new StreamContent(ms);

                if (context.Request.ContentType != null)
                {
                    requestMessage.Content.Headers.ContentType =
                        System.Net.Http.Headers.MediaTypeHeaderValue.Parse(context.Request.ContentType);
                }
            }

            // 發送請求
            var response = await client.SendAsync(requestMessage);
            statusCode = (int)response.StatusCode;

            // 設定回應狀態碼
            context.Response.StatusCode = statusCode;

            // 複製回應 Headers
            foreach (var header in response.Headers)
            {
                if (!IsRestrictedResponseHeader(header.Key))
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }
            }

            foreach (var header in response.Content.Headers)
            {
                if (!IsRestrictedResponseHeader(header.Key))
                {
                    context.Response.Headers[header.Key] = header.Value.ToArray();
                }
            }

            // 複製回應 Body
            await response.Content.CopyToAsync(context.Response.Body);
        }
        catch (Exception ex)
        {
            statusCode = 500;
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync($"{{\"error\":\"{ex.Message}\"}}");
        }
        finally
        {
            // 記錄請求
            OnRequest?.Invoke(new LogEntry
            {
                Time = startTime,
                Method = context.Request.Method,
                Path = path,
                StatusCode = statusCode
            });
        }
    }

    private static bool IsRestrictedHeader(string header)
    {
        var restricted = new[] { "Host", "Content-Length", "Transfer-Encoding", "Connection" };
        return restricted.Contains(header, StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsRestrictedResponseHeader(string header)
    {
        var restricted = new[] { "Transfer-Encoding", "Connection" };
        return restricted.Contains(header, StringComparer.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        Stop();
        _certificate?.Dispose();
    }
}

public class LogEntry
{
    public DateTime Time { get; set; }
    public string Method { get; set; } = "";
    public string Path { get; set; } = "";
    public int StatusCode { get; set; }

    public override string ToString()
    {
        var displayPath = Path.Length > 40 ? Path[..37] + "..." : Path;
        return $"{Time:HH:mm:ss} {Method,-6} {displayPath,-40} {StatusCode}";
    }
}

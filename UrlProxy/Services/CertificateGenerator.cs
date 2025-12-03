using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace UrlProxy.Services;

public static class CertificateGenerator
{
    public static X509Certificate2 GenerateSelfSignedCertificate(List<string> ipAddresses)
    {
        using var rsa = RSA.Create(2048);

        var request = new CertificateRequest(
            "CN=localhost",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        // 加入 Subject Alternative Names
        var sanBuilder = new SubjectAlternativeNameBuilder();
        sanBuilder.AddDnsName("localhost");
        sanBuilder.AddIpAddress(System.Net.IPAddress.Parse("127.0.0.1"));
        sanBuilder.AddIpAddress(System.Net.IPAddress.Parse("10.0.2.2")); // Android 模擬器

        foreach (var ip in ipAddresses)
        {
            if (System.Net.IPAddress.TryParse(ip, out var ipAddr))
            {
                sanBuilder.AddIpAddress(ipAddr);
            }
        }

        request.CertificateExtensions.Add(sanBuilder.Build());

        // 加入基本約束
        request.CertificateExtensions.Add(
            new X509BasicConstraintsExtension(false, false, 0, false));

        // 加入金鑰用途
        request.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
                false));

        // 加入增強型金鑰用途 (Server Authentication)
        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, // Server Auth
                false));

        // 建立自簽憑證，有效期 365 天
        var cert = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(365));

        // 匯出並重新匯入以包含私鑰
        var pfxBytes = cert.Export(X509ContentType.Pfx);
        return new X509Certificate2(pfxBytes, (string?)null, X509KeyStorageFlags.Exportable);
    }
}

using Microsoft.Extensions.DependencyInjection;

namespace AwsEmulatorSharp.Emulator.Certificates;

public static class CertificatesModuleExtensions
{
    public static IServiceCollection AddCertificatesModule(this IServiceCollection services)
    {
        var configurationReader = new CertificatesConfigurationReader();
        var configuration = configurationReader.Read();

        services.AddSingleton(configuration);
        services.AddSingleton<CertificateFileWriter>();
        services.AddSingleton<AcmCertificateAuthority>();
        services.AddSingleton<CertificateStore>();
        services.AddSingleton<CertificatesResponseWriter>();
        services.AddSingleton<CertificatesSurface>();

        return services;
    }
}

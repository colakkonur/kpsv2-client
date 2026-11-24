using KpsV2;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// Uzantı metotları, tüketici tarafta ek using gerektirmesin diye DI namespace'inde.
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>KPS istemcisinin DI kaydı.</summary>
public static class KpsServiceCollectionExtensions
{
    /// <summary>
    /// <see cref="IKpsClient"/>'ı singleton olarak kaydeder ve ayarları verilen yapılandırma bölümünden okur.
    /// <code>builder.Services.AddKpsClient(builder.Configuration.GetSection("Kps"));</code>
    /// Dönen <see cref="IHttpClientBuilder"/> ile HttpClient'a handler/proxy/retry eklenebilir.
    /// </summary>
    public static IHttpClientBuilder AddKpsClient(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddOptions<KpsOptions>().Bind(configuration);
        return AddKpsCore(services);
    }

    /// <summary><see cref="IKpsClient"/>'ı singleton olarak kaydeder ve ayarları koddan verir.</summary>
    public static IHttpClientBuilder AddKpsClient(this IServiceCollection services, Action<KpsOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.AddOptions<KpsOptions>().Configure(configure);
        return AddKpsCore(services);
    }

    private static IHttpClientBuilder AddKpsCore(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        // Singleton: token önbelleği tüm istekler arasında paylaşılır. HttpClient'lar factory'den alınır.
        services.TryAddSingleton<IKpsClient>(sp => new KpsClient(
            sp.GetRequiredService<IHttpClientFactory>(),
            sp.GetRequiredService<IOptions<KpsOptions>>(),
            sp.GetService<ILogger<KpsClient>>()));

        return services.AddHttpClient(KpsClient.HttpClientName, (sp, client) =>
            client.Timeout = sp.GetRequiredService<IOptions<KpsOptions>>().Value.Timeout);
    }
}

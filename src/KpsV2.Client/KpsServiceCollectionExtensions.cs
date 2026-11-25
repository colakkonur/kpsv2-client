using KpsV2;
using KpsV2.Nvi;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

// Uzantı metotları, tüketici tarafta ek using gerektirmesin diye DI namespace'inde.
namespace Microsoft.Extensions.DependencyInjection;

/// <summary>KPS istemcilerinin (Sağlık Bakanlığı KPS ve NVİ KPS) DI kaydı.</summary>
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

    /// <summary>
    /// NVİ KPS istemcisini (<see cref="INviKpsClient"/>) singleton olarak kaydeder ve ayarları verilen yapılandırma bölümünden okur.
    /// <code>builder.Services.AddNviKpsClient(builder.Configuration.GetSection("NviKps"));</code>
    /// Sağlık Bakanlığı KPS'si (<see cref="AddKpsClient(IServiceCollection, IConfiguration)"/>) ile birlikte kaydedilebilir.
    /// </summary>
    public static IHttpClientBuilder AddNviKpsClient(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        services.AddOptions<NviKpsOptions>().Bind(configuration);
        return AddNviKpsCore(services);
    }

    /// <summary><see cref="INviKpsClient"/>'ı singleton olarak kaydeder ve ayarları koddan verir.</summary>
    public static IHttpClientBuilder AddNviKpsClient(this IServiceCollection services, Action<NviKpsOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        services.AddOptions<NviKpsOptions>().Configure(configure);
        return AddNviKpsCore(services);
    }

    private static IHttpClientBuilder AddNviKpsCore(IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton<INviKpsClient>(sp => new NviKpsClient(
            sp.GetRequiredService<IHttpClientFactory>(),
            sp.GetRequiredService<IOptions<NviKpsOptions>>(),
            sp.GetService<ILogger<NviKpsClient>>()));

        return services.AddHttpClient(NviKpsClient.HttpClientName, (sp, client) =>
            client.Timeout = sp.GetRequiredService<IOptions<NviKpsOptions>>().Value.Timeout);
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

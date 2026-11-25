namespace KpsV2.Nvi;

/// <summary>
/// NVİ (Nüfus ve Vatandaşlık İşleri) KPS bağlantı ayarları. appsettings.json'da varsayılan olarak <c>"NviKps"</c> bölümünden okunur.
/// <para>
/// NVİ KPS, Sağlık Bakanlığı KPS'sinden (<see cref="KpsOptions"/>) ayrı bir sistemdir: başvuru kpsbasvuru.nvi.gov.tr üzerinden yapılır,
/// kurum/uygulama kodu yoktur, yalnızca NVİ'nin verdiği kullanıcı adı ve şifre kullanılır. Herkese açık test ortamı yoktur;
/// sorgu servisine yalnızca başvuruda tanımlı IP'lerden erişilebilir.
/// </para>
/// </summary>
public sealed class NviKpsOptions
{
    /// <summary>appsettings.json'daki varsayılan bölüm adı.</summary>
    public const string SectionName = "NviKps";

    /// <summary>NVİ'nin token servisi (STS).</summary>
    public const string DefaultStsAddress = "https://kimlikdogrulama.nvi.gov.tr/Services/Issuer.svc/IWSTrust13";

    /// <summary>NVİ KPS sorgu servisi (Router).</summary>
    public const string DefaultKpsAddress = "https://kpsv2.nvi.gov.tr/Services/RoutingService.svc";

    /// <summary>NVİ'nin başvuru onayında verdiği web servis kullanıcı adı.</summary>
    public string Username { get; set; } = "";

    /// <summary>NVİ'nin başvuru onayında verdiği web servis şifresi.</summary>
    public string Password { get; set; } = "";

    /// <summary>HTTP isteği zaman aşımı (DI ile kullanımda). Varsayılan 60 sn.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>Token süresi dolmadan bu kadar önce yenilenir. Varsayılan 1 dk.</summary>
    public TimeSpan TokenRefreshMargin { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>Geçici hatalarda (bağlantı, zaman aşımı, 5xx) ek deneme sayısı. 0: kapalı. Varsayılan 2.</summary>
    public int RetryCount { get; set; } = 2;

    /// <summary>İlk yeniden denemeden önce beklenecek süre; sonraki her denemede katlanır. Varsayılan 1 sn.</summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Sorgudan önce kimlik numarasının kontrol hanelerini doğrula. Varsayılan: açık.</summary>
    public bool ValidateIdentityNumber { get; set; } = true;

    /// <summary>STS adresini ezmek için (ör. ara proxy). Boşsa <see cref="DefaultStsAddress"/>.</summary>
    public Uri? StsAddress { get; set; }

    /// <summary>Sorgu servisi adresini ezmek için (ör. ara proxy). Boşsa <see cref="DefaultKpsAddress"/>.</summary>
    public Uri? KpsAddress { get; set; }

    internal string EffectiveStsAddress => StsAddress?.ToString() ?? DefaultStsAddress;

    internal string EffectiveKpsAddress => KpsAddress?.ToString() ?? DefaultKpsAddress;

    /// <summary>Eksik/hatalı ayar varsa <see cref="KpsConfigurationException"/> fırlatır.</summary>
    public void Validate()
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(Username)) missing.Add(nameof(Username));
        if (string.IsNullOrEmpty(Password)) missing.Add(nameof(Password));
        if (missing.Count > 0)
            throw new KpsConfigurationException($"NVİ KPS ayarları eksik: {string.Join(", ", missing)}.");
        if (Timeout <= TimeSpan.Zero)
            throw new KpsConfigurationException("Timeout sıfırdan büyük olmalı.");
        if (RetryCount < 0 || RetryDelay < TimeSpan.Zero)
            throw new KpsConfigurationException("RetryCount ve RetryDelay negatif olamaz.");
    }
}

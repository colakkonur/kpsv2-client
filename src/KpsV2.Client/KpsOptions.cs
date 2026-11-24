namespace KpsV2;

/// <summary>KPS ortamı.</summary>
public enum KpsEnvironment
{
    /// <summary>kpsv2test.saglik.gov.tr — IP kısıtı yok, yalnızca örnek kimlik numaraları sorgulanabilir.</summary>
    Test,

    /// <summary>kpsv2.saglik.gov.tr — gerçek veri; yalnızca KPS'ye tanımlı IP'lerden erişilebilir.</summary>
    Prod,
}

/// <summary>
/// KPS bağlantı ayarları. appsettings.json'da varsayılan olarak <c>"Kps"</c> bölümünden okunur.
/// <para>
/// Kullanıcı tipine göre: <b>standart kullanıcı</b> (HBYS, AHBS...) için <see cref="InstitutionCode"/> + <see cref="ApplicationCode"/>,
/// <b>uygulama kullanıcısı</b> (merkez teşkilat) için <see cref="RequesterIdentityNumber"/> doldurulur.
/// </para>
/// </summary>
public sealed class KpsOptions
{
    /// <summary>appsettings.json'daki varsayılan bölüm adı.</summary>
    public const string SectionName = "Kps";

    /// <summary>Test veya Prod. Varsayılan: <see cref="KpsEnvironment.Test"/>.</summary>
    public KpsEnvironment Environment { get; set; } = KpsEnvironment.Test;

    /// <summary>Sorgulamayı yapacak kişinin T.C. kimlik numarası.</summary>
    public string Username { get; set; } = "";

    /// <summary>KPS Yönetim Sistemi'nde (kpsyonetimv2.saglik.gov.tr) belirlenen web servis şifresi.</summary>
    public string Password { get; set; } = "";

    /// <summary>Standart kullanıcı: sorgulamayı yapan sağlık kuruluşunun kodu (KPS: KurumKodu).</summary>
    public string? InstitutionCode { get; set; }

    /// <summary>Standart kullanıcı: Kimlik Paylaşım Birimi'nin uygulamaya verdiği GUID (KPS: UygulamaKodu).</summary>
    public string? ApplicationCode { get; set; }

    /// <summary>Uygulama kullanıcısı: sorgulayan kişinin kimlik numarası (KPS: SorgulayanKimlikNo).</summary>
    public string? RequesterIdentityNumber { get; set; }

    /// <summary>HTTP isteği zaman aşımı (DI ile kullanımda). Varsayılan 60 sn.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>Token süresi dolmadan bu kadar önce yenilenir. Varsayılan 1 dk.</summary>
    public TimeSpan TokenRefreshMargin { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Geçici hatalarda (bağlantı, zaman aşımı, KPS 5xx) ek deneme sayısı. KPS sorguları salt okunur olduğu için
    /// yeniden denemek güvenlidir. 0: kapalı. Varsayılan 2.
    /// </summary>
    public int RetryCount { get; set; } = 2;

    /// <summary>İlk yeniden denemeden önce beklenecek süre; sonraki her denemede katlanır. Varsayılan 1 sn.</summary>
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Sorgudan önce kimlik numarasının kontrol hanelerini doğrula. Varsayılan: açık.</summary>
    public bool ValidateIdentityNumber { get; set; } = true;

    /// <summary>Ortam adresini ezmek için (ör. ara proxy). Boşsa <see cref="Environment"/>'a göre seçilir.</summary>
    public Uri? BaseAddress { get; set; }

    /// <summary>Uygulama kullanıcısı mı (RequesterIdentityNumber dolu)?</summary>
    public bool IsApplicationUser => !string.IsNullOrWhiteSpace(RequesterIdentityNumber);

    internal Uri EffectiveBaseAddress => BaseAddress ?? new Uri(Environment == KpsEnvironment.Prod
        ? "https://kpsv2.saglik.gov.tr"
        : "https://kpsv2test.saglik.gov.tr");

    /// <summary>Eksik/çelişkili ayar varsa <see cref="KpsConfigurationException"/> fırlatır.</summary>
    public void Validate()
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(Username)) missing.Add(nameof(Username));
        if (string.IsNullOrEmpty(Password)) missing.Add(nameof(Password));
        if (!IsApplicationUser)
        {
            if (string.IsNullOrWhiteSpace(InstitutionCode)) missing.Add(nameof(InstitutionCode));
            if (string.IsNullOrWhiteSpace(ApplicationCode)) missing.Add(nameof(ApplicationCode));
        }
        if (missing.Count > 0)
            throw new KpsConfigurationException(
                $"KPS ({Environment}) ayarları eksik: {string.Join(", ", missing)}. " +
                "Standart kullanıcı için InstitutionCode + ApplicationCode, uygulama kullanıcısı için RequesterIdentityNumber gerekir.");
        if (Timeout <= TimeSpan.Zero)
            throw new KpsConfigurationException("Timeout sıfırdan büyük olmalı.");
        if (RetryCount < 0 || RetryDelay < TimeSpan.Zero)
            throw new KpsConfigurationException("RetryCount ve RetryDelay negatif olamaz.");
    }

    /// <summary>
    /// kpsv2test.saglik.gov.tr "Örnek Kodlar" sayfasında herkese açık yayınlanan standart test kullanıcısı.
    /// Yalnızca test ortamında, örnek kimlik numaralarıyla çalışır.
    /// </summary>
    public static KpsOptions PublicTestUser() => new()
    {
        Environment = KpsEnvironment.Test,
        Username = "test_user",
        Password = "f6)@6U:l",
        InstitutionCode = "123456",
        ApplicationCode = "8353df93-453c-4e23-8be8-2f913dd35313",
    };
}

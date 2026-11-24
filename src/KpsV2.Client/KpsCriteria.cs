using KpsV2.Contracts;

namespace KpsV2;

/// <summary>
/// KPS sorgu kriterlerini kısa yoldan oluşturur. WSDL tipleri doğrudan da kullanılabilir
/// (ör. <c>new YakinlikSorgulaKriter { KimlikNo = ..., DogumGun = ..., ... }</c>).
/// </summary>
public static class KpsCriteria
{
    /// <summary>TcKimlikNoSorgula kriteri. KPS tüm alanların dolu olmasını ister.</summary>
    public static KisiSorgulaKisiBilgileriCO Person(string firstName, string lastName, string fatherName, string motherName,
        ECinsiyet gender, DateOnly birthDate, string birthPlace) => new()
    {
        Adi = firstName,
        Soyadi = lastName,
        BabaAdi = fatherName,
        AnneAdi = motherName,
        Cinsiyet = gender,
        DogumTarihi = birthDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified),
        DogumYeri = birthPlace,
    };

    /// <summary>
    /// Aile (yakınlık) sorgusu kriteri: KimlikNoIleVatandasAileBilgisi / MaviKartliAileBilgisi (+KardesDahil).
    /// Doğum tarihi, sorgulanan kişiyi doğrulamak için istenir.
    /// </summary>
    public static YakinlikSorgulaKriter Kinship(long identityNumber, DateOnly birthDate) => new()
    {
        KimlikNo = identityNumber,
        DogumGun = birthDate.Day,
        DogumAy = birthDate.Month,
        DogumYil = birthDate.Year,
    };

    /// <summary>Kamu yakınlık sorgusu kriteri: KimlikNoIleVatandasAileYakinlikBilgisi (+KardesDahil).</summary>
    public static KamuYakinlikSorgulaKriter PublicKinship(long identityNumber, DateOnly birthDate) => new()
    {
        KimlikNo = identityNumber,
        DogumGun = birthDate.Day,
        DogumAy = birthDate.Month,
        DogumYil = birthDate.Year,
    };

    /// <summary>AileBireyleriSorgula kriteri. <paramref name="wholeFamily"/>: false = Aile, true = TumAile.</summary>
    public static AileBireyleriSorgulaKriter FamilyMembers(long identityNumber, bool wholeFamily = false) => new()
    {
        TCKimlikNo = identityNumber,
        Tip = wholeFamily ? EAileSorguTipi.TumAile : EAileSorguTipi.Aile,
    };
}

/// <summary>YetkiListesi satırı: bir servis metoduna yetkiniz olup olmadığı.</summary>
public sealed record KpsPermission(string Service, string Method, bool IsAuthorized);

/// <summary>Bir dönüş alanının yetki durumu. <see cref="Path"/> ör. "TCVatandasiKisiKutukleri/KisiBilgisi/TemelBilgisi/Ad".</summary>
public sealed record KpsFieldPermission(string Path, string Type, bool IsAuthorized);

/// <summary>YetkiListesi sonucu (<c>WcfService[]</c>) için uzantılar.</summary>
public static class KpsPermissionExtensions
{
    /// <summary>KPS'nin Kps servisinin YetkiListesi'ndeki adı.</summary>
    public const string KpsServiceName = "Kps Servisleri";

    /// <summary>KPS'nin Parametre servisinin YetkiListesi'ndeki adı.</summary>
    public const string ParameterServiceName = "Parametre Servisleri";

    /// <summary>Servis/metot bazında düz yetki listesi.</summary>
    public static IReadOnlyList<KpsPermission> MethodPermissions(this WcfService[]? services) =>
        (services ?? Array.Empty<WcfService>())
            .Where(s => s is not null)
            .SelectMany(s => (s.Methods ?? Array.Empty<Method>())
                .Where(m => m is not null)
                .Select(m => new KpsPermission(s.Name ?? "", m.Name ?? "", m.IsAuthorized == true)))
            .ToList();

    /// <summary>Verilen metoda (ör. "BilesikKisiSorgula") yetki var mı? Servis adı verilmezse tüm servislerde aranır.</summary>
    public static bool IsAuthorized(this WcfService[]? services, string method, string? service = null) =>
        services.MethodPermissions().Any(y => y.IsAuthorized && y.Method == method && (service is null || y.Service == service));

    /// <summary>
    /// Bir metodun dönen alanları için alan bazında yetkiler (ör. "TCVatandasiKisiKutukleri/KisiBilgisi/TemelBilgisi/Ad").
    /// Yetkisiz alanlar KPS cevabında null döner; bir alanın neden boş geldiğini anlamak için kullanın.
    /// </summary>
    public static IReadOnlyList<KpsFieldPermission> FieldPermissions(this WcfService[]? services, string method, string service = KpsServiceName)
    {
        var m = (services ?? Array.Empty<WcfService>())
            .Where(s => s?.Name == service)
            .SelectMany(s => s.Methods ?? Array.Empty<Method>())
            .FirstOrDefault(x => x?.Name == method);
        var list = new List<KpsFieldPermission>();
        Collect(m?.ReturnProperties, "", list);
        return list;

        static void Collect(Property[]? properties, string prefix, List<KpsFieldPermission> list)
        {
            foreach (var p in properties ?? Array.Empty<Property>())
            {
                if (p is null) continue;
                var path = prefix.Length == 0 ? p.Name : $"{prefix}/{p.Name}";
                list.Add(new KpsFieldPermission(path, p.Type ?? "", p.IsAuthorized == true));
                Collect(p.SubProperties, path, list);
            }
        }
    }
}

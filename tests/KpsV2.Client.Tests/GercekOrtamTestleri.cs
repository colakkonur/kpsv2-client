using KpsV2.Contracts;
using Microsoft.Extensions.Configuration;

namespace KpsV2.Client.Tests;

// Gerçek KPS sunucularına giden testler. Varsayılan olarak ATLANIR; birim testleri sunucuya gitmez.
//
// Test ortamı (kpsv2test, herkese açık test kullanıcısı — bilgi girmeye gerek yok):
//   KPS_TEST_ENV=1 dotnet test --filter Environment=Test
//
// Prod ortamı: bilgiler deponun kökündeki TEK dosyadan okunur: appsettings.json (şablon: appsettings.example.json).
// Dosya .gitignore'dadır, depoya girmez. Yalnızca KPS'ye tanımlı IP'den çalışır.
//   dotnet test --filter Environment=Prod

internal static class GercekOrtam
{
    /// <summary>Depo kökündeki (KpsV2.sln'in yanındaki) appsettings.json; yoksa null.</summary>
    public static string? AyarDosyasi { get; } = DepoKokundekiAyarDosyasi();

    public static IConfiguration Ayarlar { get; } = AyarDosyasi is null
        ? new ConfigurationBuilder().Build()
        : new ConfigurationBuilder().AddJsonFile(AyarDosyasi).Build();

    public static bool TestAcik => Environment.GetEnvironmentVariable("KPS_TEST_ENV") == "1";

    /// <summary>appsettings.json'daki "Kps" bölümü doluysa prod ayarları, değilse null.</summary>
    public static KpsOptions? Prod
    {
        get
        {
            var s = Ayarlar.GetSection(KpsOptions.SectionName);
            if (string.IsNullOrWhiteSpace(s["Username"]) || s["Username"]!.StartsWith('<')) return null;
            var o = new KpsOptions();
            s.Bind(o);
            o.Environment = KpsEnvironment.Prod;
            return o;
        }
    }

    /// <summary>appsettings.json'daki "ProdCheck:IdentityNumber".</summary>
    public static long? ProdOrnekKimlikNo =>
        long.TryParse(Ayarlar["ProdCheck:IdentityNumber"], out var n) && n > 0 ? n : null;

    public static KpsClient Istemci(KpsOptions o) => new(new HttpClient { Timeout = o.Timeout }, o);

    private static string? DepoKokundekiAyarDosyasi()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent)
            if (File.Exists(Path.Combine(d.FullName, "KpsV2.sln")))
                return File.Exists(Path.Combine(d.FullName, "appsettings.json")) ? Path.Combine(d.FullName, "appsettings.json") : null;
        return null;
    }
}

public sealed class TestEnvironmentFactAttribute : FactAttribute
{
    public TestEnvironmentFactAttribute()
    {
        if (!GercekOrtam.TestAcik) Skip = "Test ortamı testleri için KPS_TEST_ENV=1 verin.";
    }
}

public sealed class ProdEnvironmentFactAttribute : FactAttribute
{
    public ProdEnvironmentFactAttribute(bool ornekKimlikNoGerekli = false)
    {
        if (GercekOrtam.Prod is null) Skip = "Prod bilgileri yok: depo kökünde appsettings.json oluşturup \"Kps\" bölümünü doldurun (şablon: appsettings.example.json).";
        else if (ornekKimlikNoGerekli && GercekOrtam.ProdOrnekKimlikNo is null) Skip = "appsettings.json içinde ProdCheck:IdentityNumber girilmemiş.";
    }
}

[Trait("Environment", "Test")]
public class TestOrtamiTestleri
{
    // Kimlik numaraları kpsv2test "Örnek Kodlar" sayfasındaki listelerden (docs/kaynaklar/test-kimlik-no).
    // Test ortamının arka servisi zaman zaman 6 sn'de zaman aşımına düşer: deneme sayısı yüksek tutulur.
    private readonly KpsClient _kps = GercekOrtam.Istemci(TestAyarlari());

    private static KpsOptions TestAyarlari()
    {
        var o = KpsOptions.PublicTestUser();
        o.RetryCount = 5;
        o.RetryDelay = TimeSpan.FromSeconds(2);
        return o;
    }

    [TestEnvironmentFact]
    public async Task Yetki_listesi_alinir()
    {
        var s = await _kps.YetkiListesiAsync();
        Assert.True(s.Data.IsAuthorized("BilesikKisiSorgula"));
        Assert.False(s.Data.IsAuthorized("KimlikNoIleAdresBilgisiSorgula"));
    }

    [TestEnvironmentFact]
    public Task TC_vatandasi_sorgulanir() => PersonTypeDogrula(10774881040, PersonType.TurkishCitizen);

    [TestEnvironmentFact]
    public Task Yabanci_sorgulanir() => PersonTypeDogrula(99169250708, PersonType.Foreigner);

    [TestEnvironmentFact]
    public Task Mavi_kartli_sorgulanir() => PersonTypeDogrula(14233775406, PersonType.BlueCard);

    [TestEnvironmentFact]
    public async Task Kisi_ve_yerlesim_yeri_birlikte_sorgulanir()
    {
        var s = await _kps.BilesikKisiveYerlesimYeriSorgulaAsync(14233775406);
        Assert.True(s.IsSuccess, s.Error?.ToString());
        Assert.Equal(PersonType.BlueCard, s.Data.ToSummary()!.Type);
        Assert.NotNull(s.Data!.AdresBilgisi);
    }

    [TestEnvironmentFact]
    public async Task Yerlesim_yeri_sorgulanir()
    {
        var s = await _kps.KimlikNoIleYerlesimYeriSorgulaAsync(14233775406);
        Assert.True(s.IsSuccess, s.Error?.ToString());
        Assert.False(string.IsNullOrEmpty(s.Data!.YerlesimYeriAdresi.AcikAdres));
    }

    // Test verisindeki bilinen kişi (BilesikKisiSorgula'ya bağımlı olmasın diye sabit).
    private const long Yesim = 26563360832;
    private static readonly DateOnly YesimDogum = new(1976, 3, 8);

    [TestEnvironmentFact]
    public async Task TC_kimlik_no_kisi_bilgileriyle_bulunur()
    {
        var s = await _kps.TcKimlikNoSorgulaAsync(
            KpsCriteria.Person("YEŞİM", "YELBAY", "İZZET", "GÜZİDE", ECinsiyet.KADIN, YesimDogum, "ADANA"));

        Assert.True(s.IsSuccess, s.Error?.ToString());
        Assert.Equal(Yesim, s.Data!.TCKimlikNo);
    }

    [TestEnvironmentFact]
    public async Task Vatandas_aile_bilgisi_sorgulanir()
    {
        var kriter = KpsCriteria.Kinship(Yesim, YesimDogum);

        var aile = await _kps.KimlikNoIleVatandasAileBilgisiSorgulaAsync(kriter);
        var kardesDahil = await _kps.KimlikNoIleVatandasAileBilgisiKardesDahilSorgulaAsync(kriter);

        Assert.True(aile.IsSuccess, aile.Error?.ToString());
        Assert.True(kardesDahil.IsSuccess, kardesDahil.Error?.ToString());
        Assert.True(kardesDahil.Data!.AileKisiListesi.Length > aile.Data!.AileKisiListesi.Length);
    }

    [TestEnvironmentFact] // doğum tarihi BilesikKisiSorgula'dan alınır
    public async Task Mavi_kartli_aile_bilgisi_sorgulanir()
    {
        var bilinen = (await _kps.BilesikKisiSorgulaAsync(14233775406)).Data.ToSummary()!;
        var kriter = KpsCriteria.Kinship(14233775406, bilinen.BirthDate!.Date!.Value);

        var aile = await _kps.KimlikNoIleMaviKartliAileBilgisiSorgulaAsync(kriter);
        var kardesDahil = await _kps.KimlikNoIleMaviKartliAileBilgisiKardesDahilSorgulaAsync(kriter);

        Assert.NotNull(aile.Data ?? (object?)aile.Error);
        Assert.NotNull(kardesDahil.Data ?? (object?)kardesDahil.Error);
    }

    [TestEnvironmentFact]
    public async Task Yetkisiz_metot_Yetkisiz_hatasi_verir()
    {
        var ex = await Assert.ThrowsAsync<KpsException>(() => _kps.KimlikNoIleAdresBilgisiSorgulaAsync(10774881040));
        Assert.Equal(KpsErrorKind.Unauthorized, ex.Kind);
    }

    private async Task PersonTypeDogrula(long kimlikNo, PersonType tur)
    {
        var s = await _kps.BilesikKisiSorgulaAsync(kimlikNo);
        Assert.True(s.IsSuccess, s.Error?.ToString());
        var k = s.Data.ToSummary()!;
        Assert.Equal(tur, k.Type);
        Assert.Equal(kimlikNo, k.IdentityNumber);
        Assert.False(string.IsNullOrEmpty(k.FirstName));
    }
}

[Trait("Environment", "Prod")]
public class ProdOrtamiTestleri
{
    [ProdEnvironmentFact]
    public async Task Yetki_listesi_alinir()
    {
        var s = await GercekOrtam.Istemci(GercekOrtam.Prod!).YetkiListesiAsync();
        Assert.True(s.IsSuccess, s.Error?.ToString());
        Assert.Contains(s.Data.MethodPermissions(), y => y.IsAuthorized);
    }

    [ProdEnvironmentFact(ornekKimlikNoGerekli: true)]
    public async Task Ornek_kisi_sorgulanir()
    {
        var no = GercekOrtam.ProdOrnekKimlikNo!.Value;

        var s = await GercekOrtam.Istemci(GercekOrtam.Prod!).BilesikKisiSorgulaAsync(no);

        // Kişisel veri test çıktısına yazılmaz; yalnızca sonucun dolu geldiği kontrol edilir.
        Assert.True(s.IsSuccess, s.Error?.ToString());
        Assert.Equal(no, s.Data.ToSummary()!.IdentityNumber);
    }
}

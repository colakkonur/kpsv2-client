// KPS kontrol aracı: Sağlık Bakanlığı KPS'sinin 18 metodunu ve NVİ KPS'nin 2 servisini birer kez çağırır, her biri için sonucu raporlar.
// appsettings.json'da hangi bölüm doluysa ("Kps", "NviKps") o sistem denenir.
// Kişisel veri YAZDIRMAZ: yalnızca durum, süre, hata türü/kodu ve dolu alan sayısı gösterilir.
//
//   dotnet run --project tools/KpsV2.ProdCheck                        # prod: depo kökündeki appsettings.json
//   dotnet run --project tools/KpsV2.ProdCheck -- yol/dosya.json
//   dotnet run --project tools/KpsV2.ProdCheck -- --test              # herkese açık test kullanıcısı + test kimlik no'ları
//
// Ayarlar depo kökündeki appsettings.json'dan okunur (şablon: appsettings.example.json); ortam değişkenleri (ör. Kps__Password) üstüne yazar.
// NVİ'nin herkese açık test ortamı yoktur; --test yalnızca Sağlık Bakanlığı KPS'sini dener.

using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;
using KpsV2;
using KpsV2.Contracts;
using KpsV2.Nvi;
using Microsoft.Extensions.Configuration;

var testModu = args.Contains("--test");
// Prod bilgilerinin TEK yeri: depo kökündeki (KpsV2.sln'in yanındaki) appsettings.json. Testler de aynı dosyayı okur.
var jsonYolu = args.FirstOrDefault(a => !a.StartsWith("--")) ?? Path.Combine(DepoKoku() ?? Directory.GetCurrentDirectory(), "appsettings.json");

KpsOptions? ayar;
NviKpsOptions? nviAyar = null;
ProdCheckOptions kontrol;
if (testModu)
{
    ayar = KpsOptions.PublicTestUser();
    ayar.RetryCount = 3;
    // Test verisindeki bilinen kişi; doğum tarihi verildiği için BilesikKisiSorgula zaman aşımına düşse de aile metotları denenir.
    kontrol = new ProdCheckOptions { IdentityNumber = 26563360832, BirthDate = new DateOnly(1976, 3, 8), BlueCardIdentityNumber = 14233775406 };
}
else
{
    if (!File.Exists(jsonYolu))
    {
        Console.Error.WriteLine($"Ayar dosyası bulunamadı: {Path.GetFullPath(jsonYolu)}");
        Console.Error.WriteLine("Depo kökünde appsettings.example.json dosyasını appsettings.json adıyla kopyalayıp doldurun.");
        return 1;
    }
    var config = new ConfigurationBuilder().AddJsonFile(Path.GetFullPath(jsonYolu)).AddEnvironmentVariables().Build();
    ayar = null;
    if (Dolu(config.GetSection(KpsOptions.SectionName)))
    {
        ayar = new KpsOptions { Environment = KpsEnvironment.Prod };
        config.GetSection(KpsOptions.SectionName).Bind(ayar);
    }
    if (Dolu(config.GetSection(NviKpsOptions.SectionName)))
    {
        nviAyar = new NviKpsOptions();
        config.GetSection(NviKpsOptions.SectionName).Bind(nviAyar);
    }
    if (ayar is null && nviAyar is null)
    {
        Console.Error.WriteLine($"{Path.GetFullPath(jsonYolu)}: \"Kps\" (Sağlık Bakanlığı KPS) veya \"NviKps\" (NVİ KPS) bölümünü doldurun.");
        return 1;
    }
    kontrol = config.GetSection("ProdCheck").Get<ProdCheckOptions>() ?? new ProdCheckOptions();
    if (kontrol.IdentityNumber <= 0)
    {
        Console.Error.WriteLine("ProdCheck:IdentityNumber gerekli (sorgulanmasına izniniz olan bir kimlik numarası).");
        return 1;
    }
}

KpsClient? saglik = null;
NviKpsClient? nvi = null;
try
{
    if (ayar is not null) saglik = new KpsClient(new HttpClient { Timeout = ayar.Timeout }, ayar);
    if (nviAyar is not null) nvi = new NviKpsClient(new HttpClient { Timeout = nviAyar.Timeout }, nviAyar);
}
catch (KpsConfigurationException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

if (ayar is not null)
    Console.WriteLine($"Sağlık Bakanlığı KPS — ortam: {ayar.Environment}, kullanıcı tipi: {(ayar.IsApplicationUser ? "uygulama" : "standart")}, " +
                      $"yeniden deneme: {ayar.RetryCount}");
if (nviAyar is not null)
    Console.WriteLine($"NVİ KPS — yeniden deneme: {nviAyar.RetryCount}");
Console.WriteLine("Kişisel veri yazdırılmaz; 'alan' sütunu cevaptaki dolu alan sayısıdır.\n");

var satirlar = new List<Satir>();
const string NviServis = "NVİ";
WcfService[]? yetkiler = null;
PersonSummary? kisi = null;
bool? Yetkili(string metot, string servis = KpsPermissionExtensions.KpsServiceName) =>
    yetkiler is null || servis == NviServis ? null
    : yetkiler.MethodPermissions().Any(y => y.Service == servis && y.Method == metot) ? yetkiler.IsAuthorized(metot, servis)
    : null; // listede hiç yok

if (saglik is not null) await SaglikKontroluAsync(saglik);
if (nvi is not null) await NviKontroluAsync(nvi);

// ---- Rapor ----
Console.WriteLine();
Console.WriteLine($"{"Metot",-58} {"Yetki listesi",-14} {"Sonuç",-34} {"Süre",8} {"Alan",6}");
Console.WriteLine(new string('-', 124));
foreach (var s in satirlar)
{
    var yetki = s.Nvi ? "—" : s.YetkiListesinde switch { true => "yetkili", false => "yetkisiz", null => "listede yok" };
    Console.WriteLine($"{s.Metot,-58} {yetki,-14} {s.Sonuc,-34} {(s.Sure is null ? "" : $"{s.Sure} ms"),8} {s.Alan?.ToString() ?? "",6}");
    if (s.Not is not null) Console.WriteLine($"{"",58}   └ {s.Not}");
}
Console.WriteLine();
Console.WriteLine($"Toplam {satirlar.Count}: ✔ {satirlar.Count(s => s.Tur == "ok")} başarılı, ○ {satirlar.Count(s => s.Tur == "bos")} boş/iş hatası, " +
                  $"⛔ {satirlar.Count(s => s.Tur == "yetkisiz")} yetkisiz, ✘ {satirlar.Count(s => s.Tur == "hata")} hata, " +
                  $"– {satirlar.Count(s => s.Tur == "atlandi")} atlandı");
return satirlar.Any(s => s.Tur == "hata") ? 2 : 0;

// =====================================================================

async Task SaglikKontroluAsync(KpsClient kps)
{
    Console.WriteLine("— Sağlık Bakanlığı KPS");

    // ---- 1) Yetki listesi: hem kendisi test edilir hem de diğer metotların beklenen yetkisini verir ----
    await Calistir("YetkiListesi", async () =>
    {
        var s = await kps.YetkiListesiAsync();
        yetkiler = s.Data;
        return s;
    });
    // ---- 2) Kişi: sonraki kriterler için gerekli bilgiler buradan alınır (yalnızca bellekte tutulur) ----
    await Calistir("BilesikKisiSorgula", async () =>
    {
        var s = await kps.BilesikKisiSorgulaAsync(kontrol.IdentityNumber);
        kisi = s.Data.ToSummary();
        return s;
    });
    await Calistir("BilesikKisiveAdresSorgula", () => kps.BilesikKisiveAdresSorgulaAsync(kontrol.IdentityNumber));
    await Calistir("BilesikKisiveYerlesimYeriSorgula", () => kps.BilesikKisiveYerlesimYeriSorgulaAsync(kontrol.IdentityNumber));

    var dogum = kontrol.BirthDate ?? kisi?.BirthDate?.Date;

    if (kisi is { FirstName: not null, LastName: not null, FatherName: not null, MotherName: not null, BirthPlace: not null } && dogum is not null)
        await Calistir("TcKimlikNoSorgula", () => kps.TcKimlikNoSorgulaAsync(KpsCriteria.Person(kisi.FirstName, kisi.LastName, kisi.FatherName, kisi.MotherName,
            kisi.Gender?.Kod == 2 ? ECinsiyet.KADIN : kisi.Gender?.Kod == 1 ? ECinsiyet.ERKEK : ECinsiyet.BILINMEYEN, dogum.Value, kisi.BirthPlace)));
    else
        Atla("TcKimlikNoSorgula", "kişinin ad/soyad/anne/baba/doğum bilgisi BilesikKisiSorgula'dan alınamadı");

    // ---- 3) Adres ----
    await Calistir("KimlikNoIleAdresBilgisiSorgula", () => kps.KimlikNoIleAdresBilgisiSorgulaAsync(kontrol.IdentityNumber));

    long? adresNo = kontrol.AddressNumber;
    await Calistir("KimlikNoIleYerlesimYeriSorgula", async () =>
    {
        var s = await kps.KimlikNoIleYerlesimYeriSorgulaAsync(kontrol.IdentityNumber);
        adresNo ??= s.Data?.YerlesimYeriAdresi?.AdresNo is > 0 and var n ? n : null;
        return s;
    });
    if (adresNo is > 0)
        await Calistir("AdresNoIleYerlesimYeriSorgula", () => kps.AdresNoIleYerlesimYeriSorgulaAsync(adresNo.Value));
    else
        Atla("AdresNoIleYerlesimYeriSorgula", "adres no yok (ProdCheck:AddressNumber verin veya yerleşim yeri sorgusu adres no döndürmeli)");

    // ---- 4) Nüfus kaydı ve aile ----
    await Calistir("KimlikNoIleNufusKayitOrnegiSorgula", () => kps.KimlikNoIleNufusKayitOrnegiSorgulaAsync(kontrol.IdentityNumber));
    await Calistir("AileBireyleriSorgula", () => kps.AileBireyleriSorgulaAsync(KpsCriteria.FamilyMembers(kontrol.IdentityNumber)));

    if (dogum is not null)
    {
        var yk = KpsCriteria.Kinship(kontrol.IdentityNumber, dogum.Value);
        var kyk = KpsCriteria.PublicKinship(kontrol.IdentityNumber, dogum.Value);
        await Calistir("KimlikNoIleVatandasAileBilgisiSorgula", () => kps.KimlikNoIleVatandasAileBilgisiSorgulaAsync(yk));
        await Calistir("KimlikNoIleVatandasAileBilgisiKardesDahilSorgula", () => kps.KimlikNoIleVatandasAileBilgisiKardesDahilSorgulaAsync(yk));
        await Calistir("KimlikNoIleVatandasAileYakinlikBilgisiSorgula", () => kps.KimlikNoIleVatandasAileYakinlikBilgisiSorgulaAsync(kyk));
        await Calistir("KimlikNoIleVatandasAileYakinlikBilgisiKardesDahilSorgula", () => kps.KimlikNoIleVatandasAileYakinlikBilgisiKardesDahilSorgulaAsync(kyk));
    }
    else
    {
        foreach (var m in new[] { "KimlikNoIleVatandasAileBilgisiSorgula", "KimlikNoIleVatandasAileBilgisiKardesDahilSorgula",
                                  "KimlikNoIleVatandasAileYakinlikBilgisiSorgula", "KimlikNoIleVatandasAileYakinlikBilgisiKardesDahilSorgula" })
            Atla(m, "doğum tarihi yok (ProdCheck:BirthDate verin)");
    }

    // Mavi kartlı aile metotları: mavi kartlı bir kimlik no verilmişse onunla, yoksa ana kişiyle çağrılır.
    var mkNo = kontrol.BlueCardIdentityNumber ?? kontrol.IdentityNumber;
    var mkDogum = kontrol.BlueCardIdentityNumber is null ? dogum : kontrol.BlueCardBirthDate;
    if (kontrol.BlueCardIdentityNumber is not null && mkDogum is null)
    {
        try { mkDogum = (await kps.BilesikKisiSorgulaAsync(mkNo)).Data.ToSummary()?.BirthDate?.Date; }
        catch (KpsException) { /* aşağıda atlanır */ }
    }
    if (mkDogum is not null)
    {
        var mk = KpsCriteria.Kinship(mkNo, mkDogum.Value);
        await Calistir("KimlikNoIleMaviKartliAileBilgisiSorgula", () => kps.KimlikNoIleMaviKartliAileBilgisiSorgulaAsync(mk));
        await Calistir("KimlikNoIleMaviKartliAileBilgisiKardesDahilSorgula", () => kps.KimlikNoIleMaviKartliAileBilgisiKardesDahilSorgulaAsync(mk));
    }
    else
    {
        Atla("KimlikNoIleMaviKartliAileBilgisiSorgula", "mavi kartlı kişinin doğum tarihi yok");
        Atla("KimlikNoIleMaviKartliAileBilgisiKardesDahilSorgula", "mavi kartlı kişinin doğum tarihi yok");
    }

    // ---- 5) Parametre servisi ----
    string? ilkParametre = kontrol.ParameterName;
    await Calistir("ParametreAdListesi", async () =>
    {
        var s = await kps.ParametreAdListesiAsync();
        ilkParametre ??= s.Data?.FirstOrDefault(p => !string.IsNullOrEmpty(p));
        return s;
    }, KpsPermissionExtensions.ParameterServiceName);
    if (ilkParametre is not null)
        await Calistir("ParametreDegerleri", () => kps.ParametreDegerleriAsync(ilkParametre), KpsPermissionExtensions.ParameterServiceName);
    else
        Atla("ParametreDegerleri", "parametre adı yok (ParametreAdListesi başarısız; ProdCheck:ParameterName verin)", KpsPermissionExtensions.ParameterServiceName);
}

async Task NviKontroluAsync(NviKpsClient nvi)
{
    Console.WriteLine("— NVİ KPS");
    // Doğrulama için ad/soyad/doğum yılı: NVİ kişi sorgusundan, yoksa Sağlık KPS'sinden, yoksa ProdCheck ayarlarından.
    string? ad = null, soyad = null;
    int? dogumYili = null;
    await Calistir("NVİ KisiSorgulaTCKimlikNo", async () =>
    {
        var s = await nvi.KisiSorgulaTCKimlikNoAsync(kontrol.IdentityNumber);
        (ad, soyad, dogumYili) = (s.Data?.TemelBilgisi?.Ad, s.Data?.TemelBilgisi?.Soyad, s.Data?.TemelBilgisi?.DogumTarih?.Yil);
        return s;
    }, NviServis);
    ad ??= kisi?.FirstName ?? kontrol.FirstName;
    soyad ??= kisi?.LastName ?? kontrol.LastName;
    dogumYili ??= kisi?.BirthDate?.Yil ?? kontrol.BirthDate?.Year;
    if (ad is not null && soyad is not null && dogumYili is not null)
        await Calistir("NVİ TCKimlikNoDogrula", () => nvi.TCKimlikNoDogrulaAsync(kontrol.IdentityNumber, ad, soyad, dogumYili.Value), NviServis);
    else
        Atla("NVİ TCKimlikNoDogrula", "ad/soyad/doğum yılı yok (ProdCheck:FirstName, LastName, BirthDate verin)", NviServis);
}

// =====================================================================

async Task Calistir<T>(string metot, Func<Task<KpsResult<T>>> cagri, string servis = KpsPermissionExtensions.KpsServiceName) where T : class
{
    var sure = Stopwatch.StartNew();
    Console.Write($"  {metot} ... ");
    try
    {
        var s = await cagri();
        var alan = Alan.Say(s.Data);
        var (tur, sonuc) = s.IsSuccess ? ("ok", "✔ başarılı")
            : s.Error is not null ? ("bos", $"○ HataBilgisi [{s.Error.Code}] {Kisalt(s.Error.Description, 18)}")
            : ("bos", "○ boş sonuç");
        var not = s.InnerErrors.Count > 0 ? $"iç hata: {string.Join("; ", s.InnerErrors.Take(3).Select(h => $"{h.Path} [{h.Error.Code}]"))}" : null;
        satirlar.Add(new Satir(metot, Yetkili(metot, servis), tur, sonuc, sure.ElapsedMilliseconds, alan, not, servis == NviServis));
        Console.WriteLine(sonuc);
    }
    catch (KpsException ex)
    {
        var tur = ex.Kind == KpsErrorKind.Unauthorized ? "yetkisiz" : "hata";
        var sonuc = (ex.Kind == KpsErrorKind.Unauthorized ? "⛔ " : "✘ ") + ex.Kind;
        satirlar.Add(new Satir(metot, Yetkili(metot, servis), tur, sonuc, sure.ElapsedMilliseconds, null, Kisalt(ex.SoapFault ?? ex.Message, 110), servis == NviServis));
        Console.WriteLine(sonuc);
    }
    catch (ArgumentException ex)
    {
        satirlar.Add(new Satir(metot, Yetkili(metot, servis), "hata", "✘ geçersiz argüman", null, null, ex.Message, servis == NviServis));
        Console.WriteLine("✘ geçersiz argüman");
    }
}

void Atla(string metot, string neden, string servis = KpsPermissionExtensions.KpsServiceName)
{
    satirlar.Add(new Satir(metot, Yetkili(metot, servis), "atlandi", "– atlandı", null, null, neden, servis == NviServis));
    Console.WriteLine($"  {metot} ... – atlandı ({neden})");
}

static string? DepoKoku()
{
    for (var d = new DirectoryInfo(Directory.GetCurrentDirectory()); d is not null; d = d.Parent)
        if (File.Exists(Path.Combine(d.FullName, "KpsV2.sln"))) return d.FullName;
    return null;
}

static string Kisalt(string? s, int n) => s is null ? "" : s.Length <= n ? s : s[..n] + "…";

static bool Dolu(IConfigurationSection bolum) => bolum["Username"] is { Length: > 0 } u && !u.StartsWith('<');

internal sealed record Satir(string Metot, bool? YetkiListesinde, string Tur, string Sonuc, long? Sure, int? Alan, string? Not, bool Nvi);

/// <summary>appsettings.json → "ProdCheck" bölümü: kontrolde sorgulanacak kişi.</summary>
internal sealed class ProdCheckOptions
{
    /// <summary>Zorunlu: sorgulanmasına izniniz olan bir kimlik numarası (ör. kendi TC'niz).</summary>
    public long IdentityNumber { get; set; }
    /// <summary>İsteğe bağlı: boşsa BilesikKisiSorgula'dan alınır.</summary>
    public DateOnly? BirthDate { get; set; }
    /// <summary>İsteğe bağlı: boşsa KimlikNoIleYerlesimYeriSorgula'dan alınır.</summary>
    public long? AddressNumber { get; set; }
    /// <summary>İsteğe bağlı: mavi kartlı aile metotları için mavi kartlı bir kişi.</summary>
    public long? BlueCardIdentityNumber { get; set; }
    /// <summary>İsteğe bağlı: boşsa BilesikKisiSorgula'dan alınır.</summary>
    public DateOnly? BlueCardBirthDate { get; set; }
    /// <summary>İsteğe bağlı: ParametreDegerleri için parametre adı; boşsa ParametreAdListesi'nin ilki.</summary>
    public string? ParameterName { get; set; }
    /// <summary>İsteğe bağlı: NVİ TCKimlikNoDogrula için ad; boşsa kişi sorgularından alınır.</summary>
    public string? FirstName { get; set; }
    /// <summary>İsteğe bağlı: NVİ TCKimlikNoDogrula için soyad; boşsa kişi sorgularından alınır.</summary>
    public string? LastName { get; set; }
}

/// <summary>Cevaptaki dolu yaprak alan sayısı (değerleri değil yalnızca sayıyı verir).</summary>
internal static class Alan
{
    public static int Say(object? o, int derinlik = 0)
    {
        if (o is null || derinlik > 12) return 0;
        var t = o.GetType();
        if (t.IsPrimitive || o is string or DateTime or Enum or decimal) return o is string s && s.Length == 0 ? 0 : 1;
        if (o is IEnumerable e) return e.Cast<object?>().Sum(x => Say(x, derinlik + 1));
        return t.GetProperties()
            .Where(p => p.GetCustomAttribute<DataMemberAttribute>() is not null && p.GetIndexParameters().Length == 0)
            .Sum(p => Say(p.GetValue(o), derinlik + 1));
    }
}

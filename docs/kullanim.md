# KpsV2.Client — Kullanım

Sağlık Bakanlığı **Kimlik Paylaşım Sistemi (KPS v2)** için .NET istemcisinin API başvurusu: ayarlar, metotlar, sonuç tipi ve hatalar. Bir projeyi ilk kez bağlıyorsanız adım adım anlatım için `entegrasyon-kilavuzu.md` dosyasına bakın. Her metodun dönen alanlarının tam listesi `metot-referansi.md` dosyasındadır.

Bu dosya aynı zamanda NuGet paketinin README'sidir.

> **Resmî paket değildir.** Bu paket Sağlık Bakanlığı tarafından yayınlanmamıştır ve Bakanlıkla bağlantısı yoktur. KPS'yi kullanmak için kurumunuzun KPS Yönetim Sistemi'nden aldığı kendi kullanıcı adı, şifre, kurum kodu ve uygulama kodunu girmeniz gerekir.

## Özellikler

- **Kps servisinin 16 ve Parametre servisinin 2 metodu**, WSDL'den üretilmiş tiplerle (`KpsV2.Contracts`). İstekler tipli kriterlerle gönderilir; cevaplar hiçbir alan kaybolmadan tipli nesnelere dönüşür.
- STS token'ı süresi dolana kadar önbellekte tutulur. Eşzamanlı isteklerde token tek sefer alınır. KPS token'ı reddederse yenisi alınır.
- Geçici hatalarda (bağlantı, zaman aşımı, KPS 5xx) otomatik yeniden deneme yapılır. Her hata `KpsErrorKind` ile sınıflandırılır.
- Üç kimlik türünün (TC / yabancı / mavi kart) ortak alanları `PersonSummary` ile tek tipte toplanır.
- Yetki listesi metot ve alan bazında okunur.
- ASP.NET Core DI ve `appsettings.json` ile çalışır. Hedef `net7.0`: .NET 7, 8, 9 ve 10 projelerine eklenebilir.

**Adlandırma:** Paketin kendi API'si İngilizcedir (`KpsResult`, `PersonSummary`, `KpsCriteria`...). KPS'nin metot ve veri tipi adları ise Bakanlığın WSDL'indeki gibi Türkçe kalır (`BilesikKisiSorgulaAsync`, `TemelBilgisi`, `Ad`, `Soyad`...).

## Ortamlar

| `Environment` | Adres | Not |
|---|---|---|
| `Test` (varsayılan) | kpsv2test.saglik.gov.tr | IP kısıtı yok; yalnızca örnek kimlik numaraları sorgulanabilir |
| `Prod` | kpsv2.saglik.gov.tr | Gerçek veri; yalnızca KPS'ye tanımlı çıkış IP'lerinden erişilebilir |

## Ayarlar

`appsettings.json` içindeki `Kps` bölümü:

```json
{
  "Kps": {
    "Environment": "Prod",
    "Username": "<sorgulayan kişinin TC kimlik no>",
    "Password": "<KPS web servis şifresi>",
    "InstitutionCode": "<kurum kodu>",
    "ApplicationCode": "<uygulama GUID>"
  }
}
```

| Alan | Zorunlu | Açıklama |
|---|---|---|
| `Environment` | Hayır | `Test` (varsayılan) veya `Prod` |
| `Username` | Evet | Sorgulamayı yapacak kişinin T.C. kimlik numarası |
| `Password` | Evet | kpsyonetimv2.saglik.gov.tr'de belirlenen web servis şifresi |
| `InstitutionCode` | Standart kullanıcıda | Sağlık kuruluşunun kodu (KPS: KurumKodu) |
| `ApplicationCode` | Standart kullanıcıda | Kimlik Paylaşım Birimi'nin verdiği GUID (KPS: UygulamaKodu) |
| `RequesterIdentityNumber` | Uygulama kullanıcısında | Merkez teşkilat uygulamaları: InstitutionCode ve ApplicationCode yerine (KPS: SorgulayanKimlikNo) |
| `Timeout` | Hayır | İstek zaman aşımı. Varsayılan `00:01:00` |
| `RetryCount` | Hayır | Geçici hatalarda ek deneme sayısı. Varsayılan `2`, `0` kapatır |
| `RetryDelay` | Hayır | İlk bekleme süresi; her denemede katlanır. Varsayılan `00:00:01` |
| `ValidateIdentityNumber` | Hayır | Sorgudan önce kontrol hanelerini doğrular. Varsayılan `true` |
| `BaseAddress` | Hayır | Ortam adresini ezer (ör. ara proxy). Boşsa `Environment`'a göre seçilir |

> **Şifre:** `Password` alanını kaynak koduyla birlikte depoya göndermeyin. Sunucuda `Kps__Password` ortam değişkenini veya Azure Key Vault'u kullanabilirsiniz; .NET bunları `appsettings.json`'ın üzerine yazar.

## Kayıt

```csharp
// Program.cs
builder.Services.AddKpsClient(builder.Configuration.GetSection(KpsOptions.SectionName));   // "Kps"
```

`IKpsClient` singleton olarak kaydedilir. `AddKpsClient` bir `IHttpClientBuilder` döndürür. Proxy gibi ayarları buna ekleyebilirsiniz:

```csharp
builder.Services.AddKpsClient(builder.Configuration.GetSection("Kps"))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { Proxy = new WebProxy("http://proxy:8080") });
```

DI kullanmadan:

```csharp
using var http = new HttpClient();
var kps = new KpsClient(http, new KpsOptions { Environment = KpsEnvironment.Test, Username = "...", Password = "...", InstitutionCode = "...", ApplicationCode = "..." });
```

## Sorgu

```csharp
using KpsV2;
using KpsV2.Contracts;

public class PatientIdentityService(IKpsClient kps)
{
    public async Task<PersonSummary?> GetAsync(long identityNumber, CancellationToken ct)
    {
        var result = await kps.BilesikKisiSorgulaAsync(identityNumber, ct);
        if (!result.IsSuccess)
            return null;                   // result.Error: ör. [2] "Aradığınız kriterlere uygun kayıt bulunamadı."

        return result.Data.ToSummary();    // TC, yabancı veya mavi kart; kişi yoksa null
        // summary.FullName, summary.BirthDate?.Date, summary.Gender?.Aciklama, summary.IsDeceased, summary.Type ...
    }
}
```

Özetin dışındaki her alana tipli olarak erişilir:

```csharp
var tc = result.Data!.TCVatandasiKisiKutukleri;
var district = tc.KisiBilgisi.KayitYeriBilgisi.Ilce.Aciklama;   // nüfusa kayıtlı ilçe
var idCardSerial = tc.TCKKBilgisi?.SeriNo;                      // kimlik kartı
```

Kriterli sorgular:

```csharp
// Ad/soyad/doğum bilgisiyle T.C. kimlik no bulma
var s1 = await kps.TcKimlikNoSorgulaAsync(
    KpsCriteria.Person("AHMET", "YILMAZ", "MEHMET", "AYŞE", ECinsiyet.ERKEK, new DateOnly(1980, 3, 15), "ANKARA"));
long? identityNumber = s1.Data?.TCKimlikNo;

// Aile bilgisi: kişinin doğum tarihi doğrulama için istenir
var s2 = await kps.KimlikNoIleVatandasAileBilgisiKardesDahilSorgulaAsync(KpsCriteria.Kinship(identityNumber!.Value, new DateOnly(1980, 3, 15)));
foreach (var member in s2.Data?.AileKisiListesi ?? [])
    Console.WriteLine(member.Yakinlik?.Aciklama);   // Kendisi, Babası, Kardeşi...
```

Hata durumunu ayrıca kontrol etmek istemiyorsanız `GetDataOrThrow()` kullanın. Hata varsa `KpsException` fırlatır; türü `NotFound` veya `ServiceError` olur:

```csharp
var address = (await kps.KimlikNoIleYerlesimYeriSorgulaAsync(no)).GetDataOrThrow().YerlesimYeriAdresi.AcikAdres;
```

## Metotlar

| Metot | `Data` tipi |
|---|---|
| `BilesikKisiSorgulaAsync(identityNumber)` | `BilesikKisiBilgisi` → `.ToSummary()` |
| `BilesikKisiveAdresSorgulaAsync(identityNumber)` | `BilesikKisiveAdresBilgisi` (+ `AdresBilgisi`) |
| `BilesikKisiveYerlesimYeriSorgulaAsync(identityNumber)` | `BilesikKisiveYerlesimYeriBilgisi` (+ `AdresBilgisi`) |
| `TcKimlikNoSorgulaAsync(KpsCriteria.Person(...))` | `TCKisiBilgisi` |
| `KimlikNoIleAdresBilgisiSorgulaAsync(identityNumber)` | `KisiAdresBilgisi` |
| `KimlikNoIleYerlesimYeriSorgulaAsync(identityNumber)` | `TcKimlikNoIleAdresBilgisi` (yerleşim yeri + adres geçmişi) |
| `AdresNoIleYerlesimYeriSorgulaAsync(addressNumber)` | `AcikAdres` |
| `KimlikNoIleNufusKayitOrnegiSorgulaAsync(identityNumber)` | `GenelNufusKayitOrnegi` |
| `AileBireyleriSorgulaAsync(KpsCriteria.FamilyMembers(no, wholeFamily))` | `AileBilgisi` |
| `KimlikNoIleVatandasAileBilgisi[KardesDahil]SorgulaAsync(KpsCriteria.Kinship(...))` | `YakinlikServisiAileBilgisi` |
| `KimlikNoIleMaviKartliAileBilgisi[KardesDahil]SorgulaAsync(KpsCriteria.Kinship(...))` | `MaviKartliAileBilgisi` |
| `KimlikNoIleVatandasAileYakinlikBilgisi[KardesDahil]SorgulaAsync(KpsCriteria.PublicKinship(...))` | `KamuYakinlikServisiAileBilgisi` |
| `YetkiListesiAsync()` | `WcfService[]` → `.MethodPermissions()`, `.IsAuthorized(method)`, `.FieldPermissions(method)` |
| `ParametreAdListesiAsync()` | `string[]` |
| `ParametreDegerleriAsync(parameterName)` | `Parametre[]` |
| `CallAsync(service, method, bodyXml)` | Ham `XDocument`: bu pakette tipli karşılığı olmayan servisler için (Uavt, Gizlilik, Olay...) |

## Sonuç: `KpsResult<T>`

| Üye | İçerik |
|---|---|
| `Data` | WSDL tipi; tüm alanlar |
| `Error` | Üst seviyedeki veya Sonuc düğümündeki HataBilgisi (`Code`, `Description`, `IsNotFound`) |
| `InnerErrors` | İç düğümlerdeki HataBilgisi'ler, yollarıyla. Ör. `Sonuc/TumAdresBilgileri/KisiAdresBilgisi: [2] KayitBulunamadi` |
| `IsSuccess` | Hata yok ve veri dolu |
| `GetDataOrThrow()` | Başarılıysa `Data`, değilse `KpsException` |
| `Xml` | Ham `<{Metot}Result>` düğümü. Kişisel veri içerir, loglamayın |

Yardımcılar: `Parametre.ToString()` açıklamayı döner. `TarihBilgisi.Date` bir `DateOnly?` döner; gün, ay veya yıl eksikse ya da tarih geçersizse null olur. `TarihBilgisi.IsEmpty` hiçbir parçanın dolu olmadığını söyler.

## Hatalar

Fırlatılan tüm KPS hataları `KpsException` tipindedir. `Kind` alanına göre karar verin:

| `Kind` | Ne zaman | `IsTransient` | Önerilen davranış |
|---|---|---|---|
| `Connection` | Ağ/TLS/DNS; Prod'da çoğunlukla IP tanımlı değil | ✔ | Yeniden denenir; sürerse IP tanımını kontrol edin |
| `Timeout` | HttpClient zaman aşımı veya KPS arka servisi 6 sn'de cevap vermedi | ✔ | Yeniden denenir; kullanıcıya "tekrar deneyin" |
| `Server` | KPS 5xx (ayrıntısız) | ✔ | Yeniden denenir |
| `Authentication` | STS: kullanıcı adı/şifre, kurum veya uygulama kodu hatalı | ✘ | Ayarları düzeltin |
| `Unauthorized` | Kurumun bu metoda yetkisi yok | ✘ | KPS Yönetim'den yetki isteyin |
| `InvalidResponse` | Cevap WSDL'e uymuyor (KPS sözleşmeyi değiştirmiş olabilir) | ✘ | Paketin güncel sürümüne geçin |
| `Configuration` | `KpsOptions` eksik (`KpsConfigurationException`) | ✘ | Hata mesajındaki alanları doldurun |
| `NotFound` / `ServiceError` | Yalnızca `GetDataOrThrow()` | ✘ | İş kuralı |

Ek olarak `HttpStatusCode` ve `SoapFault` (KPS'nin hata metni) alanları vardır.

- Kimlik numarası geçersizse `ArgumentException` fırlatılır. Bu durumda KPS'ye istek gönderilmez.
- İş hataları fırlatılmaz; `KpsResult.Error` alanında döner. Bunun tek istisnası `GetDataOrThrow()`.
- Yetkiniz olmayan alanlar KPS cevabında null gelir. Hangi alanları görebildiğinizi `(await kps.YetkiListesiAsync()).Data.FieldPermissions("BilesikKisiSorgula")` ile listeleyebilirsiniz.

> **Kişisel veri (KVKK):** Kimlik numaralarını ve kişi bilgilerini loglara yazmayın. Kimlik numarasını URL'de değil istek gövdesinde taşıyın; aksi hâlde erişim loglarına düşer. Kütüphane yalnızca metot adını, süreyi ve hata türünü loglar.

## Test ortamı

Herkese açık test kullanıcısı `KpsOptions.PublicTestUser()` ile hazır gelir. Bu kullanıcı kpsv2test.saglik.gov.tr "Örnek Kodlar" sayfasında yayınlanmıştır. Test ortamında yalnızca aynı sayfadaki örnek kimlik numaraları sorgulanabilir.

Test kullanıcısı yalnızca 9 KPS metoduna yetkilidir. Adres, nüfus kayıt örneği, aile bireyleri, yakınlık ve Parametre metotları bu kullanıcıyla `Unauthorized` döner. Test ortamının arka servisi zaman zaman 6 saniyede zaman aşımına düşer; bu durumda yeniden deneme devreye girer.

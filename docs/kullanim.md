# KpsV2.Client — Kullanım

Sağlık Bakanlığı **Kimlik Paylaşım Sistemi (KPS v2)** için .NET istemcisi. Kps servisinin 16 ve Parametre servisinin 2 metodunu tipli istek/cevaplarla sunar; token, yeniden deneme ve hata yönetimi paketin içindedir.

Aynı paket, ayrı bir sistem olan **NVİ Kimlik Paylaşımı Sistemi**'ni de destekler (TC kimlik no doğrulama ve kişi sorgulama): [8. NVİ KPS](#8-nvi-kps).

> **Resmî paket değildir.** Sağlık Bakanlığı veya NVİ ile bağlantısı yoktur. Kullanmak için kurumunuzun KPS hesap bilgileri gerekir.

## 1. Kurulum

```bash
dotnet add package KpsV2.Client
```

.NET 7 ve üzeri her projede çalışır.

## 2. Ayarlar

Projenizin `appsettings.json` dosyasına `Kps` bölümünü ekleyin:

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

| Alan | Açıklama |
|---|---|
| `Environment` | `Test` (kpsv2test.saglik.gov.tr) veya `Prod` (kpsv2.saglik.gov.tr). Varsayılan `Test` |
| `Username` | Sorgulamayı yapan kişinin TC kimlik numarası |
| `Password` | kpsyonetimv2.saglik.gov.tr'de belirlenen web servis şifresi |
| `InstitutionCode` | Kurum kodu |
| `ApplicationCode` | Kimlik Paylaşım Birimi'nin verdiği uygulama GUID'i |

İsteğe bağlı alanlar: `RequesterIdentityNumber` (merkez teşkilat uygulamaları; kurum ve uygulama kodu yerine), `Timeout` (varsayılan `00:01:00`), `RetryCount` (varsayılan `2`), `RetryDelay` (varsayılan `00:00:01`), `ValidateIdentityNumber` (varsayılan `true`).

- Şifre içeren dosyayı kaynak koduyla birlikte depoya göndermeyin. Sunucuda şifreyi `Kps__Password` ortam değişkeniyle de verebilirsiniz.
- **Prod IP kısıtlıdır:** Sunucunuzun çıkış IP'si KPS'ye tanımlı olmalıdır.

## 3. Kayıt

```csharp
// Program.cs
builder.Services.AddKpsClient(builder.Configuration.GetSection("Kps"));
```

`IKpsClient` singleton olarak kaydedilir. Token önbelleği tüm istekler arasında paylaşılır.

## 4. Sorgu

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

        return result.Data.ToSummary();    // FullName, BirthDate, Gender, IsDeceased, Type...
    }
}
```

`ToSummary()`, TC / yabancı / mavi kartlı kişinin ortak alanlarını tek tipte verir. Diğer tüm alanlara tipli olarak erişilir:

```csharp
var tc = result.Data!.TCVatandasiKisiKutukleri;
var district = tc.KisiBilgisi.KayitYeriBilgisi.Ilce.Aciklama;   // nüfusa kayıtlı ilçe
var idCardSerial = tc.TCKKBilgisi?.SeriNo;                      // kimlik kartı
```

Kriterli sorgular:

```csharp
// Ad/soyad/doğum bilgisiyle TC kimlik no bulma
var s1 = await kps.TcKimlikNoSorgulaAsync(
    KpsCriteria.Person("AHMET", "YILMAZ", "MEHMET", "AYŞE", ECinsiyet.ERKEK, new DateOnly(1980, 3, 15), "ANKARA"));
long? identityNumber = s1.Data?.TCKimlikNo;

// Aile bilgisi: kişinin doğum tarihi doğrulama için istenir
var s2 = await kps.KimlikNoIleVatandasAileBilgisiKardesDahilSorgulaAsync(KpsCriteria.Kinship(identityNumber!.Value, new DateOnly(1980, 3, 15)));
foreach (var member in s2.Data?.AileKisiListesi ?? [])
    Console.WriteLine(member.Yakinlik?.Aciklama);   // Kendisi, Babası, Kardeşi...
```

Hata kontrolünü kısaltmak için `GetDataOrThrow()`:

```csharp
var address = (await kps.KimlikNoIleYerlesimYeriSorgulaAsync(no)).GetDataOrThrow().YerlesimYeriAdresi.AcikAdres;
```

Metot ve tip adları KPS'nin kendi adlarıdır (`BilesikKisiSorgula`, `TemelBilgisi`...); paketin yardımcıları İngilizcedir (`KpsResult`, `PersonSummary`, `KpsCriteria`).

## 5. Metotlar

| Metot | `Data` tipi |
|---|---|
| `BilesikKisiSorgulaAsync(identityNumber)` | `BilesikKisiBilgisi` → `.ToSummary()` |
| `BilesikKisiveAdresSorgulaAsync(identityNumber)` | `BilesikKisiveAdresBilgisi` |
| `BilesikKisiveYerlesimYeriSorgulaAsync(identityNumber)` | `BilesikKisiveYerlesimYeriBilgisi` |
| `TcKimlikNoSorgulaAsync(KpsCriteria.Person(...))` | `TCKisiBilgisi` |
| `KimlikNoIleAdresBilgisiSorgulaAsync(identityNumber)` | `KisiAdresBilgisi` |
| `KimlikNoIleYerlesimYeriSorgulaAsync(identityNumber)` | `TcKimlikNoIleAdresBilgisi` |
| `AdresNoIleYerlesimYeriSorgulaAsync(addressNumber)` | `AcikAdres` |
| `KimlikNoIleNufusKayitOrnegiSorgulaAsync(identityNumber)` | `GenelNufusKayitOrnegi` |
| `AileBireyleriSorgulaAsync(KpsCriteria.FamilyMembers(...))` | `AileBilgisi` |
| `KimlikNoIleVatandasAileBilgisi[KardesDahil]SorgulaAsync(KpsCriteria.Kinship(...))` | `YakinlikServisiAileBilgisi` |
| `KimlikNoIleMaviKartliAileBilgisi[KardesDahil]SorgulaAsync(KpsCriteria.Kinship(...))` | `MaviKartliAileBilgisi` |
| `KimlikNoIleVatandasAileYakinlikBilgisi[KardesDahil]SorgulaAsync(KpsCriteria.PublicKinship(...))` | `KamuYakinlikServisiAileBilgisi` |
| `YetkiListesiAsync()` | `WcfService[]` → `.MethodPermissions()`, `.FieldPermissions(metot)` |
| `ParametreAdListesiAsync()` / `ParametreDegerleriAsync(ad)` | `string[]` / `Parametre[]` |

Her metodun dönen alanlarının tamamı: [metot-referansi.md](metot-referansi.md).

## 6. Sonuç ve hatalar

Her metot `KpsResult<T>` döner:

| Üye | İçerik |
|---|---|
| `Data` | KPS'nin döndürdüğü veri (tüm alanlar) |
| `Error` | Servisin iş hatası, ör. `[2] Kayıt bulunamadı`. Fırlatılmaz |
| `IsSuccess` | Hata yok ve veri dolu |
| `InnerErrors` | Cevabın iç kısımlarındaki hatalar (ör. adres geçmişi bulunamadı) |
| `GetDataOrThrow()` | Başarılıysa `Data`, değilse `KpsException` |

Teknik hatalar `KpsException` olarak fırlatılır. `ex.Kind` hatanın türünü söyler:

| `Kind` | Neden | Ne yapmalı |
|---|---|---|
| `Connection` | KPS'ye bağlanılamadı; Prod'da çoğunlukla IP tanımlı değil | IP'yi KPS'ye tanımlatın |
| `Timeout` | KPS zamanında cevap vermedi (test ortamında sık) | Otomatik yeniden denenir; sürerse sonra tekrar deneyin |
| `Server` | KPS sunucu hatası | Otomatik yeniden denenir |
| `Authentication` | Kullanıcı adı/şifre, kurum veya uygulama kodu hatalı | Ayarları kontrol edin |
| `Unauthorized` | Kurumun bu metoda yetkisi yok | KPS Yönetim'den yetki isteyin |
| `Configuration` | `Kps` ayarlarında zorunlu alan eksik | Hata mesajındaki alanları doldurun |
| `InvalidResponse` | KPS cevabı beklenen formatta değil | Paketin güncel sürümüne geçin |

- Geçersiz kimlik numarasında `ArgumentException` fırlatılır; KPS'ye istek gitmez.
- Bir alan boş geliyorsa kurumun o alanı görme yetkisi olmayabilir: `(await kps.YetkiListesiAsync()).Data.FieldPermissions("BilesikKisiSorgula")`.
- **KVKK:** Kimlik numaralarını ve kişi bilgilerini loglara yazmayın; kimlik numarasını URL'de değil istek gövdesinde taşıyın.

## 7. Test ortamı ve Prod'a geçiş

Geliştirirken `"Environment": "Test"` ile Bakanlığın test ortamını kullanın. Herkese açık test kullanıcısı: `Username` `test_user`, `Password` `f6)@6U:l`, `InstitutionCode` `123456`, `ApplicationCode` `8353df93-453c-4e23-8be8-2f913dd35313` (kodda `KpsOptions.PublicTestUser()`). Test ortamında yalnızca örnek kimlik numaraları sorgulanabilir (ör. `10774881040`); listeler `docs/kaynaklar/test-kimlik-no/` klasöründe. Test kullanıcısı 9 KPS metoduna yetkilidir; diğerleri `Unauthorized` döner.

Prod'a geçmeden önce:

- [ ] Test ortamında bir kişi sorgusu başarılı
- [ ] `Kps` bölümüne Prod bilgileri girildi, `Environment` = `Prod`
- [ ] Şifre içeren dosya depoya gönderilmiyor
- [ ] Sunucunun çıkış IP'si KPS'ye tanımlı
- [ ] `YetkiListesiAsync()` ile kullanılacak metotların yetkisi görüldü

## 8. NVİ KPS

NVİ (Nüfus ve Vatandaşlık İşleri Genel Müdürlüğü) KPS, Sağlık Bakanlığı KPS'sinden **ayrı bir sistemdir**: başvuru [kpsbasvuru.nvi.gov.tr](https://kpsbasvuru.nvi.gov.tr) üzerinden yapılır, onaylanınca NVİ bir web servis kullanıcı adı, şifre ve yetki verilen servislerin listesini bildirir. Kurum/uygulama kodu yoktur. İki sistem aynı projede birlikte kullanılabilir.

Paketin tipli olarak sunduğu NVİ servisleri:

| NVİ servisi | C# | `Data` tipi |
|---|---|---|
| `TCKimlikNoDogrulaServis` | `TCKimlikNoDogrulaAsync(identityNumber, firstName, lastName, birthYear)` | `TCKimlikNoDogrulama` |
| `KisiSorgulaTCKimlikNoServis` | `KisiSorgulaTCKimlikNoAsync(identityNumber)` | `KisiBilgisi` |

İki metodun da birden çok kişiyi tek istekte sorgulayan liste alan sürümü vardır (`Data` dizi döner). Diğer NVİ servisleri için `CallAsync(servis, işlem, gövdeXml)`.

Ayarlar (`appsettings.json`):

```json
{
  "NviKps": {
    "Username": "<NVİ web servis kullanıcı adı>",
    "Password": "<NVİ web servis şifresi>"
  }
}
```

İsteğe bağlı alanlar `Kps` bölümündekilerle aynıdır: `Timeout`, `RetryCount`, `RetryDelay`, `ValidateIdentityNumber`. Sunucuda şifreyi `NviKps__Password` ortam değişkeniyle verebilirsiniz.

```csharp
// Program.cs
builder.Services.AddNviKpsClient(builder.Configuration.GetSection("NviKps"));
```

```csharp
using KpsV2.Nvi;

public class PatientVerificationService(INviKpsClient nvi)
{
    public async Task<bool> VerifyAsync(long identityNumber, string firstName, string lastName, int birthYear, CancellationToken ct)
    {
        var result = await nvi.TCKimlikNoDogrulaAsync(identityNumber, firstName, lastName, birthYear, ct);
        return result.IsSuccess;   // eşleşmezse result.Error dolu döner
    }
}
```

```csharp
var person = (await nvi.KisiSorgulaTCKimlikNoAsync(identityNumber)).Data;
var name = person?.TemelBilgisi?.Ad;
var birthDate = person?.TemelBilgisi?.DogumTarih?.Date;    // DateOnly?
var status = person?.DurumBilgisi?.Durum?.Aciklama;        // Açık, Ölüm...
```

Sonuç (`KpsResult<T>`) ve hatalar (`KpsException.Kind`) [6. bölümdekiyle](#6-sonuç-ve-hatalar) aynıdır. `Authentication`: NVİ kullanıcı adı veya şifresi hatalı; `Unauthorized`: kurumun bu NVİ servisine yetkisi yok.

- **Test ortamı yoktur.** NVİ herkese açık bir test ortamı sunmaz; sorgu servisine yalnızca başvuruda tanımlı IP'lerden erişilebilir. Bu yüzden NVİ servislerinin cevapları şimdilik NVİ'nin yayınladığı WSDL şemasına göre test edilmiştir; ilk gerçek çağrı depodaki kontrol aracıyla (`tools/KpsV2.ProdCheck`) doğrulanır.
- Paketin kullandığı WSDL'ler NVİ'nin herkese açık adresindedir: `https://kpsbasvuru.nvi.gov.tr/Services/Wsdl.ashx?Service=<servis adı>`.

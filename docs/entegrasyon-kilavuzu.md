# Entegrasyon Kılavuzu

Bir .NET projesini KPS v2'ye `KpsV2.Client` paketi üzerinden bağlamak için adım adım anlatım. Ayarların, metotların ve hataların ayrıntısı `kullanim.md` dosyasındadır.

## Gereksinimler

| Gereken | Nereden alınır |
| --- | --- |
| .NET 7+ proje | `TargetFramework` net7.0 veya üstü (ASP.NET Core, konsol, worker) |
| Kullanıcı adı | Sorgulamayı yapacak kişinin T.C. kimlik no |
| Şifre | kpsyonetimv2.saglik.gov.tr'de belirlenen web servis şifresi |
| Kurum kodu | Sağlık kuruluşunun kodu |
| Uygulama kodu | Kimlik Paylaşım Birimi'nin uygulamaya verdiği GUID |
| Metot yetkileri | Kurumun ihtiyaç duyduğu KPS metotlarına yetkisi (KPS Yönetim) |
| Tanımlı IP (yalnızca Prod) | Sunucunun çıkış IP'si KPS'ye tanımlı olmalı |

Merkez teşkilat uygulamaları (uygulama kullanıcısı) kurum ve uygulama kodu yerine **sorgulayan kimlik no** (`RequesterIdentityNumber`) kullanır.

## Adım 1 – Paketi projeye ekleyin

Paket nuget.org'da yayınlanır: https://www.nuget.org/packages/KpsV2.Client. Ek bir paket kaynağı veya token gerekmez:

```bash
dotnet add package KpsV2.Client
```

`.csproj` içinde şu satırın oluştuğunu kontrol edin:

```xml
<PackageReference Include="KpsV2.Client" Version="2.0.1" />
```

Kaynak kod ve sürüm notları: https://github.com/colakkonur/kpsv2-client (Releases sayfası). Aynı paket GitHub Packages'ta da yayınlanır (`https://nuget.pkg.github.com/colakkonur/index.json`), ancak oradan indirmek için `read:packages` yetkili bir GitHub token'ı gerekir; normal kullanımda nuget.org yeterlidir.

## Adım 2 – Ayarları projenin appsettings.json dosyasına ekleyin

KPS bilgileri projenizin kendi `appsettings.json` dosyasındaki `Kps` bölümüne girilir:

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

- Geliştirme sırasında `"Environment": "Test"` ile Bakanlığın test ortamına bağlanabilirsiniz (bkz. Adım 5).
- `Password` içeren dosyayı kaynak koduyla birlikte depoya göndermeyin. Sunucuda şifreyi `Kps__Password` ortam değişkeni veya Key Vault ile de verebilirsiniz; .NET bunu `appsettings.json`'ın üzerine yazar.

Tüm ayar alanları (zaman aşımı, yeniden deneme...): `kullanim.md` → Ayarlar.

## Adım 3 – Servisi kaydedin

`Program.cs` dosyasına tek satır eklenir:

```csharp
builder.Services.AddKpsClient(builder.Configuration.GetSection("Kps"));
```

Bu satır `IKpsClient` servisini singleton olarak kaydeder. Token önbelleği tüm istekler arasında paylaşılır; ayrıca bir şey yapmanız gerekmez.

## Adım 4 – Sorgu yapın

`IKpsClient` servisini constructor ile alın:

```csharp
using KpsV2;
using KpsV2.Contracts;

public class PatientIdentityService(IKpsClient kps)
{
    public async Task<PersonSummary?> GetAsync(long identityNumber, CancellationToken ct)
    {
        var result = await kps.BilesikKisiSorgulaAsync(identityNumber, ct);
        if (!result.IsSuccess)
            return null;              // result.Error: ör. [2] Kayıt bulunamadı

        return result.Data.ToSummary();   // FullName, BirthDate, Gender, IsDeceased, Type...
    }
}
```

Hangi ihtiyaç için hangi metot:

| İhtiyaç | Metot |
| --- | --- |
| Kişi bilgisi (TC / yabancı / mavi kart) | `BilesikKisiSorgulaAsync(identityNumber)` |
| Kişi + adres | `BilesikKisiveAdresSorgulaAsync(identityNumber)` |
| Yerleşim yeri ve adres geçmişi | `KimlikNoIleYerlesimYeriSorgulaAsync(identityNumber)` |
| Kimlik numarası bilinmiyor | `TcKimlikNoSorgulaAsync(KpsCriteria.Person(...))` |
| Aile bilgisi | `KimlikNoIleVatandasAileBilgisiSorgulaAsync(KpsCriteria.Kinship(identityNumber, birthDate))` |
| Nüfus kayıt örneği | `KimlikNoIleNufusKayitOrnegiSorgulaAsync(identityNumber)` |
| Kurumun yetkileri | `YetkiListesiAsync()` |

18 metodun tamamı `kullanim.md` → Metotlar bölümünde, dönen alanların tamamı ise `metot-referansi.md` dosyasındadır.

Hataları iki grupta ele alın:

- **İş sonuçları** (ör. kayıt bulunamadı) fırlatılmaz; `result.Error` alanında döner.
- **Teknik hatalar** (bağlantı, yetki, kimlik doğrulama) `KpsException` olarak fırlatılır. `ex.Kind` hatanın türünü söyler. Geçici hatalar otomatik olarak yeniden denenir. Bir web API'de türleri HTTP durum kodlarına eşleyin: `Unauthorized` → 403, `ex.IsTransient` → 503, `Authentication` → 502.

Hepsini bir arada gösteren çalışan örnek: `samples/KpsV2.Sample.WebApi`.

Kimlik numaralarını ve kişi bilgilerini loglara yazmayın (KVKK). Kimlik numarasını URL'de değil, istek gövdesinde taşıyın.

## Adım 5 – Test edin ve Prod'a geçin

Önce `"Environment": "Test"` ile deneyin. Test ortamında IP kısıtı yoktur, ama yalnızca örnek kimlik numaraları sorgulanabilir (ör. `10774881040`). Listeler `kaynaklar/test-kimlik-no/` klasöründedir. Bakanlığın herkese açık test kullanıcısının bilgileri: `Username` `test_user`, `Password` `f6)@6U:l`, `InstitutionCode` `123456`, `ApplicationCode` `8353df93-453c-4e23-8be8-2f913dd35313` (kodda `KpsOptions.PublicTestUser()`). Bu kullanıcı her metoda yetkili değildir; bkz. `kullanim.md` → Test ortamı.

Prod'a geçmeden önce:

- [ ] Test ortamında `YetkiListesiAsync()` ve bir kişi sorgusu başarılı
- [ ] Prod kullanıcı adı, kurum kodu ve uygulama kodu `Kps` bölümüne girildi; `Environment` = `Prod`
- [ ] Şifre içeren ayar dosyası depoya gönderilmiyor
- [ ] Sunucunun çıkış IP'si KPS'ye tanımlı
- [ ] Prod kontrol aracı (`gelistirici-rehberi.md` → Prod kontrolü) kullanılacak metotlarda ✔ veriyor

## Sık karşılaşılan durumlar

| Belirti | `ex.Kind` | Neden | Çözüm |
| --- | --- | --- | --- |
| Ayarlar eksik | `Configuration` | `Kps` bölümünde zorunlu alan yok | Hata mesajındaki alanları doldurun |
| Kullanıcı adı veya şifre yanlış / Uygulama bulunamadı | `Authentication` | STS bilgileri reddetti | Şifre, kurum kodu ve uygulama kodunu kontrol edin |
| Prod'da bağlantı kurulamıyor | `Connection` / `Timeout` | Sunucu IP'si KPS'ye tanımlı değil | Kimlik Paylaşım Birimi'nden IP tanımlattırın |
| Bu işlem için yetkiniz bulunmamaktadır | `Unauthorized` | Kurumun o metoda yetkisi yok | `YetkiListesiAsync()` ile kontrol edin, yetki talep edin |
| The request channel timed out (HTTP 500) | `Timeout` | KPS arka servisi 6 sn'de cevap vermedi (test ortamında sık) | Otomatik yeniden denenir; sürerse sonra tekrar deneyin |
| `ArgumentException`: geçersiz kimlik no | — | 11 hane değil veya kontrol haneleri tutmuyor | Numarayı kontrol edin; istek gönderilmez |
| `result.Error` = [2] Kayıt bulunamadı | — | Kriterlere uyan kişi yok | Normal sonuç; kullanıcıya bildirin |
| Bazı alanlar boş | — | Kurumun o alanı görme yetkisi yok | `.Data.FieldPermissions(metot)` ile kontrol edin |

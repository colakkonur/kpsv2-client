# Geliştirici Rehberi

Bu depoyu geliştirenler için. Paketi kullanmak için [kullanim.md](kullanim.md).

## Klasörler

| Klasör | İçerik |
| --- | --- |
| `src/KpsV2.Client` | Paket. `Contracts/KpsContracts.g.cs` (Sağlık KPS) ve `Contracts/NviContracts.g.cs` (NVİ KPS) WSDL'lerden üretilir, elle değiştirilmez. NVİ istemcisi `Nvi/` altında |
| `tests/KpsV2.Client.Tests` | Testler |
| `samples/KpsV2.Sample.WebApi` | Örnek ASP.NET Core projesi (Test ortamına bağlanır) |
| `tools/KpsV2.ProdCheck` | Prod'da Sağlık KPS'nin 18 metodunu ve NVİ KPS'nin 2 servisini birer kez deneyen kontrol aracı |
| `tools/generate-contracts.sh` | WSDL'ler değişirse `KpsContracts.g.cs` ve `NviContracts.g.cs`'i yeniden üretir |
| `docs` | Dokümanlar; `kaynaklar/` altında Bakanlığın teknik dokümanı ve test kimlik numaraları |

## Nasıl çalışır

1. KPS'nin token servisinden (STS) kullanıcı adı, şifre, kurum ve uygulama koduyla token alınır ve süresi dolana kadar saklanır.
2. Her sorgu bu token ile SOAP olarak KPS'ye gönderilir.
3. Cevap, WSDL'den üretilen C# tiplerine çevrilir.

KPS'nin kullandığı WCF bağlaması .NET Core'da desteklenmediği için SOAP mesajı elle kurulur (Bakanlığın örnek projesi de böyle yapar).

NVİ KPS (`Nvi/NviKpsClient.cs`) aynı akışı izler, iki farkla: STS'ten simetrik anahtarlı token istenir (kurum/uygulama kodu yoktur) ve her sorguda WS-Security zaman damgası bu anahtarla HMAC-SHA1 olarak imzalanır. Bu, NVİ WSDL'lerindeki güvenlik politikasının gereğidir (TransportBinding + IssuedToken/SymmetricKey). Zaman damgası ve SignedInfo exc-c14n biçiminde yazılır; `NviKpsClientTests` imzayı .NET'in `SignedXml` doğrulayıcısıyla kontrol eder. HTTP gönderimi, hata sınıflandırması, yeniden deneme ve token önbelleği iki istemcide ortaktır (`SoapTransport.cs`).

## Test

```bash
dotnet test                                            # birim testleri (KPS'ye gitmez)
KPS_TEST_ENV=1 dotnet test --filter Environment=Test   # Bakanlığın test ortamına gerçek istek
```

Test ortamının arka servisi sık sık zaman aşımına düşer (özellikle `Bilesik*` metotları); bu kod hatası değildir.

Doküman kod örneklerini değiştirirseniz `DokumanOrnekleriTestleri.cs` dosyasını da güncelleyin. `tests/.../Ornekler/` altına yalnızca test ortamı cevapları konur, Prod cevapları asla.

## Prod kontrolü

Prod bilgileri yalnızca depo kökündeki `appsettings.json` dosyasına girilir. Bu dosya `.gitignore`'dadır, depoya girmez.

```bash
cp appsettings.example.json appsettings.json      # doldurun
dotnet run --project tools/KpsV2.ProdCheck        # her metodu birer kez dener, kişisel veri yazdırmaz
```

Araç, `appsettings.json`'da hangi bölüm doluysa (`Kps`, `NviKps`) o sistemi dener. KPS'ye IP'si tanımlı bir makinede çalıştırın. NVİ'nin test ortamı olmadığından NVİ servislerinin gerçek cevap biçimi ilk kez bu araçla görülür; beklenmeyen bir şey çıkarsa (`InvalidResponse`, iş hatası kodları) `NviKpsClientTests`'teki sahte cevapları gerçek cevaba göre güncelleyin (kişisel veri içermeyecek şekilde).

## Değişiklik ve yayın süreci

`main`'e doğrudan commit atılamaz; her değişiklik bir PR ile gelir.

1. Bir dal açın, değişikliği yapın, PR açın.
2. Paket kodunu (`src/KpsV2.Client`) değiştirdiyseniz `KpsV2.Client.csproj` içindeki `<Version>`'ı artırın (ör. `2.0.1` → `2.0.2`). Artırmazsanız CI PR'ı durdurur. Yalnızca doküman veya test değişiyorsa sürüme dokunmayın.
3. PR'ın merge edilebilmesi için: CI testleri geçmeli ve **@colakkonur** onaylamalı.
4. Merge olunca `publish.yml` kendiliğinden çalışır: yeni sürümü nuget.org'a yayınlar, `v<sürüm>` etiketiyle bir GitHub Release oluşturur. nuget.org sürümü birkaç dakika içinde listeler.

nuget.org'a giriş API anahtarı olmadan yapılır (nuget.org'daki `kpsv2-client-policy` Trusted Publishing politikası); `publish.yml` dosyasının adı değişirse bu politika da güncellenmelidir. Paket yalnızca nuget.org'dan dağıtılır.

## KPS WSDL'i değişirse

```bash
tools/generate-contracts.sh && dotnet test
```

Yeni bir metot eklendiyse `IKpsClient`, `KpsClient`, `Contracts/KpsContracts.Ekler.cs`, `MetotSozlesmeTestleri.cs` ve `metot-referansi.md` dosyalarını güncelleyin; eksik kalırsa `MetotSozlesmeTestleri` testi kırılır.

**Yeni bir NVİ servisi** (ör. NVİ onay yazısında yetki verilen başka bir servis): adını `tools/generate-contracts.sh` içindeki `NVI_SERVISLER`'e ekleyip betiği çalıştırın, ardından `INviKpsClient` / `NviKpsClient`'a mevcut iki servis gibi bir metot, `NviKpsClientTests`'e test ve `metot-referansi.md`'ye bölüm ekleyin. NVİ'nin servis adını tanımadığı WSDL isteği `<error>Lütfen geçerli bir servis adı belirtiniz!</error>` döner; betik bu durumda durur.

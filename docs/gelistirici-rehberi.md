# Geliştirici Rehberi

Bu depoyu geliştirenler için. Paketi kullanmak için [kullanim.md](kullanim.md).

## Klasörler

| Klasör | İçerik |
| --- | --- |
| `src/KpsV2.Client` | Paket. `Contracts/KpsContracts.g.cs` KPS WSDL'inden üretilir, elle değiştirilmez |
| `tests/KpsV2.Client.Tests` | Testler |
| `samples/KpsV2.Sample.WebApi` | Örnek ASP.NET Core projesi (Test ortamına bağlanır) |
| `tools/KpsV2.ProdCheck` | Prod'da 18 metodu birer kez deneyen kontrol aracı |
| `tools/generate-contracts.sh` | KPS WSDL'i değişirse `KpsContracts.g.cs`'i yeniden üretir |
| `docs` | Dokümanlar; `kaynaklar/` altında Bakanlığın teknik dokümanı ve test kimlik numaraları |

## Nasıl çalışır

1. KPS'nin token servisinden (STS) kullanıcı adı, şifre, kurum ve uygulama koduyla token alınır ve süresi dolana kadar saklanır.
2. Her sorgu bu token ile SOAP olarak KPS'ye gönderilir.
3. Cevap, WSDL'den üretilen C# tiplerine çevrilir.

KPS'nin kullandığı WCF bağlaması .NET Core'da desteklenmediği için SOAP mesajı elle kurulur (Bakanlığın örnek projesi de böyle yapar).

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
dotnet run --project tools/KpsV2.ProdCheck        # 18 metodu birer kez dener, kişisel veri yazdırmaz
```

KPS'ye IP'si tanımlı bir makinede çalıştırın.

## Yeni sürüm yayınlama

```bash
git tag v2.0.2
git push origin v2.0.2
```

GitHub Actions (`.github/workflows/publish.yml`) testleri çalıştırır, paketi nuget.org'a yayınlar ve sürüm notlarıyla bir GitHub Release oluşturur. Sürüm numarası etiketten alınır. nuget.org'a giriş API anahtarı olmadan yapılır (nuget.org'daki `kpsv2-client-policy` Trusted Publishing politikası); `publish.yml` dosyasının adı değişirse bu politika da güncellenmelidir. nuget.org yeni sürümü birkaç dakika içinde listeler.

Paket yalnızca nuget.org'dan dağıtılır.

## KPS WSDL'i değişirse

```bash
tools/generate-contracts.sh && dotnet test
```

Yeni bir metot eklendiyse `IKpsClient`, `KpsClient`, `Contracts/KpsContracts.Ekler.cs`, `MetotSozlesmeTestleri.cs` ve `metot-referansi.md` dosyalarını güncelleyin; eksik kalırsa `MetotSozlesmeTestleri` testi kırılır.

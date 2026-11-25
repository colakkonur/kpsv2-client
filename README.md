# KpsV2.Client

[![NuGet](https://img.shields.io/nuget/v/KpsV2.Client)](https://www.nuget.org/packages/KpsV2.Client)

Sağlık Bakanlığı **Kimlik Paylaşım Sistemi (KPS v2)** için .NET paketi. Kps servisinin 16 ve Parametre servisinin 2 metodunu tipli istek/cevaplarla sunar. Ayrı bir sistem olan **NVİ Kimlik Paylaşımı Sistemi**'nin TC kimlik no doğrulama ve kişi sorgulama servislerini de içerir.

> Resmî paket değildir; Sağlık Bakanlığı veya NVİ ile bağlantısı yoktur. Kullanmak için kendi KPS hesap bilgileriniz gerekir.

```bash
dotnet add package KpsV2.Client
```

```csharp
builder.Services.AddKpsClient(builder.Configuration.GetSection("Kps"));

var result = await kps.BilesikKisiSorgulaAsync(identityNumber);
var person = result.Data.ToSummary();   // FullName, BirthDate, Gender...

// NVİ KPS
builder.Services.AddNviKpsClient(builder.Configuration.GetSection("NviKps"));

var verified = (await nvi.TCKimlikNoDogrulaAsync(identityNumber, "AHMET", "YILMAZ", 1980)).IsSuccess;
```

## Dokümanlar

| Doküman | İçerik |
| --- | --- |
| [Kullanım](docs/kullanim.md) | Kurulum, ayarlar, sorgu, hatalar, Prod'a geçiş, NVİ KPS |
| [Metot referansı](docs/metot-referansi.md) | Her metodun döndürdüğü alanlar |
| [Geliştirici rehberi](docs/gelistirici-rehberi.md) | Bu depoyu geliştirenler için: test, yayın |

Bakanlığın KPS teknik dokümanı: [docs/kaynaklar](docs/kaynaklar/). NVİ KPS'nin WSDL'leri: `https://kpsbasvuru.nvi.gov.tr/Services/Wsdl.ashx?Service=<servis adı>`.

## Lisans

MIT

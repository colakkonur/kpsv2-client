# KPS v2 Entegrasyonu

Sağlık Bakanlığı Kimlik Paylaşım Sistemi (KPS v2) için `KpsV2.Client` NuGet paketi. Kps servisinin 16 ve Parametre servisinin 2 metodunu, WSDL'den üretilmiş tipli istek/cevaplarla sunar.

[![NuGet](https://img.shields.io/nuget/v/KpsV2.Client)](https://www.nuget.org/packages/KpsV2.Client)
[![CI](https://github.com/colakkonur/kpsv2-client/actions/workflows/ci.yml/badge.svg)](https://github.com/colakkonur/kpsv2-client/actions/workflows/ci.yml)

```bash
dotnet add package KpsV2.Client
```

```csharp
builder.Services.AddKpsClient(builder.Configuration.GetSection("Kps"));
var result = await kps.BilesikKisiSorgulaAsync(identityNumber);
```

Tüm dokümanlar [`docs/`](docs/README.md) klasöründedir.

> Resmî paket değildir; Sağlık Bakanlığı ile bağlantısı yoktur. Kullanmak için kendi KPS hesap bilgileriniz gerekir.

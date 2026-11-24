# KPS v2 Entegrasyonu

Sağlık Bakanlığı Kimlik Paylaşım Sistemi (KPS v2) için `KpsV2.Client` NuGet paketi. Kps servisinin 16 ve Parametre servisinin 2 metodunu, WSDL'den üretilmiş tipli istek/cevaplarla sunar.

```bash
dotnet add package KpsV2.Client   # kaynak: https://nuget.pkg.github.com/colakkonur/index.json
```

```csharp
builder.Services.AddKpsClient(builder.Configuration.GetSection("Kps"));
var result = await kps.BilesikKisiSorgulaAsync(identityNumber);
```

Tüm dokümanlar [`docs/`](docs/README.md) klasöründedir.

> Resmî paket değildir; Sağlık Bakanlığı ile bağlantısı yoktur. Kullanmak için kendi KPS hesap bilgileriniz gerekir.

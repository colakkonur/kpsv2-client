# KPS v2 Entegrasyonu — Dokümanlar

`KpsV2.Client`, Sağlık Bakanlığı Kimlik Paylaşım Sistemi (KPS v2) için .NET NuGet paketidir. Projeler KPS'ye bu paket üzerinden bağlanır. Paket, Kps servisinin 16 ve Parametre servisinin 2 metodunu WSDL'den üretilmiş tipli istek/cevaplarla sunar; token yönetimi, yeniden deneme ve hata sınıflandırması da paketin içindedir.

Tüm dokümanlar bu klasördedir. Her dosyanın tek bir amacı vardır:

| Doküman | Ne için | Kim okur |
| --- | --- | --- |
| [entegrasyon-kilavuzu.md](entegrasyon-kilavuzu.md) | Bir projeyi KPS'ye adım adım bağlamak ve Prod'a geçmek | Paketi kullanacak proje ekipleri |
| [kullanim.md](kullanim.md) | API başvurusu: ayarlar, metotlar, `KpsResult`, hata türleri. NuGet paket sayfasında da bu gösterilir | Paketi kullanacak proje ekipleri |
| [metot-referansi.md](metot-referansi.md) | 18 metodun argümanları ve dönen alanların tam ağacı | Paketi kullanacak proje ekipleri |
| [gelistirici-rehberi.md](gelistirici-rehberi.md) | Depo yapısı, ayarlar, mimari, testler, Prod kontrol aracı, sözleşme üretme, paketleme | Bu paketi geliştirenler |
| [kaynaklar/](kaynaklar/) | Bakanlığın KPS teknik dokümanı (PDF) ve test ortamı kimlik numarası listeleri. Kaynak: [kpsv2test.saglik.gov.tr → Örnek Kodlar](https://kpsv2test.saglik.gov.tr/Details/Examples) | Başvuru |

**KPS bilgilerini nereye gireceğim?** İki ortam vardır: Test ve Prod. Paketi kullanan bir projede bilgiler o projenin `appsettings.json` dosyasındaki `Kps` bölümüne girilir. Bu depoda yalnızca Prod bilgileri girilir ve tek yeri kökteki `appsettings.json` dosyasıdır (şablon: `appsettings.example.json`). Test ortamı için bilgi girmeye gerek yoktur. Ayrıntı: `gelistirici-rehberi.md` → KPS bilgileri nereye girilir.

Nereden başlamalı:

- **Projemi KPS'ye bağlayacağım** → `entegrasyon-kilavuzu.md`
- **Bir metodun ne döndürdüğünü arıyorum** → `metot-referansi.md`
- **Pakete özellik ekleyeceğim veya yeni sürüm çıkaracağım** → `gelistirici-rehberi.md`

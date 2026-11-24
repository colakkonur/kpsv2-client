# Geliştirici Rehberi

`KpsV2.Client` paketini geliştiren ve yayınlayanlar için: depo yapısı, ayarlar, mimari, testler, Prod kontrolü, sözleşme üretme ve paketleme. Paketi kullanan projeler için `entegrasyon-kilavuzu.md` ve `kullanim.md` dosyalarına bakın.

## Depo yapısı

| Klasör / dosya | İçerik |
| --- | --- |
| `appsettings.example.json` | Prod bilgileri şablonu; `appsettings.json` olarak kopyalanır (bkz. [KPS bilgileri nereye girilir](#kps-bilgileri-nereye-girilir)) |
| `src/KpsV2.Client` | Kütüphane (net7.0, NuGet paketi) |
| `src/KpsV2.Client/Contracts` | `KpsContracts.g.cs`: WSDL'den üretilen veri tipleri, elle değiştirilmez. `KpsContracts.Ekler.cs`: bu tiplere partial sınıflarla eklenen davranışlar |
| `tests/KpsV2.Client.Tests` | Birim testleri, gerçek cevaplarla testler (`Ornekler/`), gerçek ortam testleri |
| `samples/KpsV2.Sample.WebApi` | Tüketici proje örneği: DI, sorgular, hata → HTTP eşlemesi. Yalnızca Test ortamına bağlanır |
| `tools/KpsV2.ProdCheck` | 18 metodu birer kez çağıran Prod kontrol aracı |
| `tools/generate-contracts.sh` | WSDL'den veri tiplerini yeniden üretir |
| `docs` | Tüm dokümanlar; `kaynaklar/` altında Bakanlığın teknik dokümanı ve test kimlik numaraları |
| `.github/workflows` | CI (derleme + test) ve yayın (GitHub Packages) iş akışları |

## KPS bilgileri nereye girilir

Yalnızca iki ortam vardır: **Test** (kpsv2test.saglik.gov.tr) ve **Prod** (kpsv2.saglik.gov.tr).

| Ortam | Bu depoda | Paketi kullanan projede |
| --- | --- | --- |
| Test | Hiçbir şey girilmez. Bakanlığın herkese açık test kullanıcısı kodda hazırdır (`KpsOptions.PublicTestUser()`) | Projenin `appsettings.json` → `Kps` bölümü, `"Environment": "Test"` |
| Prod | **Tek yer:** depo kökündeki `appsettings.json`. Prod testleri ve Prod kontrol aracı bu dosyayı okur | Projenin `appsettings.json` → `Kps` bölümü, `"Environment": "Prod"` |

Kökteki `appsettings.json`, paketi kullanan bir projenin `appsettings.json`'ıyla aynı `Kps` bölümünü kullanır. Ek olarak, Prod kontrolünde sorgulanacak kişi için bir `ProdCheck` bölümü vardır:

```bash
cp appsettings.example.json appsettings.json      # sonra appsettings.json'u doldurun
```

```json
{
  "Kps": {
    "Environment": "Prod",
    "Username": "<sorgulayan kişinin TC kimlik no>",
    "Password": "<KPS web servis şifresi>",
    "InstitutionCode": "<kurum kodu>",
    "ApplicationCode": "<uygulama GUID>"
  },
  "ProdCheck": { "IdentityNumber": 0 }
}
```

Kökteki `appsettings.json` `.gitignore`'dadır ve depoya girmez. Dosya şifre içerdiği için işiniz bitince silmeniz önerilir. Örnek uygulamanın (`samples/`) kendi `appsettings.json` dosyası yalnızca Test ortamı içindir; Prod bilgileri oraya girilmez.

## Mimari

1. **Token:** STS'ye (`/STS/STSService.svc`) WS-Trust 1.3 isteği gönderilir. İstekte UsernameToken olarak kullanıcı adı ve şifre bulunur. Kurum kodu ile uygulama kodu ya da sorgulayan kimlik no ise WS-Addressing referans parametresi olarak gider (`KurumKodu`, `UygulamaKodu`, `SorgulayanKimlikNo`). Dönen imzalı SAML 1.1 assertion, imza bozulmasın diye yeniden serileştirilmeden ham metin olarak saklanır. Token `NotOnOrAfter` zamanına kadar önbellekte tutulur; eşzamanlı isteklerde `SemaphoreSlim` ile tek sefer alınır.
2. **Sorgu:** Token, SOAP 1.2 zarfının `Security` başlığına konup Router'a (`/Router/RoutingService.svc`) gönderilir. Hangi servisin çağrılacağını SOAP Action belirler (`.../IKpsServices/{Metot}`, `.../IParametreServices/{Metot}`).
3. **Tipler:** İstek kriterleri ve `<{Metot}Result>` cevapları, WSDL'den üretilen DataContract tipleriyle `DataContractSerializer` üzerinden işlenir. Her cevap zarfı `IKpsServiceResult<T>` arayüzünü uygular (`HataBilgisi` + `Sonuc`). Bu sayede 18 metot tek bir generic yoldan geçer.
4. **Hatalar:** `KpsClient.Classify`, HTTP kodu ve SOAP Fault metninden `KpsErrorKind` üretir. Geçici türler üstel beklemeyle yeniden denenir. KPS token'ı reddederse token sıfırlanır ve bir kez daha denenir.

**Adlandırma:** Paketin public API'si İngilizcedir. KPS'nin metot adları (`BilesikKisiSorgulaAsync`...) ve WSDL tipleri (`KpsV2.Contracts`) Bakanlığın adlarıyla Türkçe kalır, çünkü dokümanları ve hata mesajları bu adlarla eşleşir.

**Neden WCF istemcisi değil?** KPS `WS2007FederationHttpBinding` kullanıyor ve .NET Core WCF istemcisi bu binding'i desteklemiyor (svcutil uç noktaları "uyumsuz bağlama" diye atlıyor). Bu yüzden SOAP zarfı elle kuruluyor; Bakanlığın resmi örnek projesi de aynı yöntemi kullanıyor. svcutil'den yalnızca veri tipleri alınıyor.

**Neden net7.0?** Paket, .NET 7 kullanan mevcut projelere de eklenebilsin diye bu hedefle derleniyor. Daha yeni .NET sürümleri de paketi sorunsuz kullanır.

## Komutlar

```bash
dotnet build
dotnet test                                            # birim testleri (sunucuya gitmez)
KPS_TEST_ENV=1 dotnet test --filter Environment=Test   # gerçek test ortamı (kpsv2test)
dotnet test --filter Environment=Prod                  # Prod (kökteki appsettings.json, tanımlı IP'den)
dotnet run --project tools/KpsV2.ProdCheck             # Prod kontrolü: 18 metot birer kez
cd samples/KpsV2.Sample.WebApi && dotnet run           # örnek API: http://localhost:5080/kps/...
```

## Testler

| Dosya | Ne doğrular | Sunucu |
| --- | --- | --- |
| `KpsClientTests.cs` | Token önbelleği ve eşzamanlılık, STS parametreleri, yeniden deneme, hata sınıflandırması, özet, iç hatalar, DI | Sahte (`SahteKps`) |
| `MetotSozlesmeTestleri.cs` | 18 metodun her biri: SOAP Action, istek gövdesi, tüm alanları dolu bir WSDL nesnesinin kayıpsız okunması, üst seviye hata | Sahte |
| `GercekCevapTestleri.cs` | kpsv2test'ten kaydedilmiş gerçek cevapların (`Ornekler/*.xml`) ayrıştırılması | Sahte |
| `DokumanOrnekleriTestleri.cs` | `kullanim.md` ve `entegrasyon-kilavuzu.md` dosyalarındaki kod örneklerinin derlenmesi | Sahte |
| `GercekOrtamTestleri.cs` | Test ortamında yetkili metotlar; Prod'da yetki listesi ve örnek kişi | Gerçek (varsayılan olarak atlanır) |

Dokümanlardaki bir kod örneğini değiştirirseniz `DokumanOrnekleriTestleri.cs` dosyasını da güncelleyin.

`Ornekler/` altındaki dosyalar herkese açık test verisidir. Prod'dan alınan cevapları buraya **koymayın**; kişisel veri içerir.

Prod testleri bilgileri kökteki `appsettings.json` dosyasından okur. Dosya yoksa bu testler atlanır.

### Test ortamı hakkında bilinenler (24.11.2026)

- Herkese açık test kullanıcıları (`test_user`, `test_user2`) 16 KPS metodundan yalnızca 9'una yetkilidir. Parametre servisine yetkileri yoktur.
- Yetki listesinde 16 metodun 13'ü görünür. AdresNoIleYerlesimYeriSorgula ve iki "Yakınlık" metodu listede hiç yer almaz.
- Arka servis, özellikle `Bilesik*` metotlarında, sık sık "The request channel timed out ... 00:00:06" (HTTP 500) döner. Bu bir kod hatası değildir; kütüphane bu durumu `Timeout` olarak sınıflar ve yeniden dener.

## Prod kontrolü

`tools/KpsV2.ProdCheck`, `IKpsClient`'ın 18 metodunu birer kez çağırır ve her metot için şunları raporlar: yetki listesindeki durum, sonuç (✔ başarılı / ○ iş hatası / ⛔ yetkisiz / ✘ hata), süre ve dolu alan sayısı. Kişisel veri yazdırmaz.

```bash
dotnet run --project tools/KpsV2.ProdCheck                    # Prod: kökteki appsettings.json'u okur
dotnet run --project tools/KpsV2.ProdCheck -- --test          # herkese açık test kullanıcısıyla
```

- Prod IP kısıtlı olduğu için aracı KPS'ye tanımlı IP'li bir makinede çalıştırın.
- `ProdCheck:IdentityNumber` zorunludur. Sorgulama izniniz olan bir kişinin kimlik numarasını yazın (ör. kendi TC'niz).
- `BirthDate` ve `AddressNumber` verilmezse sorgu sonuçlarından alınır.
- Bir hata (✘) varsa araç `2` çıkış koduyla biter.

## Sözleşmeleri güncelleme

`Contracts/KpsContracts.g.cs` dosyası KPS WSDL'inden üretilir. KPS yeni bir alan veya metot eklediğinde ya da `InvalidResponse` hataları görüldüğünde:

```bash
tools/generate-contracts.sh     # test ortamı WSDL'i; Prod için KPS_WSDL_HOST=https://kpsv2.saglik.gov.tr
git diff src/KpsV2.Client/Contracts
dotnet test
```

Yeni bir metot eklendiyse sırasıyla şunları güncelleyin:
1. `IKpsClient` ve `KpsClient`
2. `KpsContracts.Ekler.cs` (`IKpsServiceResult<T>` satırı)
3. `MetotSozlesmeTestleri.Durumlar`
4. `metot-referansi.md`

`MetotSozlesmeTestleri` testi, arayüzdeki metot listesiyle test listesini karşılaştırır; bir metot eklenip test listesine eklenmezse kırılır.

`metot-referansi.md` dosyası, kpsv2test'teki **Servis Bilgileri** sayfasının (`/details/getAll` JSON'u) içeriğinden derlenmiştir.

## Paketleme ve yayın

Kaynak kod GitHub'da: https://github.com/colakkonur/kpsv2-client. İki GitHub Actions iş akışı vardır:

| İş akışı | Ne zaman | Ne yapar |
| --- | --- | --- |
| `.github/workflows/ci.yml` | `main`'e her push ve her pull request | Derleme, birim testleri, paketleme denemesi |
| `.github/workflows/publish.yml` | `v*` etiketi atıldığında | Derleme, test, paketleme; paketi **nuget.org** ve **GitHub Packages**'a yayınlar, `.nupkg` dosyalarıyla bir GitHub Release oluşturur |

Yeni sürüm çıkarmak:

```bash
# Değişiklik notunu src/KpsV2.Client/KpsV2.Client.csproj içindeki <PackageReleaseNotes>'a yazın
git tag v2.0.1
git push origin v2.0.1
```

Paket sürümü etiketten alınır (`v2.0.1` → `2.0.1`); csproj'daki `<Version>`'ı da aynı tutun. Aynı sürüm numarası ikinci kez yayınlanamaz.

Yayında hiçbir kalıcı anahtar kullanılmaz:
- **nuget.org:** Trusted Publishing (OIDC). nuget.org'da `colakkonur` hesabında `kpsv2-client-policy` politikası tanımlıdır (repo `colakkonur/kpsv2-client`, iş akışı `publish.yml`, paketler `KpsV2.*`). Repo'daki `NUGET_USER` değişkeni (`colakkonur`) bu adımı açar; değişken silinirse nuget.org adımı atlanır. İş akışı dosyasının adı değişirse politikanın da güncellenmesi gerekir.
- **GitHub Packages:** Actions'ın kendi `GITHUB_TOKEN`'ı.

nuget.org yeni sürümü doğrulayıp listelemeden önce birkaç dakika (ilk yayında ~7 dk) bekletir. Paketin NuGet README'si `docs/kullanim.md` dosyasıdır; pack sırasında pakete kopyalanır.

Yerelde paket üretmek için (yayınlamadan): `dotnet pack src/KpsV2.Client -c Release -o nupkg`. `nupkg/` klasörü depoya girmez.

Gerçek ortam testleri (Test ve Prod) Actions'ta çalışmaz: Test ortamı kararsızdır, Prod ise IP kısıtlıdır ve kimlik bilgisi ister. Bunları yerelde çalıştırın.

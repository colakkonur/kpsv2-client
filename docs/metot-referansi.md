# KPS v2 Metot Referansı

Her metodun argümanları ve döndürdüğü alanlar. Kaynak: kpsv2test.saglik.gov.tr **Servis Bilgileri** sayfası. Tüm metotlar `KpsResult<T>` döner; aşağıdaki ağaçlar `Data` içindeki alanlardır (her düğümdeki `HataBilgisi` gösterilmemiştir).

"Test kullanıcısı" sütunu, test ortamındaki herkese açık kullanıcının o metoda yetkisi olup olmadığını gösterir. Kendi kurumunuzun yetkileri için `YetkiListesiAsync()`.

## Özet

| KPS metodu | C# | Dönüş (`Data`) | Test kullanıcısı |
|---|---|---|---|
| [BilesikKisiSorgula](#bilesikkisisorgula) | `BilesikKisiSorgulaAsync(long identityNumber)` | `BilesikKisiBilgisi` | ✅ |
| [BilesikKisiveAdresSorgula](#bilesikkisiveadressorgula) | `BilesikKisiveAdresSorgulaAsync(long identityNumber)` | `BilesikKisiveAdresBilgisi` | — |
| [BilesikKisiveYerlesimYeriSorgula](#bilesikkisiveyerlesimyerisorgula) | `BilesikKisiveYerlesimYeriSorgulaAsync(long identityNumber)` | `BilesikKisiveYerlesimYeriBilgisi` | ✅ |
| [TcKimlikNoSorgula](#tckimliknosorgula) | `TcKimlikNoSorgulaAsync(KisiSorgulaKisiBilgileriCO criteria)` | `TCKisiBilgisi` | ✅ |
| [KimlikNoIleAdresBilgisiSorgula](#kimliknoileadresbilgisisorgula) | `KimlikNoIleAdresBilgisiSorgulaAsync(long identityNumber)` | `KisiAdresBilgisi` | — |
| [KimlikNoIleYerlesimYeriSorgula](#kimliknoileyerlesimyerisorgula) | `KimlikNoIleYerlesimYeriSorgulaAsync(long identityNumber)` | `TcKimlikNoIleAdresBilgisi` | ✅ |
| [AdresNoIleYerlesimYeriSorgula](#adresnoileyerlesimyerisorgula) | `AdresNoIleYerlesimYeriSorgulaAsync(long addressNumber)` | `AcikAdres` | — |
| [AileBireyleriSorgula](#ailebireylerisorgula) | `AileBireyleriSorgulaAsync(AileBireyleriSorgulaKriter criteria)` | `AileBilgisi` | — |
| [KimlikNoIleNufusKayitOrnegiSorgula](#kimliknoilenufuskayitornegisorgula) | `KimlikNoIleNufusKayitOrnegiSorgulaAsync(long identityNumber)` | `GenelNufusKayitOrnegi` | — |
| [KimlikNoIleVatandasAileBilgisiSorgula](#kimliknoilevatandasailebilgisisorgula) | `KimlikNoIleVatandasAileBilgisiSorgulaAsync(YakinlikSorgulaKriter criteria)` | `YakinlikServisiAileBilgisi` | ✅ |
| [KimlikNoIleMaviKartliAileBilgisiSorgula](#kimliknoilemavikartliailebilgisisorgula) | `KimlikNoIleMaviKartliAileBilgisiSorgulaAsync(YakinlikSorgulaKriter criteria)` | `MaviKartliAileBilgisi` | ✅ |
| [KimlikNoIleVatandasAileBilgisiKardesDahilSorgula](#kimliknoilevatandasailebilgisikardesdahilsorgula) | `KimlikNoIleVatandasAileBilgisiKardesDahilSorgulaAsync(YakinlikSorgulaKriter criteria)` | `YakinlikServisiAileBilgisi` | ✅ |
| [KimlikNoIleMaviKartliAileBilgisiKardesDahilSorgula](#kimliknoilemavikartliailebilgisikardesdahilsorgula) | `KimlikNoIleMaviKartliAileBilgisiKardesDahilSorgulaAsync(YakinlikSorgulaKriter criteria)` | `MaviKartliAileBilgisi` | ✅ |
| [KimlikNoIleVatandasAileYakinlikBilgisiSorgula](#kimliknoilevatandasaileyakinlikbilgisisorgula) | `KimlikNoIleVatandasAileYakinlikBilgisiSorgulaAsync(KamuYakinlikSorgulaKriter criteria)` | `KamuYakinlikServisiAileBilgisi` | — |
| [KimlikNoIleVatandasAileYakinlikBilgisiKardesDahilSorgula](#kimliknoilevatandasaileyakinlikbilgisikardesdahilsorgula) | `KimlikNoIleVatandasAileYakinlikBilgisiKardesDahilSorgulaAsync(KamuYakinlikSorgulaKriter criteria)` | `KamuYakinlikServisiAileBilgisi` | — |
| [YetkiListesi](#yetkilistesi) | `YetkiListesiAsync()` | `WcfService[]` | ✅ |
| ParametreAdListesi | `ParametreAdListesiAsync()` | `string[]` | — |
| ParametreDegerleri | `ParametreDegerleriAsync(string parameterName)` | `Parametre[]` | — |

Kolay kriter oluşturma: `KpsCriteria.Person(...)`, `KpsCriteria.Kinship(identityNumber, birthDate)`, `KpsCriteria.PublicKinship(...)`, `KpsCriteria.FamilyMembers(identityNumber, wholeFamily)`.

## BilesikKisiSorgula

```csharp
Task<KpsResult<BilesikKisiBilgisi>> BilesikKisiSorgulaAsync(long identityNumber)
```

<div style="font-weight:bold;font-style:italic;">TCVatandasKisiKutukleri Alanındaki;</div>
<span style="font-weight:bold;text-decoration:underline;">GeciciKimlikBilgisi:</span> Türkiye Cumhuriyeti Kimlik Kartı (TCKK) almak suretiyle, Nüfus cüzdanlarını değiştiren kişilere verilen geçici belgedeki veriler bu objeden dönmektedir.
<br>
<span style="font-weight:bold;text-decoration:underline;">HataBilgisi:</span> Herhangi bir hata durumunda, ilgili hatanın detayını tutar
<br>
<span style="font-weight:bold;text-decoration:underline;">KisiBilgisi :</span> Sorgulanan kişiye ait kimlik bilgileri bu objeden dönmektedir.
<br>
<span style="font-weight:bold;text-decoration:underline;">NufusCuzdaniBilgisi:</span> Nüfus cüzdanı üzerinde bulunan kişiye ait bilgilerin döndüğü objedir. Eğer kişinin TCKK'sı yoksa bu obje null döner. Eğer kişi daha Nufüs Cüzdanı almamışsa yani ilk kimliği TCKK ise bu obje null döner
<br>
<span style="font-weight:bold;text-decoration:underline;">TCKKBilgisi:</span> Türkiye Cumhuriyeti Kimlik Kartı (TCKK) almış kişilerin kart üzerinde bulunan kişiye ait bilgilerin döndüğü objedir. Eğer kişinin TCKK'sı yoksa bu obje null döner.
<br>

**Argümanlar**

- `kimlikNo` : Int64

**Dönen alanlar** (`Data`)

- `KimlikNo` : Int64?
- `MaviKartliKisiKutukleri` : MaviKartliKisiKutukleri
  - `MaviKartBilgisi` : MaviKartBilgisi
    - `Ad` : String
    - `AnneAd` : String
    - `BabaAd` : String
    - `Birim` : Int32?
    - `Cinsiyet` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `DogumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DogumYer` : String
    - `DogumYerKod` : Int32?
    - `KartKayitNo` : Int64?
    - `KimlikNo` : Int64?
    - `MedeniHal` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `No` : Int32?
    - `OncekiSoyad` : String
    - `Seri` : String
    - `Soyad` : String
    - `Uyruk` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `VerilisNeden` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `VerilmeTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
  - `KisiBilgisi` : MaviKartKisiBilgisi
    - `AnneTCKimlikNo` : Int64?
    - `BabaTCKimlikNo` : Int64?
    - `DogumYerKod` : Int32?
    - `DurumBilgisi` : MaviKartKisiDurumBilgisi
      - `Durum` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `MedeniHal` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `OlumTarih` : TarihBilgisi
        - `Yil` : Int32?
        - `Ay` : Int32?
        - `Gun` : Int32?
    - `GercekKisiKimlikNo` : Int64?
    - `KazanilanTCKimlikNo` : Int64?
    - `KimlikNo` : Int64?
    - `TemelBilgisi` : TemelBilgisi
      - `Ad` : String
      - `AnneAd` : String
      - `BabaAd` : String
      - `Cinsiyet` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `DogumTarih` : TarihBilgisi
        - `Yil` : Int32?
        - `Ay` : Int32?
        - `Gun` : Int32?
      - `DogumYer` : String
      - `Soyad` : String
    - `Ulke` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `VeliVasiTur` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `VeliVasiVar` : Boolean?
- `TCVatandasiKisiKutukleri` : TCVatandasiKisiKutukleri
  - `GeciciKimlikBilgisi` : TCGeciciKimlikBilgisi
    - `Ad` : String
    - `AnneAd` : String
    - `BabaAd` : String
    - `BelgeNo` : String
    - `Cinsiyet` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `DogumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DuzenlenmeTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DuzenleyenIlce` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `OncekiSoyad` : String
    - `SonGecerlilikTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `Soyad` : String
    - `TCKimlikNo` : Int64?
  - `KisiBilgisi` : TCKisiBilgisi
    - `AnneTCKimlikNo` : Int64?
    - `BabaTCKimlikNo` : Int64?
    - `DogumYerKod` : Int32?
    - `DurumBilgisi` : TCKisiDurumBilgisi
      - `Din` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `Durum` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `MedeniHal` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `OlumTarih` : TarihBilgisi
        - `Yil` : Int32?
        - `Ay` : Int32?
        - `Gun` : Int32?
    - `KayitYeriBilgisi` : KayitYeriBilgisi
      - `AileSiraNo` : Int32?
      - `BireySiraNo` : Int32?
      - `Cilt` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `Il` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `Ilce` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
    - `TCKimlikNo` : Int64?
    - `OncekiYabanciKimlikNo` : Int64?
    - `TemelBilgisi` : TemelBilgisi
      - `Ad` : String
      - `AnneAd` : String
      - `BabaAd` : String
      - `Cinsiyet` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `DogumTarih` : TarihBilgisi
        - `Yil` : Int32?
        - `Ay` : Int32?
        - `Gun` : Int32?
      - `DogumYer` : String
      - `Soyad` : String
    - `VeliVasiTur` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `VeliVasiVar` : Boolean?
  - `NufusCuzdaniBilgisi` : TCCuzdanBilgisi
    - `Ad` : String
    - `AnaAd` : String
    - `BabaAd` : String
    - `CuzdanKayitNo` : Int64?
    - `CuzdanVerilmeNeden` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `DogumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DogumYer` : String
    - `No` : Int32?
    - `Seri` : String
    - `Soyad` : String
    - `TCKimlikNo` : Int64?
    - `VerildigiIlce` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `VerilmeTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
  - `TCKKBilgisi` : TCKK
    - `Ad` : String
    - `AnneAd` : String
    - `BabaAd` : String
    - `BasvuruNeden` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `Cinsiyet` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `DogumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DogumYer` : String
    - `KayitNo` : Int64?
    - `SeriNo` : String
    - `SonGecerlilikTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `Soyad` : String
    - `TCKimlikNo` : Int64?
    - `TeslimEdenBirim` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `TeslimTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `VerenMakam` : String
- `YabanciKisiKutukleri` : YabanciKisiKutukleri
  - `KisiBilgisi` : YabanciKisiBilgisi
    - `AnneKimlikNo` : Int64?
    - `BabaKimlikNo` : Int64?
    - `BitisTarihiBelirsizOlmaNeden` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `DogumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DurumBilgisi` : YabanciKisiDurumBilgisi
      - `Durum` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `MedeniHal` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
    - `DogumYerKod` : Int32?
    - `GercekKisiKimlikNo` : Int64?
    - `IzinBaslangicTarih` : DateTime?
    - `IzinBitisTarih` : DateTime?
    - `IzinDuzenlenenIl` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `IzinNo` : String
    - `KaynakBirim` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `KazanilanTCKimlikNo` : Int64?
    - `KimlikNo` : Int64?
    - `OlumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `Statu` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `TemelBilgisi` : TemelBilgisi
      - `Ad` : String
      - `AnneAd` : String
      - `BabaAd` : String
      - `Cinsiyet` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `DogumTarih` : TarihBilgisi
        - `Yil` : Int32?
        - `Ay` : Int32?
        - `Gun` : Int32?
      - `DogumYer` : String
      - `Soyad` : String
    - `Uyruk` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `VeliVasiTur` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `VeliVasiVar` : Boolean?

## BilesikKisiveAdresSorgula

```csharp
Task<KpsResult<BilesikKisiveAdresBilgisi>> BilesikKisiveAdresSorgulaAsync(long identityNumber)
```

**Argümanlar**

- `kimlikNo` : Int64

**Dönen alanlar** (`Data`)

- `KimlikNo` : Int64?
- `MaviKartliKisiKutukleri` : MaviKartliKisiKutukleri
  - `MaviKartBilgisi` : MaviKartBilgisi
    - `Ad` : String
    - `AnneAd` : String
    - `BabaAd` : String
    - `Birim` : Int32?
    - `Cinsiyet` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `DogumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DogumYer` : String
    - `DogumYerKod` : Int32?
    - `KartKayitNo` : Int64?
    - `KimlikNo` : Int64?
    - `MedeniHal` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `No` : Int32?
    - `OncekiSoyad` : String
    - `Seri` : String
    - `Soyad` : String
    - `Uyruk` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `VerilisNeden` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `VerilmeTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
  - `KisiBilgisi` : MaviKartKisiBilgisi
    - `AnneTCKimlikNo` : Int64?
    - `BabaTCKimlikNo` : Int64?
    - `DogumYerKod` : Int32?
    - `DurumBilgisi` : MaviKartKisiDurumBilgisi
      - `Durum` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `MedeniHal` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `OlumTarih` : TarihBilgisi
        - `Yil` : Int32?
        - `Ay` : Int32?
        - `Gun` : Int32?
    - `GercekKisiKimlikNo` : Int64?
    - `KazanilanTCKimlikNo` : Int64?
    - `KimlikNo` : Int64?
    - `TemelBilgisi` : TemelBilgisi
      - `Ad` : String
      - `AnneAd` : String
      - `BabaAd` : String
      - `Cinsiyet` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `DogumTarih` : TarihBilgisi
        - `Yil` : Int32?
        - `Ay` : Int32?
        - `Gun` : Int32?
      - `DogumYer` : String
      - `Soyad` : String
    - `Ulke` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `VeliVasiTur` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `VeliVasiVar` : Boolean?
- `TCVatandasiKisiKutukleri` : TCVatandasiKisiKutukleri
  - `GeciciKimlikBilgisi` : TCGeciciKimlikBilgisi
    - `Ad` : String
    - `AnneAd` : String
    - `BabaAd` : String
    - `BelgeNo` : String
    - `Cinsiyet` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `DogumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DuzenlenmeTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DuzenleyenIlce` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `OncekiSoyad` : String
    - `SonGecerlilikTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `Soyad` : String
    - `TCKimlikNo` : Int64?
  - `KisiBilgisi` : TCKisiBilgisi
    - `AnneTCKimlikNo` : Int64?
    - `BabaTCKimlikNo` : Int64?
    - `DogumYerKod` : Int32?
    - `DurumBilgisi` : TCKisiDurumBilgisi
      - `Din` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `Durum` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `MedeniHal` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `OlumTarih` : TarihBilgisi
        - `Yil` : Int32?
        - `Ay` : Int32?
        - `Gun` : Int32?
    - `KayitYeriBilgisi` : KayitYeriBilgisi
      - `AileSiraNo` : Int32?
      - `BireySiraNo` : Int32?
      - `Cilt` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `Il` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `Ilce` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
    - `TCKimlikNo` : Int64?
    - `OncekiYabanciKimlikNo` : Int64?
    - `TemelBilgisi` : TemelBilgisi
      - `Ad` : String
      - `AnneAd` : String
      - `BabaAd` : String
      - `Cinsiyet` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `DogumTarih` : TarihBilgisi
        - `Yil` : Int32?
        - `Ay` : Int32?
        - `Gun` : Int32?
      - `DogumYer` : String
      - `Soyad` : String
    - `VeliVasiTur` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `VeliVasiVar` : Boolean?
  - `NufusCuzdaniBilgisi` : TCCuzdanBilgisi
    - `Ad` : String
    - `AnaAd` : String
    - `BabaAd` : String
    - `CuzdanKayitNo` : Int64?
    - `CuzdanVerilmeNeden` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `DogumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DogumYer` : String
    - `No` : Int32?
    - `Seri` : String
    - `Soyad` : String
    - `TCKimlikNo` : Int64?
    - `VerildigiIlce` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `VerilmeTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
  - `TCKKBilgisi` : TCKK
    - `Ad` : String
    - `AnneAd` : String
    - `BabaAd` : String
    - `BasvuruNeden` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `Cinsiyet` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `DogumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DogumYer` : String
    - `KayitNo` : Int64?
    - `SeriNo` : String
    - `SonGecerlilikTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `Soyad` : String
    - `TCKimlikNo` : Int64?
    - `TeslimEdenBirim` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `TeslimTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `VerenMakam` : String
- `YabanciKisiKutukleri` : YabanciKisiKutukleri
  - `KisiBilgisi` : YabanciKisiBilgisi
    - `AnneKimlikNo` : Int64?
    - `BabaKimlikNo` : Int64?
    - `BitisTarihiBelirsizOlmaNeden` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `DogumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DurumBilgisi` : YabanciKisiDurumBilgisi
      - `Durum` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `MedeniHal` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
    - `DogumYerKod` : Int32?
    - `GercekKisiKimlikNo` : Int64?
    - `IzinBaslangicTarih` : DateTime?
    - `IzinBitisTarih` : DateTime?
    - `IzinDuzenlenenIl` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `IzinNo` : String
    - `KaynakBirim` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `KazanilanTCKimlikNo` : Int64?
    - `KimlikNo` : Int64?
    - `OlumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `Statu` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `TemelBilgisi` : TemelBilgisi
      - `Ad` : String
      - `AnneAd` : String
      - `BabaAd` : String
      - `Cinsiyet` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `DogumTarih` : TarihBilgisi
        - `Yil` : Int32?
        - `Ay` : Int32?
        - `Gun` : Int32?
      - `DogumYer` : String
      - `Soyad` : String
    - `Uyruk` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `VeliVasiTur` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `VeliVasiVar` : Boolean?
- `AdresBilgisi` : KisiAdresBilgisi
  - `AcikAdres` : String
  - `AdresNo` : Int64?
  - `AdresTip` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `BeldeAdresi` : BeldeAdresi
    - `BagimsizBolumDurum` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BagimsizBolumTipi` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BinaAda` : String
    - `BinaBlokAdi` : String
    - `BinaDurum` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BinaKodu` : Int64?
    - `BinaNo` : Int32?
    - `BinaNumaratajTipi` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BinaPafta` : String
    - `BinaParsel` : String
    - `BinaSiteAdi` : String
    - `BinaYapiTipi` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `Csbm` : String
    - `CsbmKodu` : Int32?
    - `DisKapiNo` : String
    - `IcKapiNo` : String
    - `Il` : String
    - `Ilce` : String
    - `IlceKodu` : Int32?
    - `IlKodu` : Int32?
    - `KatNo` : String
    - `Koy` : String
    - `KoyKayitNo` : Int64?
    - `KoyKodu` : Int64?
    - `Mahalle` : String
    - `MahalleKodu` : Int32?
    - `TapuBagimsizBolumNo` : String
    - `YapiKullanimAmac` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `YolAltiKatSayisi` : Int32?
    - `YolUstuKatSayisi` : Int32?
  - `BeyandaBulunanKimlikNo` : Int64?
  - `BeyanTarihi` : TarihBilgisi
    - `Yil` : Int32?
    - `Ay` : Int32?
    - `Gun` : Int32?
  - `IlIlceMerkezAdresi` : IlIlceMerkezAdresi
    - `BagimsizBolumDurum` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BagimsizBolumTipi` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BinaAda` : String
    - `BinaBlokAdi` : String
    - `BinaDurum` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BinaKodu` : Int64?
    - `BinaNo` : Int32?
    - `BinaNumaratajTipi` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BinaPafta` : String
    - `BinaParsel` : String
    - `BinaSiteAdi` : String
    - `BinaYapiTipi` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `Csbm` : String
    - `CsbmKodu` : Int32?
    - `DisKapiNo` : String
    - `IcKapiNo` : String
    - `Il` : String
    - `Ilce` : String
    - `IlceKodu` : Int32?
    - `IlKodu` : Int32?
    - `KatNo` : String
    - `Mahalle` : String
    - `MahalleKodu` : Int32?
    - `TapuBagimsizBolumNo` : String
    - `YapiKullanimAmac` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `YolAltiKatSayisi` : Int32?
    - `YolUstuKatSayisi` : Int32?
  - `KoyAdresi` : KoyAdresi
    - `BagimsizBolumDurum` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BagimsizBolumTipi` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BinaAda` : String
    - `BinaBlokAdi` : String
    - `BinaDurum` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BinaKodu` : Int64?
    - `BinaNo` : Int32?
    - `BinaNumaratajTipi` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BinaPafta` : String
    - `BinaParsel` : String
    - `BinaSiteAdi` : String
    - `BinaYapiTipi` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `Csbm` : String
    - `CsbmKodu` : Int32?
    - `DisKapiNo` : String
    - `IcKapiNo` : String
    - `Il` : String
    - `Ilce` : String
    - `IlceKodu` : Int32?
    - `IlKodu` : Int32?
    - `KatNo` : String
    - `Koy` : String
    - `KoyKayitNo` : Int64?
    - `KoyKodu` : Int64?
    - `Mahalle` : String
    - `MahalleKodu` : Int32?
    - `TapuBagimsizBolumNo` : String
    - `YapiKullanimAmac` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `YolAltiKatSayisi` : Int32?
    - `YolUstuKatSayisi` : Int32?
  - `TasinmaTarihi` : TarihBilgisi
    - `Yil` : Int32?
    - `Ay` : Int32?
    - `Gun` : Int32?
  - `TescilTarihi` : TarihBilgisi
    - `Yil` : Int32?
    - `Ay` : Int32?
    - `Gun` : Int32?
  - `YurtDisiAdresi` : YurtDisiAdresi
    - `DisTemsDuzeltmeBeyanTarih` : DateTime?
    - `DisTemsDuzeltmeKararTarih` : DateTime?
    - `DisTemsDuzeltmeNeden` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `DisTemsilcilik` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `YabanciAdres` : String
    - `YabanciSehir` : String
    - `YabanciUlke` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String

## BilesikKisiveYerlesimYeriSorgula

```csharp
Task<KpsResult<BilesikKisiveYerlesimYeriBilgisi>> BilesikKisiveYerlesimYeriSorgulaAsync(long identityNumber)
```

**Argümanlar**

- `kimlikNo` : Int64

**Dönen alanlar** (`Data`)

- `KimlikNo` : Int64?
- `MaviKartliKisiKutukleri` : MaviKartliKisiKutukleri
  - `MaviKartBilgisi` : MaviKartBilgisi
    - `Ad` : String
    - `AnneAd` : String
    - `BabaAd` : String
    - `Birim` : Int32?
    - `Cinsiyet` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `DogumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DogumYer` : String
    - `DogumYerKod` : Int32?
    - `KartKayitNo` : Int64?
    - `KimlikNo` : Int64?
    - `MedeniHal` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `No` : Int32?
    - `OncekiSoyad` : String
    - `Seri` : String
    - `Soyad` : String
    - `Uyruk` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `VerilisNeden` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `VerilmeTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
  - `KisiBilgisi` : MaviKartKisiBilgisi
    - `AnneTCKimlikNo` : Int64?
    - `BabaTCKimlikNo` : Int64?
    - `DogumYerKod` : Int32?
    - `DurumBilgisi` : MaviKartKisiDurumBilgisi
      - `Durum` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `MedeniHal` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `OlumTarih` : TarihBilgisi
        - `Yil` : Int32?
        - `Ay` : Int32?
        - `Gun` : Int32?
    - `GercekKisiKimlikNo` : Int64?
    - `KazanilanTCKimlikNo` : Int64?
    - `KimlikNo` : Int64?
    - `TemelBilgisi` : TemelBilgisi
      - `Ad` : String
      - `AnneAd` : String
      - `BabaAd` : String
      - `Cinsiyet` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `DogumTarih` : TarihBilgisi
        - `Yil` : Int32?
        - `Ay` : Int32?
        - `Gun` : Int32?
      - `DogumYer` : String
      - `Soyad` : String
    - `Ulke` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `VeliVasiTur` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `VeliVasiVar` : Boolean?
- `TCVatandasiKisiKutukleri` : TCVatandasiKisiKutukleri
  - `GeciciKimlikBilgisi` : TCGeciciKimlikBilgisi
    - `Ad` : String
    - `AnneAd` : String
    - `BabaAd` : String
    - `BelgeNo` : String
    - `Cinsiyet` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `DogumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DuzenlenmeTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DuzenleyenIlce` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `OncekiSoyad` : String
    - `SonGecerlilikTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `Soyad` : String
    - `TCKimlikNo` : Int64?
  - `KisiBilgisi` : TCKisiBilgisi
    - `AnneTCKimlikNo` : Int64?
    - `BabaTCKimlikNo` : Int64?
    - `DogumYerKod` : Int32?
    - `DurumBilgisi` : TCKisiDurumBilgisi
      - `Din` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `Durum` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `MedeniHal` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `OlumTarih` : TarihBilgisi
        - `Yil` : Int32?
        - `Ay` : Int32?
        - `Gun` : Int32?
    - `KayitYeriBilgisi` : KayitYeriBilgisi
      - `AileSiraNo` : Int32?
      - `BireySiraNo` : Int32?
      - `Cilt` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `Il` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `Ilce` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
    - `TCKimlikNo` : Int64?
    - `OncekiYabanciKimlikNo` : Int64?
    - `TemelBilgisi` : TemelBilgisi
      - `Ad` : String
      - `AnneAd` : String
      - `BabaAd` : String
      - `Cinsiyet` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `DogumTarih` : TarihBilgisi
        - `Yil` : Int32?
        - `Ay` : Int32?
        - `Gun` : Int32?
      - `DogumYer` : String
      - `Soyad` : String
    - `VeliVasiTur` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `VeliVasiVar` : Boolean?
  - `NufusCuzdaniBilgisi` : TCCuzdanBilgisi
    - `Ad` : String
    - `AnneAd` : String
    - `BabaAd` : String
    - `CuzdanKayitNo` : Int64?
    - `CuzdanVerilmeNeden` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `DogumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DogumYer` : String
    - `No` : Int32?
    - `Seri` : String
    - `Soyad` : String
    - `TCKimlikNo` : Int64?
    - `VerildigiIlce` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `VerilmeTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
  - `TCKKBilgisi` : TCKK
    - `Ad` : String
    - `AnneAd` : String
    - `BabaAd` : String
    - `BasvuruNeden` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `Cinsiyet` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `DogumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DogumYer` : String
    - `KayitNo` : Int64?
    - `SeriNo` : String
    - `SonGecerlilikTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `Soyad` : String
    - `TCKimlikNo` : Int64?
    - `TeslimEdenBirim` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `TeslimTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `VerenMakam` : String
- `YabanciKisiKutukleri` : YabanciKisiKutukleri
  - `KisiBilgisi` : YabanciKisiBilgisi
    - `AnneKimlikNo` : Int64?
    - `BabaKimlikNo` : Int64?
    - `BitisTarihiBelirsizOlmaNeden` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `DogumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DurumBilgisi` : YabanciKisiDurumBilgisi
      - `Durum` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `MedeniHal` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
    - `DogumYerKod` : Int32?
    - `GercekKisiKimlikNo` : Int64?
    - `IzinBaslangicTarih` : DateTime?
    - `IzinBitisTarih` : DateTime?
    - `IzinDuzenlenenIl` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `IzinNo` : String
    - `KaynakBirim` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `KazanilanTCKimlikNo` : Int64?
    - `KimlikNo` : Int64?
    - `OlumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `Statu` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `TemelBilgisi` : TemelBilgisi
      - `Ad` : String
      - `AnneAd` : String
      - `BabaAd` : String
      - `Cinsiyet` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `DogumTarih` : TarihBilgisi
        - `Yil` : Int32?
        - `Ay` : Int32?
        - `Gun` : Int32?
      - `DogumYer` : String
      - `Soyad` : String
    - `Uyruk` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `VeliVasiTur` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `VeliVasiVar` : Boolean?
- `AdresBilgisi` : TcKimlikNoIleAdresBilgisi
  - `TumAdresBilgileri` : KisiAdresBilgisi[]
    - `AcikAdres` : String
    - `AdresNo` : Int64?
    - `AdresTip` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BeldeAdresi` : BeldeAdresi
      - `BagimsizBolumDurum` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `BagimsizBolumTipi` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `BinaAda` : String
      - `BinaBlokAdi` : String
      - `BinaDurum` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `BinaKodu` : Int64?
      - `BinaNo` : Int32?
      - `BinaNumaratajTipi` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `BinaPafta` : String
      - `BinaParsel` : String
      - `BinaSiteAdi` : String
      - `BinaYapiTipi` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `Csbm` : String
      - `CsbmKodu` : Int32?
      - `DisKapiNo` : String
      - `IcKapiNo` : String
      - `Il` : String
      - `Ilce` : String
      - `IlceKodu` : Int32?
      - `IlKodu` : Int32?
      - `KatNo` : String
      - `Koy` : String
      - `KoyKayitNo` : Int64?
      - `KoyKodu` : Int64?
      - `Mahalle` : String
      - `MahalleKodu` : Int32?
      - `TapuBagimsizBolumNo` : String
      - `YapiKullanimAmac` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `YolAltiKatSayisi` : Int32?
      - `YolUstuKatSayisi` : Int32?
    - `BeyandaBulunanKimlikNo` : Int64?
    - `BeyanTarihi` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `IlIlceMerkezAdresi` : IlIlceMerkezAdresi
      - `BagimsizBolumDurum` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `BagimsizBolumTipi` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `BinaAda` : String
      - `BinaBlokAdi` : String
      - `BinaDurum` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `BinaKodu` : Int64?
      - `BinaNo` : Int32?
      - `BinaNumaratajTipi` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `BinaPafta` : String
      - `BinaParsel` : String
      - `BinaSiteAdi` : String
      - `BinaYapiTipi` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `Csbm` : String
      - `CsbmKodu` : Int32?
      - `DisKapiNo` : String
      - `IcKapiNo` : String
      - `Il` : String
      - `Ilce` : String
      - `IlceKodu` : Int32?
      - `IlKodu` : Int32?
      - `KatNo` : String
      - `Mahalle` : String
      - `MahalleKodu` : Int32?
      - `TapuBagimsizBolumNo` : String
      - `YapiKullanimAmac` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `YolAltiKatSayisi` : Int32?
      - `YolUstuKatSayisi` : Int32?
    - `KoyAdresi` : KoyAdresi
      - `BagimsizBolumDurum` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `BagimsizBolumTipi` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `BinaAda` : String
      - `BinaBlokAdi` : String
      - `BinaDurum` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `BinaKodu` : Int64?
      - `BinaNo` : Int32?
      - `BinaNumaratajTipi` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `BinaPafta` : String
      - `BinaParsel` : String
      - `BinaSiteAdi` : String
      - `BinaYapiTipi` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `Csbm` : String
      - `CsbmKodu` : Int32?
      - `DisKapiNo` : String
      - `IcKapiNo` : String
      - `Il` : String
      - `Ilce` : String
      - `IlceKodu` : Int32?
      - `IlKodu` : Int32?
      - `KatNo` : String
      - `Koy` : String
      - `KoyKayitNo` : Int64?
      - `KoyKodu` : Int64?
      - `Mahalle` : String
      - `MahalleKodu` : Int32?
      - `TapuBagimsizBolumNo` : String
      - `YapiKullanimAmac` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `YolAltiKatSayisi` : Int32?
      - `YolUstuKatSayisi` : Int32?
    - `TasinmaTarihi` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `TescilTarihi` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `YurtDisiAdresi` : YurtDisiAdresi
      - `DisTemsDuzeltmeBeyanTarih` : DateTime?
      - `DisTemsDuzeltmeKararTarih` : DateTime?
      - `DisTemsDuzeltmeNeden` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `DisTemsilcilik` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `YabanciAdres` : String
      - `YabanciSehir` : String
      - `YabanciUlke` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
  - `YerlesimYeriAdresi` : KisiAdresBilgisi
    - `AcikAdres` : String
    - `AdresNo` : Int64?
    - `AdresTip` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BeldeAdresi` : BeldeAdresi
      - `BagimsizBolumDurum` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `BagimsizBolumTipi` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `BinaAda` : String
      - `BinaBlokAdi` : String
      - `BinaDurum` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `BinaKodu` : Int64?
      - `BinaNo` : Int32?
      - `BinaNumaratajTipi` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `BinaPafta` : String
      - `BinaParsel` : String
      - `BinaSiteAdi` : String
      - `BinaYapiTipi` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `Csbm` : String
      - `CsbmKodu` : Int32?
      - `DisKapiNo` : String
      - `IcKapiNo` : String
      - `Il` : String
      - `Ilce` : String
      - `IlceKodu` : Int32?
      - `IlKodu` : Int32?
      - `KatNo` : String
      - `Koy` : String
      - `KoyKayitNo` : Int64?
      - `KoyKodu` : Int64?
      - `Mahalle` : String
      - `MahalleKodu` : Int32?
      - `TapuBagimsizBolumNo` : String
      - `YapiKullanimAmac` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `YolAltiKatSayisi` : Int32?
      - `YolUstuKatSayisi` : Int32?
    - `BeyandaBulunanKimlikNo` : Int64?
    - `BeyanTarihi` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `IlIlceMerkezAdresi` : IlIlceMerkezAdresi
      - `BagimsizBolumDurum` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `BagimsizBolumTipi` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `BinaAda` : String
      - `BinaBlokAdi` : String
      - `BinaDurum` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `BinaKodu` : Int64?
      - `BinaNo` : Int32?
      - `BinaNumaratajTipi` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `BinaPafta` : String
      - `BinaParsel` : String
      - `BinaSiteAdi` : String
      - `BinaYapiTipi` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `Csbm` : String
      - `CsbmKodu` : Int32?
      - `DisKapiNo` : String
      - `IcKapiNo` : String
      - `Il` : String
      - `Ilce` : String
      - `IlceKodu` : Int32?
      - `IlKodu` : Int32?
      - `KatNo` : String
      - `Mahalle` : String
      - `MahalleKodu` : Int32?
      - `TapuBagimsizBolumNo` : String
      - `YapiKullanimAmac` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `YolAltiKatSayisi` : Int32?
      - `YolUstuKatSayisi` : Int32?
    - `KoyAdresi` : KoyAdresi
      - `BagimsizBolumDurum` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `BagimsizBolumTipi` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `BinaAda` : String
      - `BinaBlokAdi` : String
      - `BinaDurum` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `BinaKodu` : Int64?
      - `BinaNo` : Int32?
      - `BinaNumaratajTipi` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `BinaPafta` : String
      - `BinaParsel` : String
      - `BinaSiteAdi` : String
      - `BinaYapiTipi` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `Csbm` : String
      - `CsbmKodu` : Int32?
      - `DisKapiNo` : String
      - `IcKapiNo` : String
      - `Il` : String
      - `Ilce` : String
      - `IlceKodu` : Int32?
      - `IlKodu` : Int32?
      - `KatNo` : String
      - `Koy` : String
      - `KoyKayitNo` : Int64?
      - `KoyKodu` : Int64?
      - `Mahalle` : String
      - `MahalleKodu` : Int32?
      - `TapuBagimsizBolumNo` : String
      - `YapiKullanimAmac` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `YolAltiKatSayisi` : Int32?
      - `YolUstuKatSayisi` : Int32?
    - `TasinmaTarihi` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `TescilTarihi` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `YurtDisiAdresi` : YurtDisiAdresi
      - `DisTemsDuzeltmeBeyanTarih` : DateTime?
      - `DisTemsDuzeltmeKararTarih` : DateTime?
      - `DisTemsDuzeltmeNeden` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `DisTemsilcilik` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `YabanciAdres` : String
      - `YabanciSehir` : String
      - `YabanciUlke` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String

## TcKimlikNoSorgula

```csharp
Task<KpsResult<TCKisiBilgisi>> TcKimlikNoSorgulaAsync(KisiSorgulaKisiBilgileriCO criteria)
```

**Argümanlar**

- `kriter` : KisiSorgulaKisiBilgileriCO
  - `Adi` : String
  - `Soyadi` : String
  - `BabaAdi` : String
  - `AnneAdi` : String
  - `Cinsiyet` : ECinsiyet
  - `DogumYeri` : String
  - `DogumTarihi` : DateTime

**Dönen alanlar** (`Data`)

- `AnneTCKimlikNo` : Int64?
- `BabaTCKimlikNo` : Int64?
- `DogumYerKod` : Int32?
- `DurumBilgisi` : TCKisiDurumBilgisi
  - `Din` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `Durum` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `MedeniHal` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `OlumTarih` : TarihBilgisi
    - `Yil` : Int32?
    - `Ay` : Int32?
    - `Gun` : Int32?
- `KayitYeriBilgisi` : KayitYeriBilgisi
  - `AileSiraNo` : Int32?
  - `BireySiraNo` : Int32?
  - `Cilt` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `Il` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `Ilce` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
- `TCKimlikNo` : Int64?
- `TemelBilgisi` : TemelBilgisi
  - `Ad` : String
  - `AnneAd` : String
  - `BabaAd` : String
  - `Cinsiyet` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `DogumTarih` : TarihBilgisi
    - `Yil` : Int32?
    - `Ay` : Int32?
    - `Gun` : Int32?
  - `DogumYer` : String
  - `Soyad` : String

## KimlikNoIleAdresBilgisiSorgula

```csharp
Task<KpsResult<KisiAdresBilgisi>> KimlikNoIleAdresBilgisiSorgulaAsync(long identityNumber)
```

**Argümanlar**

- `kimlikNo` : Int64

**Dönen alanlar** (`Data`)

- `AcikAdres` : String
- `AdresNo` : Int64?
- `AdresTip` : Parametre
  - `Kod` : Int32?
  - `Aciklama` : String
- `BeldeAdresi` : BeldeAdresi
  - `BagimsizBolumDurum` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `BagimsizBolumTipi` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `BinaAda` : String
  - `BinaBlokAdi` : String
  - `BinaDurum` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `BinaKodu` : Int64?
  - `BinaNo` : Int32?
  - `BinaNumaratajTipi` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `BinaPafta` : String
  - `BinaParsel` : String
  - `BinaSiteAdi` : String
  - `BinaYapiTipi` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `Csbm` : String
  - `CsbmKodu` : Int32?
  - `DisKapiNo` : String
  - `IcKapiNo` : String
  - `Il` : String
  - `Ilce` : String
  - `IlceKodu` : Int32?
  - `IlKodu` : Int32?
  - `KatNo` : String
  - `Koy` : String
  - `KoyKayitNo` : Int64?
  - `KoyKodu` : Int64?
  - `Mahalle` : String
  - `MahalleKodu` : Int32?
  - `TapuBagimsizBolumNo` : String
  - `YapiKullanimAmac` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `YolAltiKatSayisi` : Int32?
  - `YolUstuKatSayisi` : Int32?
- `BeyandaBulunanKimlikNo` : Int64?
- `BeyanTarihi` : TarihBilgisi
  - `Yil` : Int32?
  - `Ay` : Int32?
  - `Gun` : Int32?
- `IlIlceMerkezAdresi` : IlIlceMerkezAdresi
  - `BagimsizBolumDurum` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `BagimsizBolumTipi` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `BinaAda` : String
  - `BinaBlokAdi` : String
  - `BinaDurum` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `BinaKodu` : Int64?
  - `BinaNo` : Int32?
  - `BinaNumaratajTipi` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `BinaPafta` : String
  - `BinaParsel` : String
  - `BinaSiteAdi` : String
  - `BinaYapiTipi` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `Csbm` : String
  - `CsbmKodu` : Int32?
  - `DisKapiNo` : String
  - `IcKapiNo` : String
  - `Il` : String
  - `Ilce` : String
  - `IlceKodu` : Int32?
  - `IlKodu` : Int32?
  - `KatNo` : String
  - `Mahalle` : String
  - `MahalleKodu` : Int32?
  - `TapuBagimsizBolumNo` : String
  - `YapiKullanimAmac` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `YolAltiKatSayisi` : Int32?
  - `YolUstuKatSayisi` : Int32?
- `KoyAdresi` : KoyAdresi
  - `BagimsizBolumDurum` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `BagimsizBolumTipi` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `BinaAda` : String
  - `BinaBlokAdi` : String
  - `BinaDurum` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `BinaKodu` : Int64?
  - `BinaNo` : Int32?
  - `BinaNumaratajTipi` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `BinaPafta` : String
  - `BinaParsel` : String
  - `BinaSiteAdi` : String
  - `BinaYapiTipi` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `Csbm` : String
  - `CsbmKodu` : Int32?
  - `DisKapiNo` : String
  - `IcKapiNo` : String
  - `Il` : String
  - `Ilce` : String
  - `IlceKodu` : Int32?
  - `IlKodu` : Int32?
  - `KatNo` : String
  - `Koy` : String
  - `KoyKayitNo` : Int64?
  - `KoyKodu` : Int64?
  - `Mahalle` : String
  - `MahalleKodu` : Int32?
  - `TapuBagimsizBolumNo` : String
  - `YapiKullanimAmac` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `YolAltiKatSayisi` : Int32?
  - `YolUstuKatSayisi` : Int32?
- `TasinmaTarihi` : TarihBilgisi
  - `Yil` : Int32?
  - `Ay` : Int32?
  - `Gun` : Int32?
- `TescilTarihi` : TarihBilgisi
  - `Yil` : Int32?
  - `Ay` : Int32?
  - `Gun` : Int32?
- `YurtDisiAdresi` : YurtDisiAdresi
  - `DisTemsDuzeltmeBeyanTarih` : DateTime?
  - `DisTemsDuzeltmeKararTarih` : DateTime?
  - `DisTemsDuzeltmeNeden` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `DisTemsilcilik` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `YabanciAdres` : String
  - `YabanciSehir` : String
  - `YabanciUlke` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String

## KimlikNoIleYerlesimYeriSorgula

```csharp
Task<KpsResult<TcKimlikNoIleAdresBilgisi>> KimlikNoIleYerlesimYeriSorgulaAsync(long identityNumber)
```

**Argümanlar**

- `kimlikNo` : Int64

**Dönen alanlar** (`Data`)

- `TumAdresBilgileri` : KisiAdresBilgisi[]
  - `AcikAdres` : String
  - `AdresNo` : Int64?
  - `AdresTip` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `BeldeAdresi` : BeldeAdresi
    - `BagimsizBolumDurum` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BagimsizBolumTipi` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BinaAda` : String
    - `BinaBlokAdi` : String
    - `BinaDurum` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BinaKodu` : Int64?
    - `BinaNo` : Int32?
    - `BinaNumaratajTipi` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BinaPafta` : String
    - `BinaParsel` : String
    - `BinaSiteAdi` : String
    - `BinaYapiTipi` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `Csbm` : String
    - `CsbmKodu` : Int32?
    - `DisKapiNo` : String
    - `IcKapiNo` : String
    - `Il` : String
    - `Ilce` : String
    - `IlceKodu` : Int32?
    - `IlKodu` : Int32?
    - `KatNo` : String
    - `Koy` : String
    - `KoyKayitNo` : Int64?
    - `KoyKodu` : Int64?
    - `Mahalle` : String
    - `MahalleKodu` : Int32?
    - `TapuBagimsizBolumNo` : String
    - `YapiKullanimAmac` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `YolAltiKatSayisi` : Int32?
    - `YolUstuKatSayisi` : Int32?
  - `BeyandaBulunanKimlikNo` : Int64?
  - `BeyanTarihi` : TarihBilgisi
    - `Yil` : Int32?
    - `Ay` : Int32?
    - `Gun` : Int32?
  - `IlIlceMerkezAdresi` : IlIlceMerkezAdresi
    - `BagimsizBolumDurum` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BagimsizBolumTipi` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BinaAda` : String
    - `BinaBlokAdi` : String
    - `BinaDurum` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BinaKodu` : Int64?
    - `BinaNo` : Int32?
    - `BinaNumaratajTipi` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BinaPafta` : String
    - `BinaParsel` : String
    - `BinaSiteAdi` : String
    - `BinaYapiTipi` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `Csbm` : String
    - `CsbmKodu` : Int32?
    - `DisKapiNo` : String
    - `IcKapiNo` : String
    - `Il` : String
    - `Ilce` : String
    - `IlceKodu` : Int32?
    - `IlKodu` : Int32?
    - `KatNo` : String
    - `Mahalle` : String
    - `MahalleKodu` : Int32?
    - `TapuBagimsizBolumNo` : String
    - `YapiKullanimAmac` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `YolAltiKatSayisi` : Int32?
    - `YolUstuKatSayisi` : Int32?
  - `KoyAdresi` : KoyAdresi
    - `BagimsizBolumDurum` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BagimsizBolumTipi` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BinaAda` : String
    - `BinaBlokAdi` : String
    - `BinaDurum` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BinaKodu` : Int64?
    - `BinaNo` : Int32?
    - `BinaNumaratajTipi` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BinaPafta` : String
    - `BinaParsel` : String
    - `BinaSiteAdi` : String
    - `BinaYapiTipi` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `Csbm` : String
    - `CsbmKodu` : Int32?
    - `DisKapiNo` : String
    - `IcKapiNo` : String
    - `Il` : String
    - `Ilce` : String
    - `IlceKodu` : Int32?
    - `IlKodu` : Int32?
    - `KatNo` : String
    - `Koy` : String
    - `KoyKayitNo` : Int64?
    - `KoyKodu` : Int64?
    - `Mahalle` : String
    - `MahalleKodu` : Int32?
    - `TapuBagimsizBolumNo` : String
    - `YapiKullanimAmac` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `YolAltiKatSayisi` : Int32?
    - `YolUstuKatSayisi` : Int32?
  - `TasinmaTarihi` : TarihBilgisi
    - `Yil` : Int32?
    - `Ay` : Int32?
    - `Gun` : Int32?
  - `TescilTarihi` : TarihBilgisi
    - `Yil` : Int32?
    - `Ay` : Int32?
    - `Gun` : Int32?
  - `YurtDisiAdresi` : YurtDisiAdresi
    - `DisTemsDuzeltmeBeyanTarih` : DateTime?
    - `DisTemsDuzeltmeKararTarih` : DateTime?
    - `DisTemsDuzeltmeNeden` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `DisTemsilcilik` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `YabanciAdres` : String
    - `YabanciSehir` : String
    - `YabanciUlke` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
- `YerlesimYeriAdresi` : KisiAdresBilgisi
  - `AcikAdres` : String
  - `AdresNo` : Int64?
  - `AdresTip` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `BeldeAdresi` : BeldeAdresi
    - `BagimsizBolumDurum` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BagimsizBolumTipi` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BinaAda` : String
    - `BinaBlokAdi` : String
    - `BinaDurum` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BinaKodu` : Int64?
    - `BinaNo` : Int32?
    - `BinaNumaratajTipi` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BinaPafta` : String
    - `BinaParsel` : String
    - `BinaSiteAdi` : String
    - `BinaYapiTipi` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `Csbm` : String
    - `CsbmKodu` : Int32?
    - `DisKapiNo` : String
    - `IcKapiNo` : String
    - `Il` : String
    - `Ilce` : String
    - `IlceKodu` : Int32?
    - `IlKodu` : Int32?
    - `KatNo` : String
    - `Koy` : String
    - `KoyKayitNo` : Int64?
    - `KoyKodu` : Int64?
    - `Mahalle` : String
    - `MahalleKodu` : Int32?
    - `TapuBagimsizBolumNo` : String
    - `YapiKullanimAmac` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `YolAltiKatSayisi` : Int32?
    - `YolUstuKatSayisi` : Int32?
  - `BeyandaBulunanKimlikNo` : Int64?
  - `BeyanTarihi` : TarihBilgisi
    - `Yil` : Int32?
    - `Ay` : Int32?
    - `Gun` : Int32?
  - `IlIlceMerkezAdresi` : IlIlceMerkezAdresi
    - `BagimsizBolumDurum` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BagimsizBolumTipi` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BinaAda` : String
    - `BinaBlokAdi` : String
    - `BinaDurum` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BinaKodu` : Int64?
    - `BinaNo` : Int32?
    - `BinaNumaratajTipi` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BinaPafta` : String
    - `BinaParsel` : String
    - `BinaSiteAdi` : String
    - `BinaYapiTipi` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `Csbm` : String
    - `CsbmKodu` : Int32?
    - `DisKapiNo` : String
    - `IcKapiNo` : String
    - `Il` : String
    - `Ilce` : String
    - `IlceKodu` : Int32?
    - `IlKodu` : Int32?
    - `KatNo` : String
    - `Mahalle` : String
    - `MahalleKodu` : Int32?
    - `TapuBagimsizBolumNo` : String
    - `YapiKullanimAmac` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `YolAltiKatSayisi` : Int32?
    - `YolUstuKatSayisi` : Int32?
  - `KoyAdresi` : KoyAdresi
    - `BagimsizBolumDurum` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BagimsizBolumTipi` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BinaAda` : String
    - `BinaBlokAdi` : String
    - `BinaDurum` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BinaKodu` : Int64?
    - `BinaNo` : Int32?
    - `BinaNumaratajTipi` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `BinaPafta` : String
    - `BinaParsel` : String
    - `BinaSiteAdi` : String
    - `BinaYapiTipi` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `Csbm` : String
    - `CsbmKodu` : Int32?
    - `DisKapiNo` : String
    - `IcKapiNo` : String
    - `Il` : String
    - `Ilce` : String
    - `IlceKodu` : Int32?
    - `IlKodu` : Int32?
    - `KatNo` : String
    - `Koy` : String
    - `KoyKayitNo` : Int64?
    - `KoyKodu` : Int64?
    - `Mahalle` : String
    - `MahalleKodu` : Int32?
    - `TapuBagimsizBolumNo` : String
    - `YapiKullanimAmac` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `YolAltiKatSayisi` : Int32?
    - `YolUstuKatSayisi` : Int32?
  - `TasinmaTarihi` : TarihBilgisi
    - `Yil` : Int32?
    - `Ay` : Int32?
    - `Gun` : Int32?
  - `TescilTarihi` : TarihBilgisi
    - `Yil` : Int32?
    - `Ay` : Int32?
    - `Gun` : Int32?
  - `YurtDisiAdresi` : YurtDisiAdresi
    - `DisTemsDuzeltmeBeyanTarih` : DateTime?
    - `DisTemsDuzeltmeKararTarih` : DateTime?
    - `DisTemsDuzeltmeNeden` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `DisTemsilcilik` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `YabanciAdres` : String
    - `YabanciSehir` : String
    - `YabanciUlke` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String

## AdresNoIleYerlesimYeriSorgula

```csharp
Task<KpsResult<AcikAdres>> AdresNoIleYerlesimYeriSorgulaAsync(long addressNumber)
```

**Argümanlar**

- `adresNo` : Int64

**Dönen alanlar** (`Data`)

- `AdresNo` : Int64?
- `Aciklama` : String
- `Tip` : Parametre
  - `Kod` : Int32?
  - `Aciklama` : String
- `BeldeAdresi` : BeldeAdresi
  - `BagimsizBolumDurum` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `BagimsizBolumTipi` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `BinaAda` : String
  - `BinaBlokAdi` : String
  - `BinaDurum` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `BinaKodu` : Int64?
  - `BinaNo` : Int32?
  - `BinaNumaratajTipi` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `BinaPafta` : String
  - `BinaParsel` : String
  - `BinaSiteAdi` : String
  - `BinaYapiTipi` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `Csbm` : String
  - `CsbmKodu` : Int32?
  - `DisKapiNo` : String
  - `IcKapiNo` : String
  - `Il` : String
  - `Ilce` : String
  - `IlceKodu` : Int32?
  - `IlKodu` : Int32?
  - `KatNo` : String
  - `Koy` : String
  - `KoyKayitNo` : Int64?
  - `KoyKodu` : Int64?
  - `Mahalle` : String
  - `MahalleKodu` : Int32?
  - `TapuBagimsizBolumNo` : String
  - `YapiKullanimAmac` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `YolAltiKatSayisi` : Int32?
  - `YolUstuKatSayisi` : Int32?
- `IlIlceMerkeziAdresi` : IlIlceMerkezAdresi
  - `BagimsizBolumDurum` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `BagimsizBolumTipi` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `BinaAda` : String
  - `BinaBlokAdi` : String
  - `BinaDurum` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `BinaKodu` : Int64?
  - `BinaNo` : Int32?
  - `BinaNumaratajTipi` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `BinaPafta` : String
  - `BinaParsel` : String
  - `BinaSiteAdi` : String
  - `BinaYapiTipi` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `Csbm` : String
  - `CsbmKodu` : Int32?
  - `DisKapiNo` : String
  - `IcKapiNo` : String
  - `Il` : String
  - `Ilce` : String
  - `IlceKodu` : Int32?
  - `IlKodu` : Int32?
  - `KatNo` : String
  - `Mahalle` : String
  - `MahalleKodu` : Int32?
  - `TapuBagimsizBolumNo` : String
  - `YapiKullanimAmac` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `YolAltiKatSayisi` : Int32?
  - `YolUstuKatSayisi` : Int32?
- `KoyAdresi` : KoyAdresi
  - `BagimsizBolumDurum` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `BagimsizBolumTipi` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `BinaAda` : String
  - `BinaBlokAdi` : String
  - `BinaDurum` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `BinaKodu` : Int64?
  - `BinaNo` : Int32?
  - `BinaNumaratajTipi` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `BinaPafta` : String
  - `BinaParsel` : String
  - `BinaSiteAdi` : String
  - `BinaYapiTipi` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `Csbm` : String
  - `CsbmKodu` : Int32?
  - `DisKapiNo` : String
  - `IcKapiNo` : String
  - `Il` : String
  - `Ilce` : String
  - `IlceKodu` : Int32?
  - `IlKodu` : Int32?
  - `KatNo` : String
  - `Koy` : String
  - `KoyKayitNo` : Int64?
  - `KoyKodu` : Int64?
  - `Mahalle` : String
  - `MahalleKodu` : Int32?
  - `TapuBagimsizBolumNo` : String
  - `YapiKullanimAmac` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `YolAltiKatSayisi` : Int32?
  - `YolUstuKatSayisi` : Int32?

## AileBireyleriSorgula

```csharp
Task<KpsResult<AileBilgisi>> AileBireyleriSorgulaAsync(AileBireyleriSorgulaKriter criteria)
```

**Argümanlar**

- `kriter` : AileBireyleriSorgulaKriter
  - `TCKimlikNo` : Int64
  - `Tip` : EAileSorguTipi

**Dönen alanlar** (`Data`)

- `TCKimlikNo` : Int64?
- `KisiBilgisi` : TCKisiBilgisi
  - `AnneTCKimlikNo` : Int64?
  - `BabaTCKimlikNo` : Int64?
  - `DogumYerKod` : Int32?
  - `DurumBilgisi` : TCKisiDurumBilgisi
    - `Din` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `Durum` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `MedeniHal` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `OlumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
  - `EsTCKimlikNo` : Int64?
  - `KayitYeriBilgisi` : KayitYeriBilgisi
    - `AileSiraNo` : Int32?
    - `BireySiraNo` : Int32?
    - `Cilt` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `Il` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `Ilce` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
  - `TCKimlikNo` : Int64?
  - `TemelBilgisi` : TemelBilgisi
    - `Ad` : String
    - `AnneAd` : String
    - `BabaAd` : String
    - `Cinsiyet` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `DogumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DogumYer` : String
    - `Soyad` : String
- `AileKisiListesi` : KisiBilgisi[]
  - `DurumBilgisi` : TCKisiDurumBilgisi
    - `Din` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `Durum` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `MedeniHal` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `OlumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
  - `KayitYeriBilgisi` : KayitYeriBilgisi
    - `AileSiraNo` : Int32?
    - `BireySiraNo` : Int32?
    - `Cilt` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `Il` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `Ilce` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
  - `TCKimlikNo` : Int64?
  - `TemelBilgisi` : TemelBilgisi
    - `Ad` : String
    - `AnneAd` : String
    - `BabaAd` : String
    - `Cinsiyet` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `DogumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DogumYer` : String
    - `Soyad` : String
  - `Yakinlik` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String

## KimlikNoIleNufusKayitOrnegiSorgula

```csharp
Task<KpsResult<GenelNufusKayitOrnegi>> KimlikNoIleNufusKayitOrnegiSorgulaAsync(long identityNumber)
```

**Argümanlar**

- `kimlikNo` : Int64

**Dönen alanlar** (`Data`)

- `NkoVatandas` : Nko
  - `NkoTuru` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `Kisiler` : KisiBilgisi[]
    - `DurumBilgisi` : TCKisiDurumBilgisi
      - `Din` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `Durum` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `MedeniHal` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `OlumTarih` : TarihBilgisi
        - `Yil` : Int32?
        - `Ay` : Int32?
        - `Gun` : Int32?
    - `KayitYeriBilgisi` : KayitYeriBilgisi
      - `AileSiraNo` : Int32?
      - `BireySiraNo` : Int32?
      - `Cilt` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `Il` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `Ilce` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
    - `TCKimlikNo` : Int64?
    - `TemelBilgisi` : TemelBilgisi
      - `Ad` : String
      - `AnneAd` : String
      - `BabaAd` : String
      - `Cinsiyet` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `DogumTarih` : TarihBilgisi
        - `Yil` : Int32?
        - `Ay` : Int32?
        - `Gun` : Int32?
      - `DogumYer` : String
      - `Soyad` : String
    - `Yakinlik` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
  - `Olaylar` : NkoOlay[]
    - `Ad` : String
    - `Dusunce` : String
    - `KayitYeri` : KisiKayitYeriKodBilgisi
      - `AileSiraNo` : Int32?
      - `BireySiraNo` : Int32?
      - `CiltKod` : Int32?
      - `IlceKod` : Int32?
    - `OlayKayitNo` : Int64?
    - `OlayTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `OlayTipi` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `Soyad` : String
- `NkoMaviKart` : NkoMaviKart
  - `NkoTuru` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
  - `Kisiler` : NkoMaviKartKisi[]
    - `AnneKimlikNo` : Int64?
    - `BabaKimlikNo` : Int64?
    - `BosanmaTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DurumBilgisi` : NkoMaviKartKisiDurumBilgisi
      - `Durum` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `MedeniHal` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `OlumTarih` : TarihBilgisi
        - `Yil` : Int32?
        - `Ay` : Int32?
        - `Gun` : Int32?
    - `EvlenmeTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `KimlikNo` : Int64?
    - `TemelBilgisi` : TemelBilgisi
      - `Ad` : String
      - `AnneAd` : String
      - `BabaAd` : String
      - `Cinsiyet` : Parametre
        - `Kod` : Int32?
        - `Aciklama` : String
      - `DogumTarih` : TarihBilgisi
        - `Yil` : Int32?
        - `Ay` : Int32?
        - `Gun` : Int32?
      - `DogumYer` : String
      - `Soyad` : String
    - `TescilTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `Ulke` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `YakinlikKod` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
  - `Olaylar` : NkoMaviKartOlay[]
    - `Ad` : String
    - `Dusunce` : String
    - `KisiKimlikNo` : Int64?
    - `OlayTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `OlayTipi` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `Soyad` : String

## KimlikNoIleVatandasAileBilgisiSorgula

```csharp
Task<KpsResult<YakinlikServisiAileBilgisi>> KimlikNoIleVatandasAileBilgisiSorgulaAsync(YakinlikSorgulaKriter criteria)
```

**Argümanlar**

- `kriter` : YakinlikSorgulaKriter
  - `KimlikNo` : Int64
  - `DogumGun` : Int32
  - `DogumAy` : Int32
  - `DogumYil` : Int32

**Dönen alanlar** (`Data`)

- `TCKimlikNo` : Int64?
- `KisiBilgisi` : YakinlikServisiKisiBilgisi
  - `DurumBilgisi` : YakinlikServisiKisiDurumBilgisi
    - `Durum` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `MedeniHal` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `OlumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
  - `DogumYerKod` : Int32?
  - `TCKimlikNo` : Int64?
  - `AnneTCKimlikNo` : Int64?
  - `BabaTCKimlikNo` : Int64?
  - `TemelBilgisi` : YakinlikServisiKisiTemelBilgisi
    - `Ad` : String
    - `AnneAd` : String
    - `BabaAd` : String
    - `Cinsiyet` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `DogumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DogumYer` : String
    - `Soyad` : String
- `AileKisiListesi` : YakinlikServisiAileKisiBilgisi[]
  - `DurumBilgisi` : YakinlikServisiKisiDurumBilgisi
    - `Durum` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `MedeniHal` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `OlumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
  - `TCKimlikNo` : Int64?
  - `TemelBilgisi` : YakinlikServisiKisiTemelBilgisi
    - `Ad` : String
    - `AnneAd` : String
    - `BabaAd` : String
    - `Cinsiyet` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `DogumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DogumYer` : String
    - `Soyad` : String
  - `Yakinlik` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String

## KimlikNoIleMaviKartliAileBilgisiSorgula

```csharp
Task<KpsResult<MaviKartliAileBilgisi>> KimlikNoIleMaviKartliAileBilgisiSorgulaAsync(YakinlikSorgulaKriter criteria)
```

**Argümanlar**

- `kriter` : YakinlikSorgulaKriter
  - `KimlikNo` : Int64
  - `DogumGun` : Int32
  - `DogumAy` : Int32
  - `DogumYil` : Int32

**Dönen alanlar** (`Data`)

- `MaviKartliAileKisiListesi` : MaviKartliAileKisiBilgisi[]
  - `DurumBilgisi` : MaviKartliKisiDurumBilgisi
    - `Durum` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `MedeniHal` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
  - `KimlikNo` : Int64?
  - `TemelBilgisi` : MaviKartliKisiTemelBilgisi
    - `Ad` : String
    - `AnneAd` : String
    - `BabaAd` : String
    - `Cinsiyet` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `DogumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DogumYer` : String
    - `Soyad` : String
  - `Yakinlik` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
- `KimlikNo` : Int64?

## KimlikNoIleVatandasAileBilgisiKardesDahilSorgula

```csharp
Task<KpsResult<YakinlikServisiAileBilgisi>> KimlikNoIleVatandasAileBilgisiKardesDahilSorgulaAsync(YakinlikSorgulaKriter criteria)
```

**Argümanlar**

- `kriter` : YakinlikSorgulaKriter
  - `KimlikNo` : Int64
  - `DogumGun` : Int32
  - `DogumAy` : Int32
  - `DogumYil` : Int32

**Dönen alanlar** (`Data`)

- `TCKimlikNo` : Int64?
- `KisiBilgisi` : YakinlikServisiKisiBilgisi
  - `DurumBilgisi` : YakinlikServisiKisiDurumBilgisi
    - `Durum` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `MedeniHal` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `OlumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
  - `DogumYerKod` : Int32?
  - `TCKimlikNo` : Int64?
  - `AnneTCKimlikNo` : Int64?
  - `BabaTCKimlikNo` : Int64?
  - `TemelBilgisi` : YakinlikServisiKisiTemelBilgisi
    - `Ad` : String
    - `AnneAd` : String
    - `BabaAd` : String
    - `Cinsiyet` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `DogumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DogumYer` : String
    - `Soyad` : String
- `AileKisiListesi` : YakinlikServisiAileKisiBilgisi[]
  - `DurumBilgisi` : YakinlikServisiKisiDurumBilgisi
    - `Durum` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `MedeniHal` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `OlumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
  - `TCKimlikNo` : Int64?
  - `TemelBilgisi` : YakinlikServisiKisiTemelBilgisi
    - `Ad` : String
    - `AnneAd` : String
    - `BabaAd` : String
    - `Cinsiyet` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `DogumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DogumYer` : String
    - `Soyad` : String
  - `Yakinlik` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String

## KimlikNoIleMaviKartliAileBilgisiKardesDahilSorgula

```csharp
Task<KpsResult<MaviKartliAileBilgisi>> KimlikNoIleMaviKartliAileBilgisiKardesDahilSorgulaAsync(YakinlikSorgulaKriter criteria)
```

**Argümanlar**

- `kriter` : YakinlikSorgulaKriter
  - `KimlikNo` : Int64
  - `DogumGun` : Int32
  - `DogumAy` : Int32
  - `DogumYil` : Int32

**Dönen alanlar** (`Data`)

- `MaviKartliAileKisiListesi` : MaviKartliAileKisiBilgisi[]
  - `DurumBilgisi` : MaviKartliKisiDurumBilgisi
    - `Durum` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `MedeniHal` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
  - `KimlikNo` : Int64?
  - `TemelBilgisi` : MaviKartliKisiTemelBilgisi
    - `Ad` : String
    - `AnneAd` : String
    - `BabaAd` : String
    - `Cinsiyet` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `DogumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DogumYer` : String
    - `Soyad` : String
  - `Yakinlik` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String
- `KimlikNo` : Int64?

## KimlikNoIleVatandasAileYakinlikBilgisiSorgula

```csharp
Task<KpsResult<KamuYakinlikServisiAileBilgisi>> KimlikNoIleVatandasAileYakinlikBilgisiSorgulaAsync(KamuYakinlikSorgulaKriter criteria)
```

**Argümanlar**

- `kriter` : KamuYakinlikSorgulaKriter
  - `KimlikNo` : Int64
  - `DogumGun` : Int32
  - `DogumAy` : Int32
  - `DogumYil` : Int32

**Dönen alanlar** (`Data`)

- `TCKimlikNo` : Int64?
- `KisiBilgisi` : KamuYakinlikServisiKisiBilgisi
  - `DurumBilgisi` : KamuYakinlikServisiKisiDurumBilgisi
    - `Durum` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `MedeniHal` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `OlumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `BosanmaTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `EvlenmeTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
  - `DogumYerKod` : Int32?
  - `TCKimlikNo` : Int64?
  - `AnneTCKimlikNo` : Int64?
  - `BabaTCKimlikNo` : Int64?
  - `TemelBilgisi` : KamuYakinlikServisiKisiTemelBilgisi
    - `Ad` : String
    - `AnneAd` : String
    - `BabaAd` : String
    - `Cinsiyet` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `DogumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DogumYer` : String
    - `Soyad` : String
- `YakinlikListesi` : KamuYakinlikServisiAileKisiBilgisi[]
  - `DurumBilgisi` : KamuYakinlikServisiKisiDurumBilgisi
    - `Durum` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `MedeniHal` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `OlumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `BosanmaTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `EvlenmeTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
  - `TCKimlikNo` : Int64?
  - `TemelBilgisi` : KamuYakinlikServisiKisiTemelBilgisi
    - `Ad` : String
    - `AnneAd` : String
    - `BabaAd` : String
    - `Cinsiyet` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `DogumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DogumYer` : String
    - `Soyad` : String
  - `Yakinlik` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String

## KimlikNoIleVatandasAileYakinlikBilgisiKardesDahilSorgula

```csharp
Task<KpsResult<KamuYakinlikServisiAileBilgisi>> KimlikNoIleVatandasAileYakinlikBilgisiKardesDahilSorgulaAsync(KamuYakinlikSorgulaKriter criteria)
```

**Argümanlar**

- `kriter` : KamuYakinlikSorgulaKriter
  - `KimlikNo` : Int64
  - `DogumGun` : Int32
  - `DogumAy` : Int32
  - `DogumYil` : Int32

**Dönen alanlar** (`Data`)

- `TCKimlikNo` : Int64?
- `KisiBilgisi` : KamuYakinlikServisiKisiBilgisi
  - `DurumBilgisi` : KamuYakinlikServisiKisiDurumBilgisi
    - `Durum` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `MedeniHal` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `OlumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `BosanmaTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `EvlenmeTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
  - `DogumYerKod` : Int32?
  - `TCKimlikNo` : Int64?
  - `AnneTCKimlikNo` : Int64?
  - `BabaTCKimlikNo` : Int64?
  - `TemelBilgisi` : KamuYakinlikServisiKisiTemelBilgisi
    - `Ad` : String
    - `AnneAd` : String
    - `BabaAd` : String
    - `Cinsiyet` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `DogumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DogumYer` : String
    - `Soyad` : String
- `YakinlikListesi` : KamuYakinlikServisiAileKisiBilgisi[]
  - `DurumBilgisi` : KamuYakinlikServisiKisiDurumBilgisi
    - `Durum` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `MedeniHal` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `OlumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `BosanmaTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `EvlenmeTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
  - `TCKimlikNo` : Int64?
  - `TemelBilgisi` : KamuYakinlikServisiKisiTemelBilgisi
    - `Ad` : String
    - `AnneAd` : String
    - `BabaAd` : String
    - `Cinsiyet` : Parametre
      - `Kod` : Int32?
      - `Aciklama` : String
    - `DogumTarih` : TarihBilgisi
      - `Yil` : Int32?
      - `Ay` : Int32?
      - `Gun` : Int32?
    - `DogumYer` : String
    - `Soyad` : String
  - `Yakinlik` : Parametre
    - `Kod` : Int32?
    - `Aciklama` : String

## YetkiListesi

```csharp
Task<KpsResult<WcfService[]>> YetkiListesiAsync()
```

**Argümanlar**

- (yok)

**Dönen alanlar** (`Data`)

- `Name` : String
- `Methods` : Method[]
  - `Name` : String
  - `MethodId` : String
  - `Argumans` : List<Property>
  - `ReturnProperties` : List<Property>
  - `Description` : String
  - `IsAuthorized` : Boolean?
- `Description` : String
- `IsAuthorized` : Boolean?

## Parametre Servisi

```csharp
Task<KpsResult<string[]>> ParametreAdListesiAsync()
Task<KpsResult<Parametre[]>> ParametreDegerleriAsync(string parameterName)
```

`ParametreAdListesi` tüm parametre adlarını (ör. kod listeleri) döner; `ParametreDegerleri` verilen parametrenin `Kod`/`Aciklama` değerlerini döner. Test kullanıcılarının bu servise yetkisi yoktur.

## NVİ KPS

NVİ Kimlik Paylaşımı Sistemi'nin servisleri (`INviKpsClient`, ayarlar ve kurulum: [kullanim.md § 8](kullanim.md#8-nvi-kps)). Kaynak: NVİ'nin herkese açık WSDL'leri, `https://kpsbasvuru.nvi.gov.tr/Services/Wsdl.ashx?Service=<servis adı>`. Tipler `KpsV2.Nvi.Contracts` namespace'indedir. Her servisin tek işlemi `ListeleCoklu`'dur: bir kriter listesi alır, kişi başına bir sonuç döner.

| NVİ servisi | C# | Dönüş (`Data`) |
|---|---|---|
| [TCKimlikNoDogrulaServis](#tckimliknodogrulaservis) | `TCKimlikNoDogrulaAsync(long identityNumber, string firstName, string lastName, int birthYear)` | `TCKimlikNoDogrulama` |
| | `TCKimlikNoDogrulaAsync(IEnumerable<TCKimlikNoDogrulaSorguKriteri> criteria)` | `TCKimlikNoDogrulama[]` |
| [KisiSorgulaTCKimlikNoServis](#kisisorgulatckimliknoservis) | `KisiSorgulaTCKimlikNoAsync(long identityNumber)` | `KisiBilgisi` |
| | `KisiSorgulaTCKimlikNoAsync(IEnumerable<long> identityNumbers)` | `KisiBilgisi[]` |

Tek kişilik sürümlerde kişinin kendi `HataBilgisi`'si `Error`'a taşınır; liste sürümlerinde her elemanın `HataBilgisi`'sine bakın (`InnerErrors` hepsini yollarıyla verir).

### TCKimlikNoDogrulaServis

Kriter (`TCKimlikNoDogrulaSorguKriteri`): `TCKimlikNo`, `Ad`, `Soyad`, `DogumYil`.

```
TCKimlikNoDogrulama
├── TCKimlikNo
├── Ad
├── Soyad
├── DogumYil
└── HataBilgisi { Kod, Aciklama }
```

### KisiSorgulaTCKimlikNoServis

Kriter (`KisiSorgulaTCKimlikNoSorguKriteri`): `TCKimlikNo`.

```
KisiBilgisi
├── TCKimlikNo, AnneTCKimlikNo, BabaTCKimlikNo, EsTCKimlikNo
├── DogumYerKod
├── TemelBilgisi
│   ├── Ad, Soyad, AnneAd, BabaAd
│   ├── Cinsiyet { Kod, Aciklama }
│   ├── DogumTarih { Gun, Ay, Yil }  → .Date (DateOnly?)
│   └── DogumYer
├── DurumBilgisi
│   ├── Durum { Kod, Aciklama }
│   ├── MedeniHal { Kod, Aciklama }
│   ├── Din { Kod, Aciklama }
│   └── OlumTarih { Gun, Ay, Yil }
├── KayitYeriBilgisi
│   ├── Il, Ilce, Cilt { Kod, Aciklama }
│   └── AileSiraNo, BireySiraNo
└── HataBilgisi { Kod, Aciklama }
```

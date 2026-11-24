using System.Net;
using System.Xml.Linq;
using KpsV2.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KpsV2.Client.Tests;

public class KpsClientTests
{
    private const long TcNo = 10774881040;   // test ortamı örnek listesinden

    private const string TemelBilgisi =
        "<TemelBilgisi><Ad>AHMET</Ad><AnneAd>AYŞE</AnneAd><BabaAd>MEHMET</BabaAd>" +
        "<Cinsiyet><Aciklama>Erkek</Aciklama><Kod>1</Kod></Cinsiyet>" +
        "<DogumTarih><Ay>3</Ay><Gun>15</Gun><Yil>1980</Yil></DogumTarih><DogumYer>ANKARA</DogumYer><Soyad>YILMAZ</Soyad></TemelBilgisi>";

    private static string TcKisiSonucu() => SahteKps.Sonuc("BilesikKisiSorgula",
        "<HataBilgisi i:nil=\"true\"/>" +
        "<Sonuc><HataBilgisi i:nil=\"true\"/><KimlikNo>10774881040</KimlikNo><MaviKartliKisiKutukleri i:nil=\"true\"/>" +
        "<TCVatandasiKisiKutukleri><GeciciKimlikBilgisi i:nil=\"true\"/><HataBilgisi i:nil=\"true\"/><KisiBilgisi>" +
        "<AnneTCKimlikNo>26563360832</AnneTCKimlikNo><BabaTCKimlikNo i:nil=\"true\"/><DogumYerKod>1</DogumYerKod>" +
        "<DurumBilgisi><Din i:nil=\"true\"/><Durum><Aciklama>Açık</Aciklama><Kod>1</Kod></Durum>" +
        "<MedeniHal><Aciklama>Evli</Aciklama><Kod>2</Kod></MedeniHal><OlumTarih i:nil=\"true\"/></DurumBilgisi>" +
        "<HataBilgisi i:nil=\"true\"/>" +
        "<KayitYeriBilgisi><AileSiraNo>12</AileSiraNo><BireySiraNo>3</BireySiraNo><Cilt><Aciklama>MERKEZ</Aciklama><Kod>5</Kod></Cilt>" +
        "<Il><Aciklama>ADANA</Aciklama><Kod>1</Kod></Il><Ilce><Aciklama>SEYHAN</Aciklama><Kod>1104</Kod></Ilce></KayitYeriBilgisi>" +
        "<OncekiYabanciKimlikNo i:nil=\"true\"/><TCKimlikNo>10774881040</TCKimlikNo>" + TemelBilgisi +
        "<VeliVasiTur i:nil=\"true\"/><VeliVasiVar>false</VeliVasiVar>" +
        "</KisiBilgisi><NufusCuzdaniBilgisi i:nil=\"true\"/><TCKKBilgisi i:nil=\"true\"/></TCVatandasiKisiKutukleri>" +
        "<YabanciKisiKutukleri i:nil=\"true\"/></Sonuc>");

    // ---------------- token ----------------

    [Fact]
    public async Task Token_onbellekte_tutulur_ve_birebir_eklenir()
    {
        var h = new SahteKps();
        var kps = SahteKps.Istemci(h);

        await kps.BilesikKisiSorgulaAsync(TcNo);
        await kps.BilesikKisiSorgulaAsync(TcNo);

        Assert.Equal(1, h.StsSayisi);
        Assert.Equal(2, h.KpsSayisi);
        Assert.All(h.Istekler.Where(i => i.Url.Contains("/Router/")), i => Assert.Contains(h.TokenXml, i.Govde));
    }

    [Fact]
    public async Task Es_zamanli_isteklerde_tek_token_alinir()
    {
        var h = new SahteKps { StsGecikmesi = TimeSpan.FromMilliseconds(100) };
        var kps = SahteKps.Istemci(h);

        await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => kps.BilesikKisiSorgulaAsync(TcNo)));

        Assert.Equal(1, h.StsSayisi);
        Assert.Equal(20, h.KpsSayisi);
    }

    [Fact]
    public async Task Suresi_dolmak_uzere_olan_token_yenilenir()
    {
        var h = new SahteKps { TokenBitis = DateTimeOffset.UtcNow.AddSeconds(30) };
        var kps = SahteKps.Istemci(h); // yenileme payı 1 dk > 30 sn

        await kps.BilesikKisiSorgulaAsync(TcNo);
        await kps.BilesikKisiSorgulaAsync(TcNo);

        Assert.Equal(2, h.StsSayisi);
    }

    [Fact]
    public async Task KPS_tokeni_reddederse_bir_kez_yeni_token_ile_denenir()
    {
        var h = new SahteKps();
        h.SiradakiCevaplar.Enqueue((HttpStatusCode.InternalServerError, SahteKps.Fault("An error occurred when verifying security for the message.")));

        var s = await SahteKps.Istemci(h).YetkiListesiAsync();

        Assert.Equal(2, h.StsSayisi);
        Assert.Equal(2, h.KpsSayisi);
        Assert.Null(s.Error);
    }

    [Fact]
    public async Task Standart_kullanici_kurum_ve_uygulama_kodu_gonderir()
    {
        var h = new SahteKps();
        await SahteKps.Istemci(h, o => o.Username = "a<b").YetkiListesiAsync();

        var sts = XDocument.Parse(h.Istekler.First(i => i.Url.Contains("/STS/")).Govde);
        Assert.Equal("123456", sts.Descendants("KurumKodu").Single().Value);
        Assert.Equal("8353df93-453c-4e23-8be8-2f913dd35313", sts.Descendants("UygulamaKodu").Single().Value);
        Assert.Empty(sts.Descendants("SorgulayanKimlikNo"));
        XNamespace wsse = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
        Assert.Equal("a<b", sts.Descendants(wsse + "Username").Single().Value); // XML kaçışı yapılmış
    }

    [Fact]
    public async Task Uygulama_kullanicisi_sorgulayan_kimlik_no_gonderir()
    {
        var h = new SahteKps();
        await SahteKps.Istemci(h, o => { o.InstitutionCode = null; o.ApplicationCode = null; o.RequesterIdentityNumber = "26563360832"; })
            .YetkiListesiAsync();

        var sts = XDocument.Parse(h.Istekler.First(i => i.Url.Contains("/STS/")).Govde);
        Assert.Equal("26563360832", sts.Descendants("SorgulayanKimlikNo").Single().Value);
        Assert.Empty(sts.Descendants("KurumKodu"));
    }

    [Fact]
    public async Task STS_hatasi_KimlikDogrulama_olarak_firlatilir_ve_tekrar_denenmez()
    {
        var h = new SahteKps { StsHatasi = (HttpStatusCode.InternalServerError, SahteKps.Fault("Kullanıcı adı veya şifre yanlış.")) };

        var ex = await Assert.ThrowsAsync<KpsException>(() => SahteKps.Istemci(h).YetkiListesiAsync());

        Assert.Equal(KpsErrorKind.Authentication, ex.Kind);
        Assert.Equal("Kullanıcı adı veya şifre yanlış.", ex.SoapFault);
        Assert.False(ex.IsTransient);
        Assert.Equal(1, h.StsSayisi);
        Assert.Equal(0, h.KpsSayisi);
    }

    // ---------------- istek ----------------

    [Fact]
    public async Task Prod_ortami_prod_adreslerini_kullanir()
    {
        var h = new SahteKps();
        await SahteKps.Istemci(h, o => o.Environment = KpsEnvironment.Prod).YetkiListesiAsync();

        Assert.All(h.Istekler, i => Assert.StartsWith("https://kpsv2.saglik.gov.tr/", i.Url));
        Assert.Contains("<a:To s:mustUnderstand=\"1\">https://kpsv2.saglik.gov.tr/Router/RoutingService.svc</a:To>", h.SonKpsGovdesi);
    }

    [Fact]
    public async Task Gecersiz_kimlik_no_istek_atmadan_reddedilir()
    {
        var h = new SahteKps();
        var kps = SahteKps.Istemci(h);

        await Assert.ThrowsAsync<ArgumentException>(() => kps.BilesikKisiSorgulaAsync(12345678901));
        await Assert.ThrowsAsync<ArgumentException>(() => kps.KimlikNoIleVatandasAileBilgisiSorgulaAsync(KpsCriteria.Kinship(12345678901, new DateOnly(2000, 1, 1))));
        await Assert.ThrowsAsync<ArgumentException>(() => kps.AileBireyleriSorgulaAsync(KpsCriteria.FamilyMembers(12345678901)));
        Assert.Empty(h.Istekler);
    }

    [Fact]
    public async Task TcKimlikNoSorgula_tarihi_saat_dilimsiz_gonderir()
    {
        var h = new SahteKps();
        var kriter = KpsCriteria.Person("A", "B", "C", "D", ECinsiyet.KADIN, new DateOnly(1990, 12, 31), "E");
        kriter.DogumTarihi = new DateTime(1990, 12, 31, 23, 0, 0, DateTimeKind.Local); // çağıranın hatası bile olsa

        await SahteKps.Istemci(h).TcKimlikNoSorgulaAsync(kriter);

        var k = XDocument.Parse(h.SonKpsGovdesi).Descendants(XName.Get("kriter", SahteKps.Ns)).Single();
        Assert.Equal("1990-12-31T00:00:00", k.Element(XName.Get("DogumTarihi", SahteKps.Ns))!.Value);
        Assert.Equal("KADIN", k.Element(XName.Get("Cinsiyet", SahteKps.Ns))!.Value);
    }

    // ---------------- cevap ----------------

    [Fact]
    public async Task TC_vatandasi_cevabi_tipli_okunur_ve_ozetlenir()
    {
        var h = new SahteKps { KpsCevabi = _ => (HttpStatusCode.OK, TcKisiSonucu()) };
        var s = await SahteKps.Istemci(h).BilesikKisiSorgulaAsync(TcNo);

        Assert.True(s.IsSuccess);
        Assert.Empty(s.InnerErrors);
        var kisi = s.Data!.TCVatandasiKisiKutukleri.KisiBilgisi;
        Assert.Equal("SEYHAN", kisi.KayitYeriBilgisi.Ilce.Aciklama);   // özet dışı alanlar da tipli
        Assert.Equal(12, kisi.KayitYeriBilgisi.AileSiraNo);
        Assert.False(kisi.VeliVasiVar);

        var k = s.Data.ToSummary()!;
        Assert.Equal(PersonType.TurkishCitizen, k.Type);
        Assert.Equal(TcNo, k.IdentityNumber);
        Assert.Equal("AHMET YILMAZ", k.FullName);
        Assert.Equal("MEHMET", k.FatherName);
        Assert.Equal("AYŞE", k.MotherName);
        Assert.Equal(26563360832, k.MotherIdentityNumber);
        Assert.Null(k.FatherIdentityNumber);
        Assert.Equal("Erkek", k.Gender!.Aciklama);
        Assert.Equal(new DateOnly(1980, 3, 15), k.BirthDate!.Date);
        Assert.Equal("15.03.1980", k.BirthDate.ToString());
        Assert.Equal("ANKARA", k.BirthPlace);
        Assert.Equal("Evli", k.MaritalStatus!.ToString());
        Assert.Null(k.DeathDate);
        Assert.False(k.IsDeceased);
        Assert.Same(kisi, k.Source);
    }

    [Fact]
    public async Task Yabanci_cevabi_ozetlenir()
    {
        var cevap = SahteKps.Sonuc("BilesikKisiSorgula",
            "<HataBilgisi i:nil=\"true\"/><Sonuc><HataBilgisi i:nil=\"true\"/><KimlikNo>99169250708</KimlikNo>" +
            "<MaviKartliKisiKutukleri i:nil=\"true\"/><TCVatandasiKisiKutukleri i:nil=\"true\"/>" +
            "<YabanciKisiKutukleri><HataBilgisi i:nil=\"true\"/><KisiBilgisi>" +
            "<AnneKimlikNo i:nil=\"true\"/><BabaKimlikNo i:nil=\"true\"/><BitisTarihiBelirsizOlmaNeden i:nil=\"true\"/>" +
            "<DogumTarih i:nil=\"true\"/><DogumYerKod i:nil=\"true\"/>" +
            "<DurumBilgisi><Durum><Aciklama>Ölüm</Aciklama><Kod>3</Kod></Durum><MedeniHal i:nil=\"true\"/></DurumBilgisi>" +
            "<GercekKisiKimlikNo i:nil=\"true\"/><HataBilgisi i:nil=\"true\"/>" +
            "<IzinBaslangicTarih>2019-05-01T00:00:00</IzinBaslangicTarih><IzinBitisTarih i:nil=\"true\"/><IzinDuzenlenenIl i:nil=\"true\"/>" +
            "<IzinNo>IZ-1</IzinNo><KaynakBirim i:nil=\"true\"/><KazanilanTCKimlikNo i:nil=\"true\"/>" +
            "<KimlikNo>99169250708</KimlikNo><OlumTarih><Ay>1</Ay><Gun>2</Gun><Yil>2020</Yil></OlumTarih>" +
            "<Statu i:nil=\"true\"/>" + TemelBilgisi +
            "<Uyruk><Aciklama>ALMANYA</Aciklama><Kod>49</Kod></Uyruk><VeliVasiTur i:nil=\"true\"/><VeliVasiVar i:nil=\"true\"/>" +
            "</KisiBilgisi></YabanciKisiKutukleri></Sonuc>");
        var h = new SahteKps { KpsCevabi = _ => (HttpStatusCode.OK, cevap) };

        var s = await SahteKps.Istemci(h).BilesikKisiSorgulaAsync(99169250708);
        var k = s.Data.ToSummary()!;

        Assert.Equal(PersonType.Foreigner, k.Type);
        Assert.Equal(99169250708, k.IdentityNumber);
        Assert.Equal("ALMANYA", k.Nationality!.Aciklama);
        Assert.Equal(new DateOnly(2020, 1, 2), k.DeathDate!.Date);
        Assert.True(k.IsDeceased);
        Assert.Equal("IZ-1", s.Data!.YabanciKisiKutukleri.KisiBilgisi.IzinNo);
        Assert.Equal(new DateTime(2019, 5, 1), s.Data.YabanciKisiKutukleri.KisiBilgisi.IzinBaslangicTarih);
    }

    [Fact]
    public async Task Kisi_bulunamazsa_ozet_null_doner()
    {
        var cevap = SahteKps.Sonuc("BilesikKisiSorgula",
            "<HataBilgisi i:nil=\"true\"/><Sonuc><HataBilgisi i:nil=\"true\"/><KimlikNo>10000000146</KimlikNo>" +
            "<MaviKartliKisiKutukleri i:nil=\"true\"/><TCVatandasiKisiKutukleri i:nil=\"true\"/><YabanciKisiKutukleri i:nil=\"true\"/></Sonuc>");
        var h = new SahteKps { KpsCevabi = _ => (HttpStatusCode.OK, cevap) };

        var s = await SahteKps.Istemci(h).BilesikKisiSorgulaAsync(10000000146);

        Assert.Null(s.Error);
        Assert.Null(s.Data.ToSummary());
    }

    [Fact]
    public async Task Sonuc_seviyesindeki_hata_Hata_alaninda_doner_ve_VeriyiAl_firlatir()
    {
        var cevap = SahteKps.Sonuc("TcKimlikNoSorgula",
            "<HataBilgisi i:nil=\"true\"/><Sonuc><AnneTCKimlikNo i:nil=\"true\"/><BabaTCKimlikNo i:nil=\"true\"/><DogumYerKod i:nil=\"true\"/>" +
            "<DurumBilgisi i:nil=\"true\"/><HataBilgisi><Aciklama>Aradığınız kriterlere uygun kayıt bulunamadı.</Aciklama><Kod>2</Kod></HataBilgisi>" +
            "<KayitYeriBilgisi i:nil=\"true\"/><OncekiYabanciKimlikNo i:nil=\"true\"/><TCKimlikNo i:nil=\"true\"/><TemelBilgisi i:nil=\"true\"/>" +
            "<VeliVasiTur i:nil=\"true\"/><VeliVasiVar i:nil=\"true\"/></Sonuc>");
        var h = new SahteKps { KpsCevabi = _ => (HttpStatusCode.OK, cevap) };

        var s = await SahteKps.Istemci(h).TcKimlikNoSorgulaAsync(
            KpsCriteria.Person("A", "B", "C", "D", ECinsiyet.KADIN, new DateOnly(1990, 1, 1), "E"));

        Assert.False(s.IsSuccess);
        Assert.Equal(new KpsError(2, "Aradığınız kriterlere uygun kayıt bulunamadı."), s.Error);
        Assert.Null(s.Data.ToSummary());
        var ex = Assert.Throws<KpsException>(() => s.GetDataOrThrow());
        Assert.Equal(KpsErrorKind.NotFound, ex.Kind);
    }

    [Fact]
    public async Task Ic_dugumlerdeki_hatalar_yollariyla_listelenir()
    {
        var cevap = SahteKps.Sonuc("KimlikNoIleYerlesimYeriSorgula",
            "<HataBilgisi i:nil=\"true\"/><Sonuc><HataBilgisi i:nil=\"true\"/><TumAdresBilgileri><KisiAdresBilgisi>" +
            "<HataBilgisi><Aciklama>KayitBulunamadi</Aciklama><Kod>2</Kod></HataBilgisi></KisiAdresBilgisi></TumAdresBilgileri>" +
            "<YerlesimYeriAdresi><AcikAdres>st.45 KÖLN / ALMANYA</AcikAdres><AdresNo>0</AdresNo>" +
            "<AdresTip><Aciklama>Yurt Dışı Adresi</Aciklama><Kod>4</Kod></AdresTip><HataBilgisi i:nil=\"true\"/></YerlesimYeriAdresi></Sonuc>");
        var h = new SahteKps { KpsCevabi = _ => (HttpStatusCode.OK, cevap) };

        var s = await SahteKps.Istemci(h).KimlikNoIleYerlesimYeriSorgulaAsync(14233775406);

        Assert.True(s.IsSuccess);
        Assert.Equal("st.45 KÖLN / ALMANYA", s.Data!.YerlesimYeriAdresi.AcikAdres);
        Assert.Equal("Yurt Dışı Adresi", s.Data.YerlesimYeriAdresi.AdresTip.Aciklama);
        var ic = Assert.Single(s.InnerErrors);
        Assert.Equal("Sonuc/TumAdresBilgileri/KisiAdresBilgisi", ic.Path);
        Assert.True(ic.Error.IsNotFound);
    }

    [Fact]
    public async Task Yetki_listesi_duz_listeye_cevrilir()
    {
        var cevap = SahteKps.Sonuc("YetkiListesi",
            "<HataBilgisi i:nil=\"true\"/><Sonuc><WcfService><Description i:nil=\"true\"/><IsAuthorized>false</IsAuthorized><Methods>" +
            "<Method><Argumans i:nil=\"true\"/><Description i:nil=\"true\"/><IsAuthorized>true</IsAuthorized><MethodId i:nil=\"true\"/><Name>BilesikKisiSorgula</Name></Method>" +
            "<Method><Argumans i:nil=\"true\"/><Description i:nil=\"true\"/><IsAuthorized>false</IsAuthorized><MethodId i:nil=\"true\"/><Name>KimlikNoIleAdresBilgisiSorgula</Name></Method>" +
            "</Methods><Name>Kps Servisleri</Name></WcfService></Sonuc>");
        var h = new SahteKps { KpsCevabi = _ => (HttpStatusCode.OK, cevap) };

        var s = await SahteKps.Istemci(h).YetkiListesiAsync();

        Assert.Equal(new[]
        {
            new KpsPermission("Kps Servisleri", "BilesikKisiSorgula", true),
            new KpsPermission("Kps Servisleri", "KimlikNoIleAdresBilgisiSorgula", false),
        }, s.Data.MethodPermissions());
        Assert.True(s.Data.IsAuthorized("BilesikKisiSorgula"));
        Assert.False(s.Data.IsAuthorized("KimlikNoIleAdresBilgisiSorgula"));
    }

    [Fact]
    public async Task Semaya_uymayan_cevap_CevapOkunamadi_firlatir()
    {
        var h = new SahteKps { KpsCevabi = _ => (HttpStatusCode.OK, SahteKps.Zarf("<Baska/>")) };

        var ex = await Assert.ThrowsAsync<KpsException>(() => SahteKps.Istemci(h).YetkiListesiAsync());

        Assert.Equal(KpsErrorKind.InvalidResponse, ex.Kind);
    }

    // ---------------- hatalar ve yeniden deneme ----------------

    [Fact]
    public async Task Yetki_hatasi_Yetkisiz_olarak_firlatilir_ve_tekrar_denenmez()
    {
        var h = new SahteKps { KpsCevabi = _ => (HttpStatusCode.InternalServerError, SahteKps.Fault("Bu işlem için yetkiniz bulunmamaktadır.")) };

        var ex = await Assert.ThrowsAsync<KpsException>(() => SahteKps.Istemci(h).KimlikNoIleAdresBilgisiSorgulaAsync(TcNo));

        Assert.Equal(KpsErrorKind.Unauthorized, ex.Kind);
        Assert.Equal(HttpStatusCode.InternalServerError, ex.HttpStatusCode);
        Assert.Equal("Bu işlem için yetkiniz bulunmamaktadır.", ex.SoapFault);
        Assert.Equal(1, h.KpsSayisi);
    }

    [Fact]
    public async Task KPS_arka_servis_zaman_asimi_tekrar_denenir()
    {
        var h = new SahteKps();
        var zamanAsimi = SahteKps.Fault("The request channel timed out while waiting for a reply after 00:00:06.");
        h.SiradakiCevaplar.Enqueue((HttpStatusCode.InternalServerError, zamanAsimi));
        h.SiradakiCevaplar.Enqueue((HttpStatusCode.InternalServerError, zamanAsimi));

        var s = await SahteKps.Istemci(h).YetkiListesiAsync(); // varsayılan 2 ek deneme

        Assert.Equal(3, h.KpsSayisi);
        Assert.Null(s.Error);
    }

    [Fact]
    public async Task Deneme_hakki_bitince_son_hata_firlatilir()
    {
        var h = new SahteKps { KpsCevabi = _ => (HttpStatusCode.InternalServerError, SahteKps.Fault("The server was unable to process the request due to an internal error.")) };

        var ex = await Assert.ThrowsAsync<KpsException>(() => SahteKps.Istemci(h, o => o.RetryCount = 1).YetkiListesiAsync());

        Assert.Equal(KpsErrorKind.Server, ex.Kind);
        Assert.True(ex.IsTransient);
        Assert.Equal(2, h.KpsSayisi);
    }

    [Theory]
    [InlineData(500, "Bu işlem için yetkiniz bulunmamaktadır.", false, KpsErrorKind.Unauthorized)]
    [InlineData(500, "The request channel timed out while waiting for a reply after 00:00:06.", false, KpsErrorKind.Timeout)]
    [InlineData(500, "An error occurred while receiving the HTTP response to https://x/.", false, KpsErrorKind.Timeout)]
    [InlineData(500, "An error occurred when verifying security for the message.", false, KpsErrorKind.Authentication)]
    [InlineData(500, "Kullanıcı adı veya şifre yanlış.", true, KpsErrorKind.Authentication)]
    [InlineData(500, "Uygulama bulunamadı. Lütfen UygulamaKodu'nu kontrol ediniz.", true, KpsErrorKind.Authentication)]
    [InlineData(500, "The server was unable to process the request due to an internal error.", false, KpsErrorKind.Server)]
    [InlineData(503, null, false, KpsErrorKind.Server)]
    [InlineData(404, null, false, KpsErrorKind.Unknown)]
    public void Hatalar_siniflandirilir(int kod, string? fault, bool sts, KpsErrorKind beklenen) =>
        Assert.Equal(beklenen, KpsClient.Classify((HttpStatusCode)kod, fault, sts));

    // ---------------- ayarlar ve DI ----------------

    [Fact]
    public void Eksik_ayar_anlasilir_hata_verir()
    {
        var ex = Assert.Throws<KpsConfigurationException>(() =>
            new KpsClient(new HttpClient(), new KpsOptions { Environment = KpsEnvironment.Prod, Username = "1", Password = "x" }));
        Assert.Contains("InstitutionCode", ex.Message);
        Assert.Contains("ApplicationCode", ex.Message);
        Assert.Equal(KpsErrorKind.Configuration, ex.Kind);
    }

    [Fact]
    public void DI_ayarlari_appsettings_bolumunden_okur_ve_singleton_kaydeder()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Kps:Environment"] = "Prod",
            ["Kps:Username"] = "11111111110",
            ["Kps:Password"] = "s",
            ["Kps:InstitutionCode"] = "1",
            ["Kps:ApplicationCode"] = "g",
            ["Kps:Timeout"] = "00:00:20",
            ["Kps:RetryCount"] = "4",
        }).Build();

        var sp = new ServiceCollection().AddKpsClient(config.GetSection(KpsOptions.SectionName)).Services.BuildServiceProvider();

        var kps = sp.GetRequiredService<IKpsClient>();
        Assert.Same(kps, sp.GetRequiredService<IKpsClient>());
        Assert.Equal(KpsEnvironment.Prod, ((KpsClient)kps).Environment);
        Assert.Equal(TimeSpan.FromSeconds(20), sp.GetRequiredService<IHttpClientFactory>().CreateClient(KpsClient.HttpClientName).Timeout);
    }

    // ---------------- kimlik no ----------------

    [Theory]
    [InlineData("10774881040", true)]   // TC (test listesi)
    [InlineData("99169250708", true)]   // Yabancı (test listesi)
    [InlineData("14233775406", true)]   // Mavi kart (test listesi)
    [InlineData("10774881041", false)]
    [InlineData("01234567890", false)]
    [InlineData("1077488104", false)]
    [InlineData("1077488104a", false)]
    [InlineData(null, false)]
    public void Kimlik_no_kontrol_haneleri(string? no, bool gecerli) =>
        Assert.Equal(gecerli, IdentityNumberValidator.IsValid(no));
}

using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using KpsV2.Nvi;
using KpsV2.Nvi.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KpsV2.Client.Tests;

/// <summary>
/// NVİ KPS istemcisi. NVİ'nin herkese açık test ortamı olmadığı için cevaplar WSDL şemasına göre kurulmuştur
/// (kpsbasvuru.nvi.gov.tr/Services/Wsdl.ashx); gerçek cevap biçimi ilk Prod çağrısında ProdCheck ile doğrulanmalıdır.
/// </summary>
public class NviKpsClientTests
{
    private const long TcNo = 10774881040;

    // ---------------- STS ----------------

    [Fact]
    public async Task STS_istegi_kullanici_adi_sifre_ve_simetrik_anahtar_ister()
    {
        var h = new SahteNvi();
        await SahteNvi.Istemci(h, o => o.Password = "p<&>ss").KisiSorgulaTCKimlikNoAsync(TcNo);

        var sts = h.Istekler.Single(i => i.Url == NviKpsOptions.DefaultStsAddress).Govde;
        var doc = XDocument.Parse(sts);
        XNamespace wsse = SahteNvi.WsseNs, trust = SahteNvi.TrustNs, addr = "http://www.w3.org/2005/08/addressing";
        Assert.Equal("kullanici", doc.Descendants(wsse + "Username").Single().Value);
        Assert.Equal("p<&>ss", doc.Descendants(wsse + "Password").Single().Value);
        Assert.Equal(SahteNvi.TrustNs + "/RST/Issue", doc.Descendants(addr + "Action").Single().Value);
        Assert.Equal(SahteNvi.TrustNs + "/SymmetricKey", doc.Descendants(trust + "KeyType").Single().Value);
        Assert.Equal("http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.1#SAMLV1.1", doc.Descendants(trust + "TokenType").Single().Value);
        Assert.Equal(NviKpsOptions.DefaultKpsAddress, doc.Descendants(addr + "EndpointReference").Single().Element(addr + "Address")!.Value);
    }

    [Fact]
    public async Task Token_onbellekte_tutulur_ve_birebir_eklenir()
    {
        var h = new SahteNvi();
        var nvi = SahteNvi.Istemci(h);

        await nvi.KisiSorgulaTCKimlikNoAsync(TcNo);
        await nvi.TCKimlikNoDogrulaAsync(TcNo, "AHMET", "YILMAZ", 1980);

        Assert.Equal(1, h.StsSayisi);
        Assert.Equal(2, h.KpsSayisi);
        Assert.All(h.KpsIstekleri, g => Assert.Contains(SahteNvi.Token, g));
    }

    [Fact]
    public async Task Token_suresi_Lifetime_alanindan_okunur()
    {
        var h = new SahteNvi { TokenBitis = DateTimeOffset.UtcNow.AddSeconds(30) }; // yenileme payı 1 dk > 30 sn
        var nvi = SahteNvi.Istemci(h);

        await nvi.KisiSorgulaTCKimlikNoAsync(TcNo);
        await nvi.KisiSorgulaTCKimlikNoAsync(TcNo);

        Assert.Equal(2, h.StsSayisi);
    }

    [Fact]
    public async Task STS_hatasi_Authentication_olarak_firlatilir()
    {
        var h = new SahteNvi { StsHatasi = (HttpStatusCode.InternalServerError, SahteKps.Fault("ID3242: The security token could not be authenticated or authorized.")) };

        var ex = await Assert.ThrowsAsync<KpsException>(() => SahteNvi.Istemci(h).KisiSorgulaTCKimlikNoAsync(TcNo));

        Assert.Equal(KpsErrorKind.Authentication, ex.Kind);
        Assert.Equal(0, h.KpsSayisi);
    }

    [Fact]
    public async Task STS_cevabinda_anahtar_yoksa_InvalidResponse()
    {
        var h = new SahteNvi { AnahtarGonder = false };

        var ex = await Assert.ThrowsAsync<KpsException>(() => SahteNvi.Istemci(h).KisiSorgulaTCKimlikNoAsync(TcNo));

        Assert.Equal(KpsErrorKind.InvalidResponse, ex.Kind);
        Assert.Contains("BinarySecret", ex.Message);
    }

    [Fact]
    public async Task Sorgu_servisi_tokeni_reddederse_bir_kez_yeni_token_ile_denenir()
    {
        var h = new SahteNvi();
        h.SiradakiCevaplar.Enqueue((HttpStatusCode.InternalServerError, SahteKps.Fault("An error occurred when verifying security for the message.")));

        var s = await SahteNvi.Istemci(h).KisiSorgulaTCKimlikNoAsync(TcNo);

        Assert.Null(s.Error);
        Assert.Equal(2, h.StsSayisi);
        Assert.Equal(2, h.KpsSayisi);
    }

    // ---------------- imza ----------------

    [Fact]
    public async Task Zaman_damgasi_token_anahtariyla_HMAC_SHA1_imzalanir_ve_dogrulanir()
    {
        var h = new SahteNvi();
        await SahteNvi.Istemci(h).KisiSorgulaTCKimlikNoAsync(TcNo);

        // .NET'in XML imza doğrulayıcısı, zarftaki imzayı exc-c14n ile kendisi kanonikleştirip doğrular.
        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.LoadXml(h.KpsIstekleri.Single());
        var imza = (XmlElement)doc.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl)[0]!;
        var signed = new WsuIdSignedXml(doc);
        signed.LoadXml(imza);

        Assert.True(signed.CheckSignature(new HMACSHA1(SahteNvi.Anahtar)));
        Assert.False(signed.CheckSignature(new HMACSHA1(new byte[32])));
        Assert.Equal("#_0", ((Reference)signed.SignedInfo!.References[0]!).Uri);
        Assert.Equal(SahteNvi.AssertionId, imza.GetElementsByTagName("KeyIdentifier", SahteNvi.WsseNs)[0]!.InnerText);
    }

    [Fact]
    public void Zaman_damgasi_ve_SignedInfo_exc_c14n_ciktisiyla_birebir_aynidir()
    {
        var ts = NviKpsClient.ZamanDamgasi("2026-01-01T00:00:00.000Z", "2026-01-01T00:05:00.000Z");
        Assert.Equal(ts, ExcC14n($"<s:Envelope xmlns:s=\"x\" xmlns:u=\"{SahteNvi.WsuNs}\"><s:Header>{ts}</s:Header></s:Envelope>", "Timestamp"));

        var si = NviKpsClient.SignedInfo("QUJD");
        Assert.Equal(si, ExcC14n($"<Signature xmlns=\"{SignedXml.XmlDsigNamespaceUrl}\">{si}</Signature>", "SignedInfo"));
    }

    private static string ExcC14n(string xml, string eleman)
    {
        var doc = new XmlDocument { PreserveWhitespace = true };
        doc.LoadXml(xml);
        var e = doc.SelectSingleNode($"//*[local-name()='{eleman}']")!;
        var t = new XmlDsigExcC14NTransform();
        t.LoadInput(e.SelectNodes("descendant-or-self::node() | descendant-or-self::*/@* | descendant-or-self::*/namespace::*")!);
        using var s = (Stream)t.GetOutput(typeof(Stream));
        return new StreamReader(s, Encoding.UTF8).ReadToEnd();
    }

    private sealed class WsuIdSignedXml(XmlDocument doc) : SignedXml(doc)
    {
        public override XmlElement? GetIdElement(XmlDocument? document, string idValue) =>
            document?.SelectSingleNode($"//*[@*[local-name()='Id' and namespace-uri()='{SahteNvi.WsuNs}']='{idValue}']") as XmlElement;
    }

    // ---------------- TCKimlikNoDogrula ----------------

    [Fact]
    public async Task TCKimlikNoDogrula_istegi_WSDL_kriterine_uyar()
    {
        var h = new SahteNvi();
        await SahteNvi.Istemci(h).TCKimlikNoDogrulaAsync(TcNo, "AHMET", "YILMAZ", 1980);

        var g = h.KpsIstekleri.Single();
        Assert.Contains($"<a:Action s:mustUnderstand=\"1\">{NviKpsClient.NviNs}/TCKimlikNoDogrulaServis/ListeleCoklu</a:Action>", g);
        Assert.Contains($"<a:To s:mustUnderstand=\"1\">{NviKpsOptions.DefaultKpsAddress}</a:To>", g);
        var kriter = XDocument.Parse(g).Descendants(XName.Get("TCKimlikNoDogrulaSorguKriteri", NviKpsClient.NviNs)).Single();
        Assert.Equal(XName.Get("kriterListesi", NviKpsClient.NviNs), kriter.Parent!.Name);
        Assert.Equal(XName.Get("ListeleCoklu", NviKpsClient.NviNs), kriter.Parent.Parent!.Name);
        Assert.Equal(["Ad", "DogumYil", "Soyad", "TCKimlikNo"], kriter.Elements().Select(e => e.Name.LocalName)); // WSDL sırası
        Assert.Equal(["AHMET", "1980", "YILMAZ", "10774881040"], kriter.Elements().Select(e => e.Value));
    }

    [Fact]
    public async Task TCKimlikNoDogrula_eslesirse_basarili()
    {
        var h = new SahteNvi { KpsCevabi = _ => SahteNvi.Sonuc(
            "<HataBilgisi i:nil=\"true\"/><SorguSonucu><TCKimlikNoDogrulama><Ad>AHMET</Ad><DogumYil>1980</DogumYil>" +
            "<HataBilgisi i:nil=\"true\"/><Soyad>YILMAZ</Soyad><TCKimlikNo>10774881040</TCKimlikNo></TCKimlikNoDogrulama></SorguSonucu>") };

        var s = await SahteNvi.Istemci(h).TCKimlikNoDogrulaAsync(TcNo, "AHMET", "YILMAZ", 1980);

        Assert.True(s.IsSuccess);
        Assert.Equal("TCKimlikNoDogrulaServis", s.Method);
        Assert.Equal(TcNo, s.Data!.TCKimlikNo);
        Assert.Equal(1980, s.Data.DogumYil);
        Assert.Empty(s.InnerErrors);
    }

    [Fact]
    public async Task TCKimlikNoDogrula_eslesmezse_kisinin_HataBilgisi_Error_olur()
    {
        var h = new SahteNvi { KpsCevabi = _ => SahteNvi.Sonuc(
            "<HataBilgisi i:nil=\"true\"/><SorguSonucu><TCKimlikNoDogrulama><Ad i:nil=\"true\"/><DogumYil i:nil=\"true\"/>" +
            "<HataBilgisi><Aciklama>Kayıt bulunamadı.</Aciklama><Kod>2</Kod></HataBilgisi>" +
            "<Soyad i:nil=\"true\"/><TCKimlikNo>10774881040</TCKimlikNo></TCKimlikNoDogrulama></SorguSonucu>") };

        var s = await SahteNvi.Istemci(h).TCKimlikNoDogrulaAsync(TcNo, "AHMET", "YANLIS", 1980);

        Assert.False(s.IsSuccess);
        Assert.Equal(new KpsError(2, "Kayıt bulunamadı."), s.Error);
        Assert.Equal("SorguSonucu/TCKimlikNoDogrulama", Assert.Single(s.InnerErrors).Path);
        Assert.Equal(KpsErrorKind.NotFound, Assert.Throws<KpsException>(() => s.GetDataOrThrow()).Kind);
    }

    [Fact]
    public async Task Ust_seviye_HataBilgisi_Error_olur()
    {
        var h = new SahteNvi { KpsCevabi = _ => SahteNvi.Sonuc(
            "<HataBilgisi><Aciklama>Sorgu kriteri hatalı.</Aciklama><Kod>5</Kod></HataBilgisi><SorguSonucu i:nil=\"true\"/>") };

        var s = await SahteNvi.Istemci(h).TCKimlikNoDogrulaAsync(TcNo, "AHMET", "YILMAZ", 1980);

        Assert.Null(s.Data);
        Assert.Equal(new KpsError(5, "Sorgu kriteri hatalı."), s.Error);
    }

    [Fact]
    public async Task Toplu_dogrulamada_her_kisi_ayri_doner()
    {
        var h = new SahteNvi();
        var s = await SahteNvi.Istemci(h).TCKimlikNoDogrulaAsync(new[]
        {
            new TCKimlikNoDogrulaSorguKriteri { TCKimlikNo = TcNo, Ad = "AHMET", Soyad = "YILMAZ", DogumYil = 1980 },
            new TCKimlikNoDogrulaSorguKriteri { TCKimlikNo = 26563360832, Ad = "AYŞE", Soyad = "YILMAZ", DogumYil = 1976 },
        });

        Assert.Equal(2, XDocument.Parse(h.KpsIstekleri.Single()).Descendants(XName.Get("TCKimlikNoDogrulaSorguKriteri", NviKpsClient.NviNs)).Count());
        Assert.Null(s.Data); // sahte cevap boş
    }

    [Theory]
    [InlineData(12345678901, "AHMET", "YILMAZ", 1980)] // kontrol hanesi tutmuyor
    [InlineData(TcNo, "", "YILMAZ", 1980)]
    [InlineData(TcNo, "AHMET", " ", 1980)]
    [InlineData(TcNo, "AHMET", "YILMAZ", 80)]
    public async Task Gecersiz_kriterde_istek_gitmez(long no, string ad, string soyad, int yil)
    {
        var h = new SahteNvi();
        await Assert.ThrowsAsync<ArgumentException>(() => SahteNvi.Istemci(h).TCKimlikNoDogrulaAsync(no, ad, soyad, yil));
        await Assert.ThrowsAsync<ArgumentException>(() => SahteNvi.Istemci(h).TCKimlikNoDogrulaAsync(Array.Empty<TCKimlikNoDogrulaSorguKriteri>()));
        Assert.Empty(h.Istekler);
    }

    // ---------------- KisiSorgulaTCKimlikNo ----------------

    [Fact]
    public async Task KisiSorgulaTCKimlikNo_tum_alanlari_okur()
    {
        var h = new SahteNvi { KpsCevabi = _ => SahteNvi.Sonuc(
            "<HataBilgisi i:nil=\"true\"/><SorguSonucu><KisiBilgisi>" +
            "<AnneTCKimlikNo>26563360832</AnneTCKimlikNo><BabaTCKimlikNo i:nil=\"true\"/><DogumYerKod>1</DogumYerKod>" +
            "<DurumBilgisi><Din i:nil=\"true\"/><Durum><Aciklama>Açık</Aciklama><Kod>1</Kod></Durum>" +
            "<MedeniHal><Aciklama>Evli</Aciklama><Kod>2</Kod></MedeniHal><OlumTarih i:nil=\"true\"/></DurumBilgisi>" +
            "<EsTCKimlikNo i:nil=\"true\"/><HataBilgisi i:nil=\"true\"/>" +
            "<KayitYeriBilgisi><AileSiraNo>12</AileSiraNo><BireySiraNo>3</BireySiraNo><Cilt><Aciklama>MERKEZ</Aciklama><Kod>5</Kod></Cilt>" +
            "<Il><Aciklama>ADANA</Aciklama><Kod>1</Kod></Il><Ilce><Aciklama>SEYHAN</Aciklama><Kod>1104</Kod></Ilce></KayitYeriBilgisi>" +
            "<TCKimlikNo>10774881040</TCKimlikNo>" +
            "<TemelBilgisi><Ad>AHMET</Ad><AnneAd>AYŞE</AnneAd><BabaAd>MEHMET</BabaAd><Cinsiyet><Aciklama>Erkek</Aciklama><Kod>1</Kod></Cinsiyet>" +
            "<DogumTarih><Ay>3</Ay><Gun>15</Gun><Yil>1980</Yil></DogumTarih><DogumYer>ANKARA</DogumYer><Soyad>YILMAZ</Soyad></TemelBilgisi>" +
            "</KisiBilgisi></SorguSonucu>") };

        var s = await SahteNvi.Istemci(h).KisiSorgulaTCKimlikNoAsync(TcNo);

        Assert.True(s.IsSuccess);
        var k = s.Data!;
        Assert.Equal(TcNo, k.TCKimlikNo);
        Assert.Equal(26563360832, k.AnneTCKimlikNo);
        Assert.Equal("AHMET", k.TemelBilgisi.Ad);
        Assert.Equal("Erkek", k.TemelBilgisi.Cinsiyet.ToString());
        Assert.Equal(new DateOnly(1980, 3, 15), k.TemelBilgisi.DogumTarih.Date);
        Assert.Equal("15.03.1980", k.TemelBilgisi.DogumTarih.ToString());
        Assert.Equal("Evli", k.DurumBilgisi.MedeniHal.Aciklama);
        Assert.Null(k.DurumBilgisi.OlumTarih);
        Assert.Equal("SEYHAN", k.KayitYeriBilgisi.Ilce.Aciklama);
        Assert.Equal(12, k.KayitYeriBilgisi.AileSiraNo);

        var g = h.KpsIstekleri.Single();
        Assert.Contains($"{NviKpsClient.NviNs}/KisiSorgulaTCKimlikNoServis/ListeleCoklu", g);
        Assert.Equal("10774881040", XDocument.Parse(g).Descendants(XName.Get("KisiSorgulaTCKimlikNoSorguKriteri", NviKpsClient.NviNs)).Single().Value);
    }

    // ---------------- hata, ayar, DI ----------------

    [Fact]
    public async Task Gecici_hatada_yeniden_denenir()
    {
        var h = new SahteNvi();
        h.SiradakiCevaplar.Enqueue((HttpStatusCode.InternalServerError, SahteKps.Fault("The request channel timed out")));

        var s = await SahteNvi.Istemci(h).KisiSorgulaTCKimlikNoAsync(TcNo);

        Assert.Equal(2, h.KpsSayisi);
        Assert.Null(s.Error);
    }

    [Fact]
    public async Task Yetkisiz_servis_Unauthorized_firlatir()
    {
        var h = new SahteNvi { KpsCevabi = _ => (HttpStatusCode.InternalServerError, SahteKps.Fault("Bu servisi kullanma yetkiniz bulunmamaktadır.")) };

        var ex = await Assert.ThrowsAsync<KpsException>(() => SahteNvi.Istemci(h).KisiSorgulaTCKimlikNoAsync(TcNo));

        Assert.Equal(KpsErrorKind.Unauthorized, ex.Kind);
        Assert.Equal(1, h.KpsSayisi); // yeniden denenmez
    }

    [Fact]
    public void Eksik_ayar_Configuration_hatasi_verir()
    {
        var ex = Assert.Throws<KpsConfigurationException>(() => new NviKpsClient(new HttpClient(), new NviKpsOptions()));
        Assert.Contains("Username", ex.Message);
        Assert.Contains("Password", ex.Message);
    }

    [Fact]
    public void DI_ile_iki_KPS_birlikte_kaydedilir()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Kps:Username"] = "test_user", ["Kps:Password"] = "x", ["Kps:InstitutionCode"] = "1", ["Kps:ApplicationCode"] = "g",
            ["NviKps:Username"] = "nvi", ["NviKps:Password"] = "y", ["NviKps:Timeout"] = "00:00:15",
        }).Build();
        var services = new ServiceCollection();
        services.AddKpsClient(config.GetSection("Kps"));
        services.AddNviKpsClient(config.GetSection(NviKpsOptions.SectionName));
        using var sp = services.BuildServiceProvider();

        Assert.IsType<KpsClient>(sp.GetRequiredService<IKpsClient>());
        Assert.IsType<NviKpsClient>(sp.GetRequiredService<INviKpsClient>());
        Assert.Same(sp.GetRequiredService<INviKpsClient>(), sp.GetRequiredService<INviKpsClient>());
        Assert.Equal(TimeSpan.FromSeconds(15), sp.GetRequiredService<IHttpClientFactory>().CreateClient(NviKpsClient.HttpClientName).Timeout);
    }
}

/// <summary>NVİ STS'ini ve sorgu servisini taklit eden HTTP handler; gelen istekleri kaydeder.</summary>
internal sealed class SahteNvi : HttpMessageHandler
{
    public const string WsseNs = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
    public const string WsuNs = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
    public const string TrustNs = "http://docs.oasis-open.org/ws-sx/ws-trust/200512";
    public const string AssertionId = "_9f1c2d3e-test";
    public static readonly byte[] Anahtar = Enumerable.Range(1, 32).Select(i => (byte)i).ToArray();

    /// <summary>Şifreli SAML token (NVİ token'ı şifreli döndürür); birebir taşınmalı.</summary>
    public const string Token =
        "<xenc:EncryptedData xmlns:xenc=\"http://www.w3.org/2001/04/xmlenc#\"  Id=\"_enc\" Type=\"http://www.w3.org/2001/04/xmlenc#Element\">\n" +
        "<xenc:CipherData><xenc:CipherValue>QUJDREVG</xenc:CipherValue></xenc:CipherData></xenc:EncryptedData>";

    public ConcurrentQueue<(string Url, string Govde)> Istekler { get; } = new();
    public int StsSayisi => Istekler.Count(i => i.Url == NviKpsOptions.DefaultStsAddress);
    public int KpsSayisi => Istekler.Count(i => i.Url == NviKpsOptions.DefaultKpsAddress);
    public IEnumerable<string> KpsIstekleri => Istekler.Where(i => i.Url == NviKpsOptions.DefaultKpsAddress).Select(i => i.Govde);

    public DateTimeOffset TokenBitis { get; set; } = DateTimeOffset.UtcNow.AddHours(1);
    public bool AnahtarGonder { get; set; } = true;
    public (HttpStatusCode, string)? StsHatasi { get; set; }
    public Func<string, (HttpStatusCode, string)> KpsCevabi { get; set; } = _ => Sonuc("<HataBilgisi i:nil=\"true\"/><SorguSonucu i:nil=\"true\"/>");
    public ConcurrentQueue<(HttpStatusCode, string)> SiradakiCevaplar { get; } = new();

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var url = request.RequestUri!.ToString();
        var govde = await request.Content!.ReadAsStringAsync(ct);
        Istekler.Enqueue((url, govde));

        if (url == NviKpsOptions.DefaultStsAddress)
            return StsHatasi is { } h ? Cevap(h.Item1, h.Item2) : Cevap(HttpStatusCode.OK, StsCevabi());
        var (kod, icerik) = SiradakiCevaplar.TryDequeue(out var sirada) ? sirada : KpsCevabi(govde);
        return Cevap(kod, icerik);
    }

    private string StsCevabi()
    {
        var referans = $"<o:SecurityTokenReference xmlns:o=\"{WsseNs}\"><o:KeyIdentifier " +
                       "ValueType=\"http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.0#SAMLAssertionID\">" +
                       AssertionId + "</o:KeyIdentifier></o:SecurityTokenReference>";
        return
            $"<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\" xmlns:u=\"{WsuNs}\"><s:Body>" +
            $"<trust:RequestSecurityTokenResponseCollection xmlns:trust=\"{TrustNs}\"><trust:RequestSecurityTokenResponse>" +
            $"<trust:Lifetime><u:Created>2026-01-01T00:00:00Z</u:Created><u:Expires>{TokenBitis.UtcDateTime:yyyy-MM-ddTHH:mm:ss.fffZ}</u:Expires></trust:Lifetime>" +
            "<trust:RequestedSecurityToken>" + Token + "</trust:RequestedSecurityToken>" +
            "<trust:RequestedAttachedReference>" + referans + "</trust:RequestedAttachedReference>" +
            "<trust:RequestedUnattachedReference>" + referans.Replace(AssertionId, "_dis") + "</trust:RequestedUnattachedReference>" +
            (AnahtarGonder ? $"<trust:RequestedProofToken><trust:BinarySecret>{Convert.ToBase64String(Anahtar)}</trust:BinarySecret></trust:RequestedProofToken>" : "") +
            "</trust:RequestSecurityTokenResponse></trust:RequestSecurityTokenResponseCollection></s:Body></s:Envelope>";
    }

    /// <summary>&lt;ListeleCokluResponse&gt;&lt;ListeleCokluResult&gt;...</summary>
    public static (HttpStatusCode, string) Sonuc(string ic) => (HttpStatusCode.OK, SahteKps.Zarf(
        $"<ListeleCokluResponse xmlns=\"{NviKpsClient.NviNs}\"><ListeleCokluResult xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">" +
        ic + "</ListeleCokluResult></ListeleCokluResponse>"));

    private static HttpResponseMessage Cevap(HttpStatusCode kod, string icerik) =>
        new(kod) { Content = new StringContent(icerik, Encoding.UTF8, "application/soap+xml") };

    public static NviKpsClient Istemci(SahteNvi handler, Action<NviKpsOptions>? ayar = null)
    {
        var o = new NviKpsOptions { Username = "kullanici", Password = "sifre", RetryDelay = TimeSpan.Zero };
        ayar?.Invoke(o);
        return new NviKpsClient(new HttpClient(handler), o);
    }
}

using System.Collections.Concurrent;
using System.Globalization;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using KpsV2.Nvi.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using static KpsV2.SoapTransport;

namespace KpsV2.Nvi;

/// <summary>
/// NVİ KPS istemcisi. NVİ'nin STS'inden simetrik anahtarlı SAML token alır (WS-Trust 1.3 / UsernameToken),
/// her istekte WS-Security zaman damgasını token'ın anahtarıyla HMAC-SHA1 olarak imzalar (WSDL politikası:
/// TransportBinding + EndorsingSupportingTokens/IssuedToken, SymmetricKey) ve sorgu servisine SOAP 1.2 istekleri atar.
/// Thread-safe'tir; uygulama boyunca tek örnek kullanın.
/// </summary>
public sealed partial class NviKpsClient : INviKpsClient
{
    /// <summary>NVİ KPS veri sözleşmelerinin namespace'i.</summary>
    public const string NviNs = "http://kps.nvi.gov.tr/2011/01/01";

    /// <summary>DI'da kullanılan adlandırılmış HttpClient.</summary>
    public const string HttpClientName = "KpsV2.Nvi";

    /// <summary>Paketin tipli olarak sunduğu NVİ servisleri.</summary>
    public static class Services
    {
        /// <summary>TC kimlik no + ad + soyad + doğum yılı doğrulaması.</summary>
        public const string TCKimlikNoDogrula = "TCKimlikNoDogrulaServis";

        /// <summary>TC kimlik no ile kişi bilgisi.</summary>
        public const string KisiSorgulaTCKimlikNo = "KisiSorgulaTCKimlikNoServis";
    }

    private const string ListeleCoklu = "ListeleCoklu";
    private const string SamlV11 = "http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.1#SAMLV1.1";
    private const string DsigNs = "http://www.w3.org/2000/09/xmldsig#";
    private const string ExcC14n = "http://www.w3.org/2001/10/xml-exc-c14n#";
    private const string BaglantiNotu = " NVİ KPS sorgu servisine yalnızca başvuruda tanımlı IP'lerden erişilebilir.";

    private static readonly ConcurrentDictionary<(Type Tip, string Kok), DataContractSerializer> Serilestiriciler = new();

    private readonly Func<HttpClient> _http;
    private readonly NviKpsOptions _o;
    private readonly ILogger _logger;
    private readonly string _stsUrl;
    private readonly string _kpsUrl;
    private readonly TokenCache<NviToken> _token;

    /// <summary>STS'den alınan token: ham XML (birebir eklenir), imza anahtarı ve token'a başvuru kimliği.</summary>
    internal sealed record NviToken(string Xml, byte[] Key, string AssertionId);

    /// <summary>DI dışında kullanım: HttpClient'ın ömrünü çağıran yönetir.</summary>
    public NviKpsClient(HttpClient httpClient, NviKpsOptions options, ILogger<NviKpsClient>? logger = null)
        : this(() => httpClient, options, logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
    }

    /// <summary>DI ile kullanım (<c>services.AddNviKpsClient(...)</c>).</summary>
    public NviKpsClient(IHttpClientFactory httpClientFactory, IOptions<NviKpsOptions> options, ILogger<NviKpsClient>? logger = null)
        : this(() => httpClientFactory.CreateClient(HttpClientName), options.Value, logger)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
    }

    private NviKpsClient(Func<HttpClient> http, NviKpsOptions options, ILogger? logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        _http = http;
        _o = options;
        _logger = logger ?? NullLogger.Instance;
        _stsUrl = options.EffectiveStsAddress;
        _kpsUrl = options.EffectiveKpsAddress;
        _token = new TokenCache<NviToken>(options.TokenRefreshMargin);
    }

    // =====================================================================
    // Servisler
    // =====================================================================

    /// <inheritdoc />
    public async Task<KpsResult<TCKimlikNoDogrulama>> TCKimlikNoDogrulaAsync(long identityNumber, string firstName, string lastName,
        int birthYear, CancellationToken ct = default)
    {
        var kriter = new TCKimlikNoDogrulaSorguKriteri { TCKimlikNo = identityNumber, Ad = firstName, Soyad = lastName, DogumYil = birthYear };
        var liste = await TCKimlikNoDogrulaAsync(new[] { kriter }, ct).ConfigureAwait(false);
        return Tekil(liste, k => KpsError.From(k.HataBilgisi));
    }

    /// <inheritdoc />
    public async Task<KpsResult<TCKimlikNoDogrulama[]>> TCKimlikNoDogrulaAsync(IEnumerable<TCKimlikNoDogrulaSorguKriteri> criteria,
        CancellationToken ct = default)
    {
        var kriterler = KriterListesi(criteria, nameof(criteria));
        foreach (var k in kriterler)
        {
            KimlikNoKontrol(k.TCKimlikNo ?? 0, nameof(criteria));
            if (string.IsNullOrWhiteSpace(k.Ad) || string.IsNullOrWhiteSpace(k.Soyad))
                throw new ArgumentException("Ad ve soyad boş olamaz.", nameof(criteria));
            if (k.DogumYil is not (> 1800 and < 10000))
                throw new ArgumentException("Doğum yılı dört haneli olmalı.", nameof(criteria));
        }
        var (sonuc, xml) = await ListeleAsync<TCKimlikNoDogrulamaSonucu>(Services.TCKimlikNoDogrula, kriterler, ct).ConfigureAwait(false);
        return new KpsResult<TCKimlikNoDogrulama[]>(Services.TCKimlikNoDogrula, sonuc?.SorguSonucu, KpsError.From(sonuc?.HataBilgisi), xml);
    }

    /// <inheritdoc />
    public async Task<KpsResult<KisiBilgisi>> KisiSorgulaTCKimlikNoAsync(long identityNumber, CancellationToken ct = default)
    {
        var liste = await KisiSorgulaTCKimlikNoAsync(new[] { identityNumber }, ct).ConfigureAwait(false);
        return Tekil(liste, k => KpsError.From(k.HataBilgisi));
    }

    /// <inheritdoc />
    public async Task<KpsResult<KisiBilgisi[]>> KisiSorgulaTCKimlikNoAsync(IEnumerable<long> identityNumbers, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(identityNumbers);
        var kriterler = KriterListesi(identityNumbers.Select(no =>
        {
            KimlikNoKontrol(no, nameof(identityNumbers));
            return new KisiSorgulaTCKimlikNoSorguKriteri { TCKimlikNo = no };
        }), nameof(identityNumbers));
        var (sonuc, xml) = await ListeleAsync<KisiBilgisiSonucu>(Services.KisiSorgulaTCKimlikNo, kriterler, ct).ConfigureAwait(false);
        return new KpsResult<KisiBilgisi[]>(Services.KisiSorgulaTCKimlikNo, sonuc?.SorguSonucu, KpsError.From(sonuc?.HataBilgisi), xml);
    }

    // =====================================================================
    // Tipli çağrı: kriter listesi -> ListeleCoklu -> <ListeleCokluResult> -> WSDL tipi
    // =====================================================================

    private async Task<(TSonuc? Sonuc, XElement Xml)> ListeleAsync<TSonuc>(string servis, Array kriterler, CancellationToken ct)
        where TSonuc : class
    {
        var doc = await CallAsync(servis, ListeleCoklu, KriterXml(kriterler), ct).ConfigureAwait(false);

        const string sonucAdi = ListeleCoklu + "Result";
        var xml = doc.Descendants(XName.Get(sonucAdi, NviNs)).FirstOrDefault()
            ?? throw new KpsException($"NVİ {servis}: cevapta <{sonucAdi}> bulunamadı.", KpsErrorKind.InvalidResponse);
        try
        {
            using var okuyucu = xml.CreateReader();
            return (Serilestirici(typeof(TSonuc), sonucAdi).ReadObject(okuyucu) as TSonuc, xml);
        }
        catch (Exception ex) when (ex is SerializationException or XmlException or InvalidCastException)
        {
            throw new KpsException($"NVİ {servis} cevabı WSDL şemasına uymuyor: {ex.Message}", KpsErrorKind.InvalidResponse, inner: ex);
        }
    }

    /// <summary>Tek kriterli çağrının sonucu: listenin ilk elemanı; hata üst seviyede yoksa elemanın kendi HataBilgisi.</summary>
    private static KpsResult<T> Tekil<T>(KpsResult<T[]> liste, Func<T, KpsError?> hata) where T : class
    {
        var veri = liste.Data?.FirstOrDefault();
        return new KpsResult<T>(liste.Method, veri, liste.Error ?? (veri is null ? null : hata(veri)), liste.Xml);
    }

    private static T[] KriterListesi<T>(IEnumerable<T> kriterler, string parametre)
    {
        ArgumentNullException.ThrowIfNull(kriterler, parametre);
        var dizi = kriterler.ToArray();
        if (dizi.Length == 0) throw new ArgumentException("En az bir kriter verilmeli.", parametre);
        if (dizi.Any(k => k is null)) throw new ArgumentException("Kriter listesinde null eleman var.", parametre);
        return dizi;
    }

    private void KimlikNoKontrol(long kimlikNo, string parametre)
    {
        if (_o.ValidateIdentityNumber && !IdentityNumberValidator.IsValid(kimlikNo))
            throw new ArgumentException("Geçersiz kimlik numarası (11 hane ve kontrol haneleri tutmalı).", parametre);
    }

    /// <summary>Kriter dizisini WSDL'deki &lt;kriterListesi&gt; elemanı olarak serileştirir.</summary>
    internal static string KriterXml(Array kriterler)
    {
        var sb = new StringBuilder();
        var ayar = new XmlWriterSettings { OmitXmlDeclaration = true, ConformanceLevel = ConformanceLevel.Fragment };
        using (var w = XmlWriter.Create(sb, ayar))
            Serilestirici(kriterler.GetType(), "kriterListesi").WriteObject(w, kriterler);
        return sb.ToString();
    }

    private static DataContractSerializer Serilestirici(Type tip, string kok) =>
        Serilestiriciler.GetOrAdd((tip, kok), k => new DataContractSerializer(k.Tip, k.Kok, NviNs));

    // =====================================================================
    // Ham çağrı: token + imzalı SOAP zarfı + yeniden deneme
    // =====================================================================

    /// <inheritdoc />
    public async Task<XDocument> CallAsync(string service, string method, string bodyXml, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(service)) throw new ArgumentException("Servis adı boş olamaz.", nameof(service));
        if (string.IsNullOrEmpty(method)) throw new ArgumentException("İşlem adı boş olamaz.", nameof(method));
        return await WithRetryAsync(c => TekCagriAsync(service, method, bodyXml, c), _o.RetryCount, _o.RetryDelay, _logger, service, ct)
            .ConfigureAwait(false);
    }

    private async Task<XDocument> TekCagriAsync(string servis, string metot, string govdeXml, CancellationToken ct)
    {
        var token = await TokenAlAsync(ct).ConfigureAwait(false);
        try
        {
            return (await SendAsync(_http(), _kpsUrl, Zarf(servis, metot, govdeXml, token), sts: false, BaglantiNotu, ct).ConfigureAwait(false)).Doc;
        }
        catch (KpsException ex) when (ex.Kind == KpsErrorKind.Authentication)
        {
            // Önbellekteki token reddedildi (ör. sunucu yeniden başladı): bir kez yenisiyle dene.
            _logger.LogInformation("NVİ {Servis}: token reddedildi, yeni token alınıyor.", servis);
            ResetToken();
            token = await TokenAlAsync(ct).ConfigureAwait(false);
            return (await SendAsync(_http(), _kpsUrl, Zarf(servis, metot, govdeXml, token), sts: false, BaglantiNotu, ct).ConfigureAwait(false)).Doc;
        }
    }

    private string Zarf(string servis, string metot, string govdeXml, NviToken token)
    {
        var (created, expires) = Timestamp();
        var zamanDamgasi = ZamanDamgasi(created, expires);
        return
            $"<s:Envelope xmlns:s=\"{SoapNs}\" xmlns:a=\"{AddrNs}\">" +
            "<s:Header>" +
            $"<a:Action s:mustUnderstand=\"1\">{NviNs}/{servis}/{metot}</a:Action>" +
            $"<a:MessageID>urn:uuid:{Guid.NewGuid()}</a:MessageID>" +
            "<a:ReplyTo><a:Address>http://www.w3.org/2005/08/addressing/anonymous</a:Address></a:ReplyTo>" +
            $"<a:To s:mustUnderstand=\"1\">{Esc(_kpsUrl)}</a:To>" +
            $"<o:Security s:mustUnderstand=\"1\" xmlns:o=\"{WsseNs}\">" +
            zamanDamgasi +
            token.Xml + // token imzalı/şifreli olduğu için birebir (ham) eklenmeli
            Imza(zamanDamgasi, token) +
            "</o:Security>" +
            "</s:Header>" +
            $"<s:Body><{metot} xmlns=\"{NviNs}\">{govdeXml}</{metot}></s:Body>" +
            "</s:Envelope>";
    }

    // =====================================================================
    // İmza: zaman damgası, token'ın simetrik anahtarıyla HMAC-SHA1 (exc-c14n)
    // =====================================================================

    /// <summary>
    /// wsu:Timestamp, Exclusive XML Canonicalization (exc-c14n) çıktısıyla birebir aynı biçimde yazılır: namespace bildirimi
    /// elemanın kendisinde, öznitelikler sıralı, boşluk yok. Böylece özetlenen bayt dizisi ile zarftaki metin aynıdır.
    /// </summary>
    internal static string ZamanDamgasi(string created, string expires) =>
        $"<u:Timestamp xmlns:u=\"{WsuNs}\" u:Id=\"_0\"><u:Created>{created}</u:Created><u:Expires>{expires}</u:Expires></u:Timestamp>";

    /// <summary>SignedInfo'nun exc-c14n biçimi (boş elemanlar açık/kapalı etiketle, varsayılan namespace elemanın kendisinde).</summary>
    internal static string SignedInfo(string digestValue) =>
        $"<SignedInfo xmlns=\"{DsigNs}\">" +
        $"<CanonicalizationMethod Algorithm=\"{ExcC14n}\"></CanonicalizationMethod>" +
        $"<SignatureMethod Algorithm=\"{DsigNs}hmac-sha1\"></SignatureMethod>" +
        "<Reference URI=\"#_0\">" +
        $"<Transforms><Transform Algorithm=\"{ExcC14n}\"></Transform></Transforms>" +
        $"<DigestMethod Algorithm=\"{DsigNs}sha1\"></DigestMethod>" +
        $"<DigestValue>{digestValue}</DigestValue>" +
        "</Reference>" +
        "</SignedInfo>";

    internal static string Imza(string zamanDamgasi, NviToken token)
    {
        var digest = Convert.ToBase64String(SHA1.HashData(Encoding.UTF8.GetBytes(zamanDamgasi)));
        var signedInfo = SignedInfo(digest);
        var imza = Convert.ToBase64String(HMACSHA1.HashData(token.Key, Encoding.UTF8.GetBytes(signedInfo)));
        return
            $"<Signature xmlns=\"{DsigNs}\">" +
            signedInfo +
            $"<SignatureValue>{imza}</SignatureValue>" +
            "<KeyInfo>" +
            $"<o:SecurityTokenReference xmlns:o=\"{WsseNs}\" b:TokenType=\"{SamlV11}\" " +
            "xmlns:b=\"http://docs.oasis-open.org/wss/oasis-wss-wssecurity-secext-1.1.xsd\">" +
            "<o:KeyIdentifier ValueType=\"http://docs.oasis-open.org/wss/oasis-wss-saml-token-profile-1.0#SAMLAssertionID\">" +
            Esc(token.AssertionId) +
            "</o:KeyIdentifier>" +
            "</o:SecurityTokenReference>" +
            "</KeyInfo>" +
            "</Signature>";
    }

    // =====================================================================
    // STS
    // =====================================================================

    /// <summary>Önbellekteki token'ı atar; sonraki istekte STS'den yenisi alınır.</summary>
    public void ResetToken() => _token.Reset();

    private Task<NviToken> TokenAlAsync(CancellationToken ct) => _token.GetAsync(async c =>
    {
        var t = await StsdenTokenAlAsync(c).ConfigureAwait(false);
        _logger.LogDebug("NVİ KPS STS token alındı, geçerlilik sonu {Bitis:o}", t.Bitis);
        return t;
    }, ct);

    private async Task<(NviToken Token, DateTimeOffset Bitis)> StsdenTokenAlAsync(CancellationToken ct)
    {
        var (created, expires) = Timestamp();
        var envelope =
            $"<s:Envelope xmlns:s=\"{SoapNs}\" xmlns:a=\"{AddrNs}\" xmlns:u=\"{WsuNs}\">" +
            "<s:Header>" +
            $"<a:Action s:mustUnderstand=\"1\">{TrustNs}/RST/Issue</a:Action>" +
            $"<a:MessageID>urn:uuid:{Guid.NewGuid()}</a:MessageID>" +
            "<a:ReplyTo><a:Address>http://www.w3.org/2005/08/addressing/anonymous</a:Address></a:ReplyTo>" +
            $"<a:To s:mustUnderstand=\"1\">{Esc(_stsUrl)}</a:To>" +
            $"<o:Security s:mustUnderstand=\"1\" xmlns:o=\"{WsseNs}\">" +
            $"<u:Timestamp u:Id=\"_0\"><u:Created>{created}</u:Created><u:Expires>{expires}</u:Expires></u:Timestamp>" +
            $"<o:UsernameToken u:Id=\"uuid-{Guid.NewGuid()}-1\">" +
            $"<o:Username>{Esc(_o.Username)}</o:Username>" +
            "<o:Password Type=\"http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-username-token-profile-1.0#PasswordText\">" +
            $"{Esc(_o.Password)}</o:Password>" +
            "</o:UsernameToken>" +
            "</o:Security>" +
            "</s:Header>" +
            "<s:Body>" +
            $"<trust:RequestSecurityToken xmlns:trust=\"{TrustNs}\">" +
            $"<trust:TokenType>{SamlV11}</trust:TokenType>" +
            $"<trust:RequestType>{TrustNs}/Issue</trust:RequestType>" +
            "<wsp:AppliesTo xmlns:wsp=\"http://schemas.xmlsoap.org/ws/2004/09/policy\">" +
            $"<a:EndpointReference><a:Address>{Esc(_kpsUrl)}</a:Address></a:EndpointReference>" +
            "</wsp:AppliesTo>" +
            $"<trust:KeyType>{TrustNs}/SymmetricKey</trust:KeyType>" +
            "</trust:RequestSecurityToken>" +
            "</s:Body>" +
            "</s:Envelope>";

        var (raw, doc) = await SendAsync(_http(), _stsUrl, envelope, sts: true, connectionHint: null, ct).ConfigureAwait(false);
        return StsCevabiniOku(raw, doc);
    }

    /// <summary>STS cevabından token XML'ini (ham), imza anahtarını, token kimliğini ve geçerlilik sonunu çıkarır.</summary>
    internal static (NviToken Token, DateTimeOffset Bitis) StsCevabiniOku(string raw, XDocument doc)
    {
        XNamespace trust = TrustNs, wsse = WsseNs, wsu = WsuNs;

        // İmza/şifre bozulmasın diye token yeniden serileştirilmeden ham metinden kesilir.
        var m = TokenRegex().Match(raw);
        if (!m.Success)
            throw new KpsException("NVİ STS cevabında RequestedSecurityToken bulunamadı.", KpsErrorKind.InvalidResponse);

        var kanit = doc.Descendants(trust + "RequestedProofToken").FirstOrDefault();
        var anahtar = kanit?.Element(trust + "BinarySecret")?.Value;
        if (string.IsNullOrWhiteSpace(anahtar))
            throw new KpsException(
                kanit?.Element(trust + "ComputedKey") is not null
                    ? "NVİ STS imza anahtarını istemci entropisiyle hesaplanacak şekilde (ComputedKey) döndürdü; bu akış desteklenmiyor."
                    : "NVİ STS cevabında imza anahtarı (RequestedProofToken/BinarySecret) bulunamadı.",
                KpsErrorKind.InvalidResponse);

        byte[] key;
        try { key = Convert.FromBase64String(anahtar.Trim()); }
        catch (FormatException ex)
        {
            throw new KpsException("NVİ STS imza anahtarı base64 değil.", KpsErrorKind.InvalidResponse, inner: ex);
        }

        // Token'a başvuru: STS'nin verdiği iç başvuru (RequestedAttachedReference), yoksa cevaptaki ilk KeyIdentifier.
        var kimlik = (doc.Descendants(trust + "RequestedAttachedReference").Descendants(wsse + "KeyIdentifier").FirstOrDefault()
                      ?? doc.Descendants(wsse + "KeyIdentifier").FirstOrDefault())?.Value.Trim();
        if (string.IsNullOrEmpty(kimlik))
            throw new KpsException("NVİ STS cevabında token kimliği (KeyIdentifier) bulunamadı.", KpsErrorKind.InvalidResponse);

        // Geçerlilik: WS-Trust Lifetime; yoksa (şifresiz token'larda) SAML Conditions; ikisi de yoksa 5 dk.
        var bitisMetni = doc.Descendants(trust + "Lifetime").Elements(wsu + "Expires").FirstOrDefault()?.Value
                         ?? doc.Descendants(XName.Get("Conditions", "urn:oasis:names:tc:SAML:1.0:assertion")).FirstOrDefault()
                             ?.Attribute("NotOnOrAfter")?.Value;
        var bitis = DateTimeOffset.TryParse(bitisMetni, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var b)
            ? b
            : DateTimeOffset.UtcNow.AddMinutes(5);

        return (new NviToken(m.Groups["token"].Value, key, kimlik), bitis);
    }

    [GeneratedRegex(@"<(?<p>\w+:)?RequestedSecurityToken>(?<token>.*)</\k<p>RequestedSecurityToken>", RegexOptions.Singleline)]
    private static partial Regex TokenRegex();
}

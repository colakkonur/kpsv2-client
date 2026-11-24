using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using KpsV2.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace KpsV2;

/// <summary>
/// KPS v2 istemcisi: STS'den SAML token alır (WS-Trust 1.3 / UsernameToken, Bearer), token'ı süresi dolana kadar
/// önbellekte tutar ve Router servisine SOAP 1.2 istekleri atar. İstek/cevap gövdeleri WSDL'den üretilen
/// veri sözleşmeleriyle (<see cref="KpsV2.Contracts"/>) DataContractSerializer ile işlenir.
/// Thread-safe'tir; uygulama boyunca tek örnek kullanın.
/// </summary>
public sealed partial class KpsClient : IKpsClient
{
    /// <summary>KPS veri sözleşmelerinin namespace'i.</summary>
    public const string KpsNs = "https://www.saglik.gov.tr/KPS/01/01/2017";

    /// <summary>DI'da kullanılan adlandırılmış HttpClient.</summary>
    public const string HttpClientName = "KpsV2";

    private const string SoapNs = "http://www.w3.org/2003/05/soap-envelope";
    private const string AddrNs = "http://www.w3.org/2005/08/addressing";
    private const string WsuNs = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
    private const string WsseNs = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
    private const string TrustNs = "http://docs.oasis-open.org/ws-sx/ws-trust/200512";
    private static readonly XNamespace SamlNs = "urn:oasis:names:tc:SAML:1.0:assertion";

    private static readonly ConcurrentDictionary<(Type Tip, string Kok), DataContractSerializer> Serilestiriciler = new();

    private readonly Func<HttpClient> _http;
    private readonly KpsOptions _o;
    private readonly ILogger _logger;
    private readonly string _stsUrl;
    private readonly string _kpsUrl;

    private readonly SemaphoreSlim _tokenKilidi = new(1, 1);
    private Token? _token;

    private sealed record Token(string Xml, DateTimeOffset Bitis);

    /// <summary>DI dışında kullanım: HttpClient'ın ömrünü çağıran yönetir.</summary>
    public KpsClient(HttpClient httpClient, KpsOptions options, ILogger<KpsClient>? logger = null)
        : this(() => httpClient, options, logger)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
    }

    /// <summary>DI ile kullanım (<c>services.AddKpsClient(...)</c>).</summary>
    public KpsClient(IHttpClientFactory httpClientFactory, IOptions<KpsOptions> options, ILogger<KpsClient>? logger = null)
        : this(() => httpClientFactory.CreateClient(HttpClientName), options.Value, logger)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
    }

    private KpsClient(Func<HttpClient> http, KpsOptions options, ILogger? logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        _http = http;
        _o = options;
        _logger = logger ?? NullLogger.Instance;
        var baseUrl = options.EffectiveBaseAddress.ToString().TrimEnd('/');
        _stsUrl = $"{baseUrl}/STS/STSService.svc";
        _kpsUrl = $"{baseUrl}/Router/RoutingService.svc";
    }

    /// <summary>Kullanılan ortam.</summary>
    public KpsEnvironment Environment => _o.Environment;

    // =====================================================================
    // Kps Servisi
    // =====================================================================

    /// <inheritdoc />
    public Task<KpsResult<BilesikKisiBilgisi>> BilesikKisiSorgulaAsync(long identityNumber, CancellationToken ct = default) =>
        KpsAsync<KpsServisSonucuBilesikKisiBilgisi, BilesikKisiBilgisi>("BilesikKisiSorgula", KimlikNoXml(identityNumber), ct);

    /// <inheritdoc />
    public Task<KpsResult<BilesikKisiveAdresBilgisi>> BilesikKisiveAdresSorgulaAsync(long identityNumber, CancellationToken ct = default) =>
        KpsAsync<KpsServisSonucuBilesikKisiveAdresBilgisi, BilesikKisiveAdresBilgisi>("BilesikKisiveAdresSorgula", KimlikNoXml(identityNumber), ct);

    /// <inheritdoc />
    public Task<KpsResult<BilesikKisiveYerlesimYeriBilgisi>> BilesikKisiveYerlesimYeriSorgulaAsync(long identityNumber, CancellationToken ct = default) =>
        KpsAsync<KpsServisSonucuBilesikKisiveYerlesimYeriBilgisi, BilesikKisiveYerlesimYeriBilgisi>("BilesikKisiveYerlesimYeriSorgula", KimlikNoXml(identityNumber), ct);

    /// <inheritdoc />
    public Task<KpsResult<TCKisiBilgisi>> TcKimlikNoSorgulaAsync(KisiSorgulaKisiBilgileriCO criteria, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        // KPS saat dilimi beklemez: yalnızca tarih kısmı, Kind=Unspecified ("yyyy-MM-ddT00:00:00") gönderilir.
        var gonderilecek = new KisiSorgulaKisiBilgileriCO
        {
            Adi = criteria.Adi,
            Soyadi = criteria.Soyadi,
            BabaAdi = criteria.BabaAdi,
            AnneAdi = criteria.AnneAdi,
            Cinsiyet = criteria.Cinsiyet,
            DogumTarihi = DateTime.SpecifyKind(criteria.DogumTarihi.Date, DateTimeKind.Unspecified),
            DogumYeri = criteria.DogumYeri,
        };
        return KpsAsync<KpsServisSonucuKisiTemelBilgisi, TCKisiBilgisi>("TcKimlikNoSorgula", KriterXml(gonderilecek), ct);
    }

    /// <inheritdoc />
    public Task<KpsResult<KisiAdresBilgisi>> KimlikNoIleAdresBilgisiSorgulaAsync(long identityNumber, CancellationToken ct = default) =>
        KpsAsync<KpsServisSonucuKisiAdresBilgisi, KisiAdresBilgisi>("KimlikNoIleAdresBilgisiSorgula", KimlikNoXml(identityNumber), ct);

    /// <inheritdoc />
    public Task<KpsResult<TcKimlikNoIleAdresBilgisi>> KimlikNoIleYerlesimYeriSorgulaAsync(long identityNumber, CancellationToken ct = default) =>
        KpsAsync<KpsServisSonucuKisiYerlesimYeriBilgisi, TcKimlikNoIleAdresBilgisi>("KimlikNoIleYerlesimYeriSorgula", KimlikNoXml(identityNumber), ct);

    /// <inheritdoc />
    public Task<KpsResult<AcikAdres>> AdresNoIleYerlesimYeriSorgulaAsync(long addressNumber, CancellationToken ct = default)
    {
        if (addressNumber <= 0) throw new ArgumentOutOfRangeException(nameof(addressNumber), "Adres numarası pozitif olmalı.");
        return KpsAsync<KpsServisSonucuAksAcikAdresServisBilgisi, AcikAdres>(
            "AdresNoIleYerlesimYeriSorgula", $"<adresNo>{addressNumber.ToString(CultureInfo.InvariantCulture)}</adresNo>", ct);
    }

    /// <inheritdoc />
    public Task<KpsResult<GenelNufusKayitOrnegi>> KimlikNoIleNufusKayitOrnegiSorgulaAsync(long identityNumber, CancellationToken ct = default) =>
        KpsAsync<KpsServisSonucuGenelNufusKayitOrnegi, GenelNufusKayitOrnegi>("KimlikNoIleNufusKayitOrnegiSorgula", KimlikNoXml(identityNumber), ct);

    /// <inheritdoc />
    public Task<KpsResult<AileBilgisi>> AileBireyleriSorgulaAsync(AileBireyleriSorgulaKriter criteria, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        KimlikNoKontrol(criteria.TCKimlikNo, nameof(criteria));
        return KpsAsync<KpsServisSonucuAileBilgisi, AileBilgisi>("AileBireyleriSorgula", KriterXml(criteria), ct);
    }

    /// <inheritdoc />
    public Task<KpsResult<YakinlikServisiAileBilgisi>> KimlikNoIleVatandasAileBilgisiSorgulaAsync(YakinlikSorgulaKriter criteria, CancellationToken ct = default) =>
        KpsAsync<KpsServisSonucuVatandasAileBilgisi, YakinlikServisiAileBilgisi>("KimlikNoIleVatandasAileBilgisiSorgula", YakinlikKriterXml(criteria), ct);

    /// <inheritdoc />
    public Task<KpsResult<YakinlikServisiAileBilgisi>> KimlikNoIleVatandasAileBilgisiKardesDahilSorgulaAsync(YakinlikSorgulaKriter criteria, CancellationToken ct = default) =>
        KpsAsync<KpsServisSonucuVatandasAileBilgisi, YakinlikServisiAileBilgisi>("KimlikNoIleVatandasAileBilgisiKardesDahilSorgula", YakinlikKriterXml(criteria), ct);

    /// <inheritdoc />
    public Task<KpsResult<MaviKartliAileBilgisi>> KimlikNoIleMaviKartliAileBilgisiSorgulaAsync(YakinlikSorgulaKriter criteria, CancellationToken ct = default) =>
        KpsAsync<KpsServisSonucuMaviKartliAileBilgisi, MaviKartliAileBilgisi>("KimlikNoIleMaviKartliAileBilgisiSorgula", YakinlikKriterXml(criteria), ct);

    /// <inheritdoc />
    public Task<KpsResult<MaviKartliAileBilgisi>> KimlikNoIleMaviKartliAileBilgisiKardesDahilSorgulaAsync(YakinlikSorgulaKriter criteria, CancellationToken ct = default) =>
        KpsAsync<KpsServisSonucuMaviKartliAileBilgisi, MaviKartliAileBilgisi>("KimlikNoIleMaviKartliAileBilgisiKardesDahilSorgula", YakinlikKriterXml(criteria), ct);

    /// <inheritdoc />
    public Task<KpsResult<KamuYakinlikServisiAileBilgisi>> KimlikNoIleVatandasAileYakinlikBilgisiSorgulaAsync(KamuYakinlikSorgulaKriter criteria, CancellationToken ct = default) =>
        KpsAsync<KpsServisSonucuKamuYakinlikAileBilgisi, KamuYakinlikServisiAileBilgisi>("KimlikNoIleVatandasAileYakinlikBilgisiSorgula", KamuYakinlikKriterXml(criteria), ct);

    /// <inheritdoc />
    public Task<KpsResult<KamuYakinlikServisiAileBilgisi>> KimlikNoIleVatandasAileYakinlikBilgisiKardesDahilSorgulaAsync(KamuYakinlikSorgulaKriter criteria, CancellationToken ct = default) =>
        KpsAsync<KpsServisSonucuKamuYakinlikAileBilgisi, KamuYakinlikServisiAileBilgisi>("KimlikNoIleVatandasAileYakinlikBilgisiKardesDahilSorgula", KamuYakinlikKriterXml(criteria), ct);

    /// <inheritdoc />
    public Task<KpsResult<WcfService[]>> YetkiListesiAsync(CancellationToken ct = default) =>
        KpsAsync<KpsServisSonucuYetkiListesi, WcfService[]>("YetkiListesi", "", ct);

    // =====================================================================
    // Parametre Servisi
    // =====================================================================

    /// <inheritdoc />
    public Task<KpsResult<string[]>> ParametreAdListesiAsync(CancellationToken ct = default) =>
        CagirAsync<KpsServisSonucuParametreListesi, string[]>(KpsService.Parameter, "ParametreAdListesi", "", ct);

    /// <inheritdoc />
    public Task<KpsResult<Parametre[]>> ParametreDegerleriAsync(string parameterName, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(parameterName)) throw new ArgumentException("Parametre adı boş olamaz.", nameof(parameterName));
        return CagirAsync<KpsServisSonucuParametreDegerleri, Parametre[]>(
            KpsService.Parameter, "ParametreDegerleri", $"<parametreAdi>{Esc(parameterName)}</parametreAdi>", ct);
    }

    // =====================================================================
    // Tipli çağrı: istek gövdesi -> SOAP -> <{metot}Result> -> WSDL tipi
    // =====================================================================

    private Task<KpsResult<T>> KpsAsync<TZarf, T>(string metot, string govdeXml, CancellationToken ct)
        where TZarf : class, IKpsServiceResult<T>
        where T : class =>
        CagirAsync<TZarf, T>(KpsService.Kps, metot, govdeXml, ct);

    private async Task<KpsResult<T>> CagirAsync<TZarf, T>(string servis, string metot, string govdeXml, CancellationToken ct)
        where TZarf : class, IKpsServiceResult<T>
        where T : class
    {
        var doc = await CallAsync(servis, metot, govdeXml, ct).ConfigureAwait(false);

        var sonucAdi = metot + "Result";
        var xml = doc.Descendants(XName.Get(sonucAdi, KpsNs)).FirstOrDefault()
            ?? throw new KpsException($"KPS {metot}: cevapta <{sonucAdi}> bulunamadı.", KpsErrorKind.InvalidResponse);

        TZarf? zarf;
        try
        {
            using var okuyucu = xml.CreateReader();
            zarf = Serilestirici(typeof(TZarf), sonucAdi).ReadObject(okuyucu) as TZarf;
        }
        catch (Exception ex) when (ex is SerializationException or XmlException or InvalidCastException)
        {
            throw new KpsException($"KPS {metot} cevabı WSDL şemasına uymuyor: {ex.Message}", KpsErrorKind.InvalidResponse, inner: ex);
        }

        var veri = zarf?.Sonuc;
        var hata = KpsError.From(zarf?.HataBilgisi) ?? (veri is IHasError h ? KpsError.From(h.HataBilgisi) : null);
        return new KpsResult<T>(metot, veri, hata, xml);
    }

    // =====================================================================
    // Ham çağrı: token + SOAP zarfı + yeniden deneme
    // =====================================================================

    /// <inheritdoc />
    public async Task<XDocument> CallAsync(string service, string method, string bodyXml, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(service)) throw new ArgumentException("Servis adı boş olamaz.", nameof(service));
        if (string.IsNullOrEmpty(method)) throw new ArgumentException("Metot adı boş olamaz.", nameof(method));
        var (servis, metot, govdeXml) = (service, method, bodyXml);

        var sure = Stopwatch.StartNew();
        for (var deneme = 0; ; deneme++)
        {
            try
            {
                var doc = await TekCagriAsync(servis, metot, govdeXml, ct).ConfigureAwait(false);
                _logger.LogDebug("KPS {Metot} tamamlandı ({Sure} ms, {Deneme}. deneme)", metot, sure.ElapsedMilliseconds, deneme + 1);
                return doc;
            }
            catch (KpsException ex) when (ex.IsTransient && deneme < _o.RetryCount && !ct.IsCancellationRequested)
            {
                var bekle = TimeSpan.FromTicks(_o.RetryDelay.Ticks * (1L << deneme));
                _logger.LogWarning("KPS {Metot} geçici hata ({Tur}), {Bekle} ms sonra tekrar denenecek ({Deneme}/{Toplam}): {Hata}",
                    metot, ex.Kind, (long)bekle.TotalMilliseconds, deneme + 1, _o.RetryCount, ex.SoapFault ?? ex.Message);
                await Task.Delay(bekle, ct).ConfigureAwait(false);
            }
        }
    }

    private async Task<XDocument> TekCagriAsync(string servis, string metot, string govdeXml, CancellationToken ct)
    {
        var token = await TokenAlAsync(ct).ConfigureAwait(false);
        try
        {
            return (await GonderAsync(_kpsUrl, KpsZarfi(servis, metot, govdeXml, token), sts: false, ct).ConfigureAwait(false)).Doc;
        }
        catch (KpsException ex) when (ex.Kind == KpsErrorKind.Authentication)
        {
            // Önbellekteki token KPS tarafında reddedildi (ör. sunucu yeniden başladı): bir kez yenisiyle dene.
            _logger.LogInformation("KPS {Metot}: token reddedildi, yeni token alınıyor.", metot);
            ResetToken();
            token = await TokenAlAsync(ct).ConfigureAwait(false);
            return (await GonderAsync(_kpsUrl, KpsZarfi(servis, metot, govdeXml, token), sts: false, ct).ConfigureAwait(false)).Doc;
        }
    }

    private string KpsZarfi(string servis, string metot, string govdeXml, string token)
    {
        var (created, expires) = ZamanDamgasi();
        return
            $"<s:Envelope xmlns:s=\"{SoapNs}\" xmlns:a=\"{AddrNs}\" xmlns:u=\"{WsuNs}\">" +
            "<s:Header>" +
            $"<a:Action s:mustUnderstand=\"1\">{KpsNs}/{servis}/{metot}</a:Action>" +
            $"<a:MessageID>urn:uuid:{Guid.NewGuid()}</a:MessageID>" +
            "<a:ReplyTo><a:Address>http://www.w3.org/2005/08/addressing/anonymous</a:Address></a:ReplyTo>" +
            $"<a:To s:mustUnderstand=\"1\">{_kpsUrl}</a:To>" +
            $"<o:Security s:mustUnderstand=\"1\" xmlns:o=\"{WsseNs}\">" +
            $"<u:Timestamp u:Id=\"_0\"><u:Created>{created}</u:Created><u:Expires>{expires}</u:Expires></u:Timestamp>" +
            token + // SAML Assertion imzalı olduğu için birebir (ham) eklenmeli
            "</o:Security>" +
            "</s:Header>" +
            $"<s:Body><{metot} xmlns=\"{KpsNs}\">{govdeXml}</{metot}></s:Body>" +
            "</s:Envelope>";
    }

    /// <summary>Önbellekteki token'ı atar; sonraki istekte STS'den yenisi alınır.</summary>
    public void ResetToken() => Volatile.Write(ref _token, null);

    // =====================================================================
    // STS
    // =====================================================================

    private async Task<string> TokenAlAsync(CancellationToken ct)
    {
        var t = Volatile.Read(ref _token);
        if (Gecerli(t)) return t!.Xml;

        await _tokenKilidi.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            t = Volatile.Read(ref _token);
            if (Gecerli(t)) return t!.Xml; // başka bir istek bu arada almış olabilir

            t = await StsdenTokenAlAsync(ct).ConfigureAwait(false);
            Volatile.Write(ref _token, t);
            _logger.LogDebug("KPS ({Ortam}) STS token alındı, geçerlilik sonu {Bitis:o}", _o.Environment, t.Bitis);
            return t.Xml;
        }
        finally
        {
            _tokenKilidi.Release();
        }
    }

    private bool Gecerli(Token? t) => t is not null && DateTimeOffset.UtcNow < t.Bitis - _o.TokenRefreshMargin;

    private async Task<Token> StsdenTokenAlAsync(CancellationToken ct)
    {
        var (created, expires) = ZamanDamgasi();
        var refParams = _o.IsApplicationUser
            ? $"<SorgulayanKimlikNo a:IsReferenceParameter=\"true\" xmlns=\"\">{Esc(_o.RequesterIdentityNumber)}</SorgulayanKimlikNo>"
            : $"<KurumKodu a:IsReferenceParameter=\"true\" xmlns=\"\">{Esc(_o.InstitutionCode)}</KurumKodu>" +
              $"<UygulamaKodu a:IsReferenceParameter=\"true\" xmlns=\"\">{Esc(_o.ApplicationCode)}</UygulamaKodu>";

        var envelope =
            $"<s:Envelope xmlns:s=\"{SoapNs}\" xmlns:a=\"{AddrNs}\" xmlns:u=\"{WsuNs}\">" +
            "<s:Header>" +
            $"<a:Action s:mustUnderstand=\"1\">{TrustNs}/RST/Issue</a:Action>" +
            $"<a:MessageID>urn:uuid:{Guid.NewGuid()}</a:MessageID>" +
            "<a:ReplyTo><a:Address>http://www.w3.org/2005/08/addressing/anonymous</a:Address></a:ReplyTo>" +
            $"<a:To s:mustUnderstand=\"1\">{_stsUrl}</a:To>" +
            refParams +
            $"<o:Security s:mustUnderstand=\"1\" xmlns:o=\"{WsseNs}\">" +
            $"<u:Timestamp u:Id=\"_0\"><u:Created>{created}</u:Created><u:Expires>{expires}</u:Expires></u:Timestamp>" +
            $"<o:UsernameToken u:Id=\"uuid-{Guid.NewGuid()}-1\">" +
            $"<o:Username>{Esc(_o.Username)}</o:Username><o:Password>{Esc(_o.Password)}</o:Password>" +
            "</o:UsernameToken>" +
            "</o:Security>" +
            "</s:Header>" +
            "<s:Body>" +
            $"<trust:RequestSecurityToken xmlns:trust=\"{TrustNs}\">" +
            "<wsp:AppliesTo xmlns:wsp=\"http://schemas.xmlsoap.org/ws/2004/09/policy\">" +
            $"<a:EndpointReference><a:Address>{_kpsUrl}</a:Address></a:EndpointReference>" +
            "</wsp:AppliesTo>" +
            $"<trust:KeyType>{TrustNs}/Bearer</trust:KeyType>" +
            $"<trust:RequestType>{TrustNs}/Issue</trust:RequestType>" +
            "</trust:RequestSecurityToken>" +
            "</s:Body>" +
            "</s:Envelope>";

        var (raw, doc) = await GonderAsync(_stsUrl, envelope, sts: true, ct).ConfigureAwait(false);

        // İmza bozulmasın diye Assertion yeniden serileştirilmeden ham metinden kesilir.
        var m = TokenRegex().Match(raw);
        if (!m.Success)
            throw new KpsException("STS cevabında RequestedSecurityToken bulunamadı.", KpsErrorKind.InvalidResponse);

        var notOnOrAfter = doc.Descendants(SamlNs + "Conditions").FirstOrDefault()?.Attribute("NotOnOrAfter")?.Value;
        var bitis = DateTimeOffset.TryParse(notOnOrAfter, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var b)
            ? b
            : DateTimeOffset.UtcNow.AddMinutes(5);
        return new Token(m.Groups["token"].Value, bitis);
    }

    // =====================================================================
    // HTTP + hata sınıflandırması
    // =====================================================================

    private async Task<(string Raw, XDocument Doc)> GonderAsync(string url, string envelope, bool sts, CancellationToken ct)
    {
        using var content = new StringContent(envelope, Encoding.UTF8);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/soap+xml; charset=utf-8");

        HttpResponseMessage resp;
        try
        {
            resp = await _http().PostAsync(url, content, ct).ConfigureAwait(false);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new KpsException($"KPS isteği zaman aşımına uğradı ({url}).", KpsErrorKind.Timeout, inner: ex);
        }
        catch (HttpRequestException ex)
        {
            var ipNotu = _o.Environment == KpsEnvironment.Prod ? " Prod ortamına yalnızca KPS'ye tanımlı IP'lerden erişilebilir." : "";
            throw new KpsException($"KPS'ye bağlanılamadı ({url}): {ex.Message}{ipNotu}", KpsErrorKind.Connection, inner: ex);
        }

        using (resp)
        {
            var raw = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            XDocument? doc = null;
            try { doc = XDocument.Parse(raw); }
            catch (XmlException) { /* HTML veya boş cevap */ }

            if (!resp.IsSuccessStatusCode || doc is null)
            {
                var fault = doc?.Descendants(XName.Get("Text", SoapNs)).FirstOrDefault()?.Value;
                var tur = Classify(resp.StatusCode, fault, sts);
                var ozet = fault ?? (raw.Length > 500 ? raw[..500] + "..." : raw);
                throw new KpsException($"KPS hatası ({tur}, HTTP {(int)resp.StatusCode}, {url}): {ozet}", tur, resp.StatusCode, fault);
            }
            return (raw, doc);
        }
    }

    internal static KpsErrorKind Classify(HttpStatusCode kod, string? fault, bool sts)
    {
        var f = fault ?? "";
        bool Icerir(string s) => f.Contains(s, StringComparison.OrdinalIgnoreCase);

        if (Icerir("yetkiniz") || Icerir("yetki yok") || Icerir("not authorized")) return KpsErrorKind.Unauthorized;
        // KPS Router'ın arka servisten cevap alamaması (test ortamında sık görülür) veya bağlantının kopması.
        if (Icerir("timed out") || Icerir("zaman aşımı") || Icerir("receiving the HTTP response")) return KpsErrorKind.Timeout;
        // STS'nin döndürdüğü her fault (şifre, uygulama kodu, kurum kodu...) ve KPS'nin token'ı reddetmesi.
        if (sts && fault is not null) return KpsErrorKind.Authentication;
        if (Icerir("verifying security") || Icerir("security token")) return KpsErrorKind.Authentication;
        if ((int)kod >= 500) return KpsErrorKind.Server;
        return KpsErrorKind.Unknown;
    }

    // =====================================================================
    // İstek gövdesi yardımcıları
    // =====================================================================

    private string KimlikNoXml(long kimlikNo)
    {
        KimlikNoKontrol(kimlikNo, nameof(kimlikNo));
        return $"<kimlikNo>{kimlikNo.ToString(CultureInfo.InvariantCulture)}</kimlikNo>";
    }

    private string YakinlikKriterXml(YakinlikSorgulaKriter criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        KimlikNoKontrol(criteria.KimlikNo, nameof(criteria));
        return KriterXml(criteria);
    }

    private string KamuYakinlikKriterXml(KamuYakinlikSorgulaKriter criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        KimlikNoKontrol(criteria.KimlikNo, nameof(criteria));
        return KriterXml(criteria);
    }

    private void KimlikNoKontrol(long kimlikNo, string parametre)
    {
        if (_o.ValidateIdentityNumber && !IdentityNumberValidator.IsValid(kimlikNo))
            throw new ArgumentException("Geçersiz kimlik numarası (11 hane ve kontrol haneleri tutmalı).", parametre);
    }

    /// <summary>Kriter nesnesini WSDL'deki &lt;criteria&gt; elemanı olarak serileştirir (alan sırası şemadaki gibi).</summary>
    internal static string KriterXml(object criteria)
    {
        var sb = new StringBuilder();
        var ayar = new XmlWriterSettings { OmitXmlDeclaration = true, ConformanceLevel = ConformanceLevel.Fragment };
        using (var w = XmlWriter.Create(sb, ayar))
            Serilestirici(criteria.GetType(), "kriter").WriteObject(w, criteria);
        return sb.ToString();
    }

    private static DataContractSerializer Serilestirici(Type tip, string kok) =>
        Serilestiriciler.GetOrAdd((tip, kok), k => new DataContractSerializer(k.Tip, k.Kok, KpsNs));

    private static (string Created, string Expires) ZamanDamgasi()
    {
        var now = DateTime.UtcNow;
        return (now.AddMinutes(-1).ToString("o", CultureInfo.InvariantCulture),
                now.AddMinutes(5).ToString("o", CultureInfo.InvariantCulture));
    }

    private static string Esc(string? value) => SecurityElement.Escape(value ?? "") ?? "";

    [GeneratedRegex(@"<(?<p>\w+:)?RequestedSecurityToken>(?<token>.*)</\k<p>RequestedSecurityToken>", RegexOptions.Singleline)]
    private static partial Regex TokenRegex();
}

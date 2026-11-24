using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace KpsV2.Client.Tests;

/// <summary>STS ve Router'ı taklit eden HTTP handler; gelen istekleri kaydeder.</summary>
internal sealed class SahteKps : HttpMessageHandler
{
    public const string Ns = "https://www.saglik.gov.tr/KPS/01/01/2017";

    public ConcurrentQueue<(string Url, string Govde)> Istekler { get; } = new();
    public int StsSayisi => Istekler.Count(i => i.Url.Contains("/STS/"));
    public int KpsSayisi => Istekler.Count(i => i.Url.Contains("/Router/"));
    public string SonKpsGovdesi => Istekler.Last(i => i.Url.Contains("/Router/")).Govde;

    public DateTimeOffset TokenBitis { get; set; } = DateTimeOffset.UtcNow.AddHours(1);

    /// <summary>Router cevabı. Varsayılan: istenen metot için HataBilgisi ve Sonuc'u boş (nil) cevap.</summary>
    public Func<string, (HttpStatusCode, string)> KpsCevabi { get; set; } = govde => (HttpStatusCode.OK, BosSonuc(Metot(govde)));

    /// <summary>Doluysa Router cevapları bu sırayla verilir; bitince <see cref="KpsCevabi"/>'na dönülür.</summary>
    public ConcurrentQueue<(HttpStatusCode, string)> SiradakiCevaplar { get; } = new();

    public (HttpStatusCode, string)? StsHatasi { get; set; }
    public TimeSpan StsGecikmesi { get; set; } = TimeSpan.Zero;

    /// <summary>Assertion, imza bozulmasın diye birebir taşınmalı: boşluklar ve öznitelik sırası korunmalı.</summary>
    public const string Assertion =
        "<saml:Assertion MajorVersion=\"1\" MinorVersion=\"1\" AssertionID=\"_test\"  Issuer=\"KPS-STS\" " +
        "xmlns:saml=\"urn:oasis:names:tc:SAML:1.0:assertion\">\n  <saml:Conditions NotBefore=\"2026-01-01T00:00:00Z\" NotOnOrAfter=\"{BITIS}\"/>" +
        "<Signature xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><SignatureValue>QUJD</SignatureValue></Signature></saml:Assertion>";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var url = request.RequestUri!.ToString();
        var govde = await request.Content!.ReadAsStringAsync(ct);
        Istekler.Enqueue((url, govde));

        if (url.Contains("/STS/"))
        {
            if (StsGecikmesi > TimeSpan.Zero) await Task.Delay(StsGecikmesi, ct);
            if (StsHatasi is { } h) return Cevap(h.Item1, h.Item2);
            return Cevap(HttpStatusCode.OK, StsCevabi());
        }
        var (kod, icerik) = SiradakiCevaplar.TryDequeue(out var sirada) ? sirada : KpsCevabi(govde);
        return Cevap(kod, icerik);
    }

    public string TokenXml => Assertion.Replace("{BITIS}", TokenBitis.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

    private string StsCevabi() =>
        "<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\"><s:Body>" +
        "<trust:RequestSecurityTokenResponseCollection xmlns:trust=\"http://docs.oasis-open.org/ws-sx/ws-trust/200512\">" +
        "<trust:RequestSecurityTokenResponse><trust:RequestedSecurityToken>" + TokenXml +
        "</trust:RequestedSecurityToken></trust:RequestSecurityTokenResponse>" +
        "</trust:RequestSecurityTokenResponseCollection></s:Body></s:Envelope>";

    private static HttpResponseMessage Cevap(HttpStatusCode kod, string icerik) =>
        new(kod) { Content = new StringContent(icerik, Encoding.UTF8, "application/soap+xml") };

    /// <summary>İstek zarfındaki SOAP Action'dan metot adı.</summary>
    public static string Metot(string istekGovdesi) =>
        Regex.Match(istekGovdesi, @"<a:Action[^>]*>[^<]*/(?<m>\w+)</a:Action>").Groups["m"].Value;

    /// <summary>KPS cevap zarfı.</summary>
    public static string Zarf(string body) =>
        "<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\"><s:Body>" + body + "</s:Body></s:Envelope>";

    /// <summary>&lt;{metot}Response&gt;&lt;{metot}Result&gt;...  — <paramref name="ic"/>: HataBilgisi + Sonuc.</summary>
    public static string Sonuc(string metot, string ic) => Zarf(
        $"<{metot}Response xmlns=\"{Ns}\"><{metot}Result xmlns:i=\"http://www.w3.org/2001/XMLSchema-instance\">" +
        ic + $"</{metot}Result></{metot}Response>");

    public static string BosSonuc(string metot) => Sonuc(metot, "<HataBilgisi i:nil=\"true\"/><Sonuc i:nil=\"true\"/>");

    public static string Fault(string metin) =>
        "<s:Envelope xmlns:s=\"http://www.w3.org/2003/05/soap-envelope\"><s:Body><s:Fault>" +
        "<s:Code><s:Value>s:Receiver</s:Value></s:Code><s:Reason><s:Text xml:lang=\"tr-TR\">" + metin +
        "</s:Text></s:Reason></s:Fault></s:Body></s:Envelope>";

    public static KpsClient Istemci(SahteKps handler, Action<KpsOptions>? ayar = null)
    {
        var o = KpsOptions.PublicTestUser();
        o.RetryDelay = TimeSpan.Zero;
        ayar?.Invoke(o);
        return new KpsClient(new HttpClient(handler), o);
    }
}

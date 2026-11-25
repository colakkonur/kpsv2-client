using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;

namespace KpsV2;

/// <summary>
/// Sağlık Bakanlığı KPS'si (<see cref="KpsClient"/>) ve NVİ KPS'si (<see cref="Nvi.NviKpsClient"/>) için ortak SOAP 1.2 taşıma:
/// HTTP gönderimi, hata sınıflandırması, yeniden deneme ve WS-Security zaman damgası.
/// </summary>
internal static class SoapTransport
{
    internal const string SoapNs = "http://www.w3.org/2003/05/soap-envelope";
    internal const string AddrNs = "http://www.w3.org/2005/08/addressing";
    internal const string WsuNs = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-utility-1.0.xsd";
    internal const string WsseNs = "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd";
    internal const string TrustNs = "http://docs.oasis-open.org/ws-sx/ws-trust/200512";

    /// <summary>
    /// Zarfı gönderir; HTTP hatası, SOAP Fault veya XML olmayan cevapta <see cref="KpsException"/> fırlatır.
    /// <paramref name="connectionHint"/> bağlantı hatası mesajının sonuna eklenir (ör. IP kısıtı notu).
    /// </summary>
    internal static async Task<(string Raw, XDocument Doc)> SendAsync(
        HttpClient http, string url, string envelope, bool sts, string? connectionHint, CancellationToken ct)
    {
        using var content = new StringContent(envelope, Encoding.UTF8);
        content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/soap+xml; charset=utf-8");

        HttpResponseMessage resp;
        try
        {
            resp = await http.PostAsync(url, content, ct).ConfigureAwait(false);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            throw new KpsException($"KPS isteği zaman aşımına uğradı ({url}).", KpsErrorKind.Timeout, inner: ex);
        }
        catch (HttpRequestException ex)
        {
            throw new KpsException($"KPS'ye bağlanılamadı ({url}): {ex.Message}{connectionHint}", KpsErrorKind.Connection, inner: ex);
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

    /// <summary>Geçici hatalarda (<see cref="KpsException.IsTransient"/>) üstel beklemeyle yeniden dener.</summary>
    internal static async Task<T> WithRetryAsync<T>(
        Func<CancellationToken, Task<T>> cagri, int retryCount, TimeSpan retryDelay, ILogger logger, string metot, CancellationToken ct)
    {
        var sure = Stopwatch.StartNew();
        for (var deneme = 0; ; deneme++)
        {
            try
            {
                var sonuc = await cagri(ct).ConfigureAwait(false);
                logger.LogDebug("KPS {Metot} tamamlandı ({Sure} ms, {Deneme}. deneme)", metot, sure.ElapsedMilliseconds, deneme + 1);
                return sonuc;
            }
            catch (KpsException ex) when (ex.IsTransient && deneme < retryCount && !ct.IsCancellationRequested)
            {
                var bekle = TimeSpan.FromTicks(retryDelay.Ticks * (1L << deneme));
                logger.LogWarning("KPS {Metot} geçici hata ({Tur}), {Bekle} ms sonra tekrar denenecek ({Deneme}/{Toplam}): {Hata}",
                    metot, ex.Kind, (long)bekle.TotalMilliseconds, deneme + 1, retryCount, ex.SoapFault ?? ex.Message);
                await Task.Delay(bekle, ct).ConfigureAwait(false);
            }
        }
    }

    /// <summary>WS-Security zaman damgası: 1 dk geriden başlar (saat farkı payı), 5 dk geçerlidir.</summary>
    internal static (string Created, string Expires) Timestamp()
    {
        var now = DateTime.UtcNow;
        return (now.AddMinutes(-1).ToString("o", CultureInfo.InvariantCulture),
                now.AddMinutes(5).ToString("o", CultureInfo.InvariantCulture));
    }

    internal static string Esc(string? value) => SecurityElement.Escape(value ?? "") ?? "";
}

/// <summary>
/// STS token önbelleği: süresi dolana kadar (yenileme payı hariç) aynı token döner; eşzamanlı isteklerde STS'ye tek istek gider.
/// </summary>
internal sealed class TokenCache<T> where T : class
{
    private readonly SemaphoreSlim _kilit = new(1, 1);
    private readonly TimeSpan _yenilemePayi;
    private Entry? _token;

    private sealed record Entry(T Value, DateTimeOffset Bitis);

    internal TokenCache(TimeSpan yenilemePayi) => _yenilemePayi = yenilemePayi;

    internal async Task<T> GetAsync(Func<CancellationToken, Task<(T Value, DateTimeOffset Bitis)>> al, CancellationToken ct)
    {
        var t = Volatile.Read(ref _token);
        if (Gecerli(t)) return t!.Value;

        await _kilit.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            t = Volatile.Read(ref _token);
            if (Gecerli(t)) return t!.Value; // başka bir istek bu arada almış olabilir

            var (deger, bitis) = await al(ct).ConfigureAwait(false);
            Volatile.Write(ref _token, new Entry(deger, bitis));
            return deger;
        }
        finally
        {
            _kilit.Release();
        }
    }

    internal void Reset() => Volatile.Write(ref _token, null);

    private bool Gecerli(Entry? t) => t is not null && DateTimeOffset.UtcNow < t.Bitis - _yenilemePayi;
}

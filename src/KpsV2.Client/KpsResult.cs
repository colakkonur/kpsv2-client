using System.Xml.Linq;
using KpsV2.Contracts;

namespace KpsV2;

/// <summary>
/// KPS sorgu sonucu. <see cref="Data"/> WSDL'deki tipin tamamıdır; hiçbir alan kaybolmaz.
/// <para>
/// Ağ, yetki, kimlik doğrulama gibi hatalar <see cref="KpsException"/> olarak fırlatılır.
/// Servisin döndürdüğü iş hataları (ör. "kayıt bulunamadı") ise <see cref="Error"/> ve <see cref="InnerErrors"/>'da döner.
/// </para>
/// </summary>
public sealed class KpsResult<T> where T : class
{
    private IReadOnlyList<KpsInnerError>? _innerErrors;

    internal KpsResult(string method, T? data, KpsError? error, XElement xml)
    {
        Method = method;
        Data = data;
        Error = error;
        Xml = xml;
    }

    /// <summary>Çağrılan KPS metodu.</summary>
    public string Method { get; }

    /// <summary>Cevaptaki Sonuc (WSDL tipi). Kayıt yoksa veya hata varsa null olabilir.</summary>
    public T? Data { get; }

    /// <summary>Cevabın üst seviyesindeki veya Sonuc düğümündeki HataBilgisi (yoksa null).</summary>
    public KpsError? Error { get; }

    /// <summary>Hata yok ve Sonuc dolu.</summary>
    public bool IsSuccess => Error is null && Data is not null;

    /// <summary>
    /// Cevaptaki dolu HataBilgisi düğümlerinin tamamı, yollarıyla birlikte. KPS bazı alt kayıtlar için
    /// (ör. yerleşim yeri bulundu ama adres geçmişi yok) iç düğümlerde hata döndürebilir.
    /// </summary>
    public IReadOnlyList<KpsInnerError> InnerErrors => _innerErrors ??= CollectInnerErrors(Xml);

    /// <summary>Ham &lt;{Metot}Result&gt; düğümü. Kişisel veri içerir: loglamayın.</summary>
    public XElement Xml { get; }

    /// <summary>Başarılıysa <see cref="Data"/>'yı döner, değilse <see cref="KpsException"/> fırlatır.</summary>
    public T GetDataOrThrow() => IsSuccess
        ? Data!
        : throw new KpsException($"KPS {Method}: {(Error?.ToString() is { Length: > 0 } e ? e : "sonuç boş döndü")}",
            Error?.IsNotFound == true ? KpsErrorKind.NotFound : KpsErrorKind.ServiceError);

    private static IReadOnlyList<KpsInnerError> CollectInnerErrors(XElement root)
    {
        var ns = root.Name.Namespace; // Sağlık KPS'si veya NVİ: cevap düğümleri kök ile aynı namespace'tedir
        var list = new List<KpsInnerError>();
        foreach (var h in root.Descendants(ns + "HataBilgisi"))
        {
            var error = KpsError.From(h);
            if (error is null) continue;
            var path = string.Join("/", h.Ancestors().TakeWhile(a => a != root).Reverse().Select(a => a.Name.LocalName));
            list.Add(new KpsInnerError(path.Length == 0 ? "(root)" : path, error));
        }
        return list;
    }
}

/// <summary>Servisin döndürdüğü iş hatası (KPS: HataBilgisi { Kod, Aciklama }).</summary>
public sealed record KpsError(int? Code, string? Description)
{
    /// <summary>KPS'nin "kayıt bulunamadı" kodu.</summary>
    public const int NotFoundCode = 2;

    /// <summary>Kod 2: aranan kayıt yok.</summary>
    public bool IsNotFound => Code == NotFoundCode;

    /// <inheritdoc />
    public override string ToString() => Code is null ? Description ?? "" : $"[{Code}] {Description}";

    internal static KpsError? From(Parametre? p) => From(p?.Kod, p?.Aciklama);

    internal static KpsError? From(Nvi.Contracts.Parametre? p) => From(p?.Kod, p?.Aciklama);

    private static KpsError? From(int? code, string? description) =>
        code is null && string.IsNullOrEmpty(description) ? null : new KpsError(code, description);

    internal static KpsError? From(XElement h)
    {
        var ns = h.Name.Namespace;
        if (!h.HasElements) return null;
        var code = int.TryParse(h.Element(ns + "Kod")?.Value, out var k) ? k : (int?)null;
        var description = h.Element(ns + "Aciklama")?.Value;
        return code is null && string.IsNullOrEmpty(description) ? null : new KpsError(code, description);
    }
}

/// <summary>Cevabın iç düğümlerindeki bir HataBilgisi. <see cref="Path"/> ör. "Sonuc/TumAdresBilgileri/KisiAdresBilgisi".</summary>
public sealed record KpsInnerError(string Path, KpsError Error)
{
    /// <inheritdoc />
    public override string ToString() => $"{Path}: {Error}";
}

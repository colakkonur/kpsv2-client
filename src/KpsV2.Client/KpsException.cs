using System.Net;

namespace KpsV2;

/// <summary>Hatanın türü; çağıran tarafın karar vermesi (yeniden dene, kullanıcıya göster, alarm üret) için.</summary>
public enum KpsErrorKind
{
    /// <summary>Sınıflandırılamadı.</summary>
    Unknown,
    /// <summary>KPS'ye bağlanılamadı (ağ, DNS, TLS, IP kısıtı). Prod'da en sık neden: IP tanımlı değil.</summary>
    Connection,
    /// <summary>İstek zaman aşımına uğradı (bizim HttpClient'ımız veya KPS'nin arka servisi). Geçicidir.</summary>
    Timeout,
    /// <summary>KPS sunucu hatası (HTTP 5xx, ayrıntısız). Çoğunlukla geçicidir.</summary>
    Server,
    /// <summary>STS kullanıcı adı/şifre, kurum kodu veya uygulama kodunu reddetti.</summary>
    Authentication,
    /// <summary>Kurumun/kullanıcının bu metoda yetkisi yok (KPS Yönetim'den yetki istenmeli).</summary>
    Unauthorized,
    /// <summary>Cevap beklenen WSDL şemasına uymuyor.</summary>
    InvalidResponse,
    /// <summary>KpsOptions eksik/hatalı.</summary>
    Configuration,
    /// <summary><see cref="KpsResult{T}.GetDataOrThrow"/>: servis "kayıt bulunamadı" döndü.</summary>
    NotFound,
    /// <summary><see cref="KpsResult{T}.GetDataOrThrow"/>: servis başka bir HataBilgisi döndü.</summary>
    ServiceError,
}

/// <summary>KPS'ye ulaşılamadığında, STS/KPS SOAP hatası döndüğünde veya cevap okunamadığında fırlatılır.</summary>
public class KpsException : Exception
{
    /// <summary>Yeni bir KPS hatası.</summary>
    public KpsException(string message, KpsErrorKind kind = KpsErrorKind.Unknown, HttpStatusCode? httpStatusCode = null,
        string? soapFault = null, Exception? inner = null)
        : base(message, inner)
    {
        Kind = kind;
        HttpStatusCode = httpStatusCode;
        SoapFault = soapFault;
    }

    /// <summary>Hatanın türü.</summary>
    public KpsErrorKind Kind { get; }

    /// <summary>Sunucu cevap verdiyse HTTP durum kodu.</summary>
    public HttpStatusCode? HttpStatusCode { get; }

    /// <summary>SOAP Fault metni (ör. "Bu işlem için yetkiniz bulunmamaktadır.").</summary>
    public string? SoapFault { get; }

    /// <summary>Aynı istek biraz sonra tekrar denenirse başarılı olabilir mi?</summary>
    public bool IsTransient => Kind is KpsErrorKind.Connection or KpsErrorKind.Timeout or KpsErrorKind.Server;
}

/// <summary>KPS ayarları eksik veya hatalı.</summary>
public sealed class KpsConfigurationException : KpsException
{
    /// <summary>Yeni bir yapılandırma hatası.</summary>
    public KpsConfigurationException(string message) : base(message, KpsErrorKind.Configuration) { }
}

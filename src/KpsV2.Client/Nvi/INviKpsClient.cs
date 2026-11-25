using System.Xml.Linq;
using KpsV2.Nvi.Contracts;

namespace KpsV2.Nvi;

/// <summary>
/// NVİ (Nüfus ve Vatandaşlık İşleri) KPS istemcisi. Sağlık Bakanlığı KPS'sinden (<see cref="IKpsClient"/>) ayrı bir sistemdir.
/// Thread-safe'tir; DI'da singleton olarak kaydedilir (<c>services.AddNviKpsClient(...)</c>).
/// <para>
/// NVİ her servis için ayrı yetki verir; yetki verilmemiş bir servis <see cref="KpsException"/>
/// (<see cref="KpsErrorKind.Unauthorized"/>) ile sonuçlanır. Servisler NVİ'nin herkese açık WSDL'lerinden alınmıştır:
/// kpsbasvuru.nvi.gov.tr/Services/Wsdl.ashx?Service=&lt;servis adı&gt;.
/// </para>
/// </summary>
public interface INviKpsClient
{
    /// <summary>
    /// TC kimlik numarası, ad, soyad ve doğum yılının nüfus kaydıyla eşleşip eşleşmediğini sorar (NVİ: TCKimlikNoDogrulaServis).
    /// Eşleşmezse <see cref="KpsResult{T}.Error"/> veya <c>Data.HataBilgisi</c> dolu döner.
    /// </summary>
    Task<KpsResult<TCKimlikNoDogrulama>> TCKimlikNoDogrulaAsync(long identityNumber, string firstName, string lastName, int birthYear,
        CancellationToken ct = default);

    /// <summary>Birden çok kişiyi tek istekte doğrular. Kişi bazındaki hatalar her elemanın <c>HataBilgisi</c>'ndedir.</summary>
    Task<KpsResult<TCKimlikNoDogrulama[]>> TCKimlikNoDogrulaAsync(IEnumerable<TCKimlikNoDogrulaSorguKriteri> criteria,
        CancellationToken ct = default);

    /// <summary>
    /// TC kimlik numarasıyla kişinin temel, durum ve kayıt yeri bilgileri (NVİ: KisiSorgulaTCKimlikNoServis).
    /// </summary>
    Task<KpsResult<KisiBilgisi>> KisiSorgulaTCKimlikNoAsync(long identityNumber, CancellationToken ct = default);

    /// <summary>Birden çok kişiyi tek istekte sorgular. Kişi bazındaki hatalar her elemanın <c>HataBilgisi</c>'ndedir.</summary>
    Task<KpsResult<KisiBilgisi[]>> KisiSorgulaTCKimlikNoAsync(IEnumerable<long> identityNumbers, CancellationToken ct = default);

    /// <summary>
    /// Paketin tipli olarak sunmadığı NVİ servisleri için ham çağrı. Token, imza, yeniden deneme ve hata sınıflandırması uygulanır.
    /// <paramref name="service"/>: NVİ servis adı (ör. "TCKimlikNoDogrulaServis"); <paramref name="method"/>: işlem (ör. "ListeleCoklu");
    /// <paramref name="bodyXml"/>: işlem elemanının iç XML'i (NVİ namespace'inde, ön eksiz).
    /// </summary>
    Task<XDocument> CallAsync(string service, string method, string bodyXml, CancellationToken ct = default);
}

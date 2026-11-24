using System.Xml.Linq;
using KpsV2.Contracts;

namespace KpsV2;

/// <summary>
/// KPS v2 istemcisi: Kps Servisi (16 metot) ve Parametre Servisi (2 metot), WSDL tipleriyle.
/// Thread-safe'tir; DI'da singleton olarak kaydedilir (<c>services.AddKpsClient(...)</c>).
/// STS token'ı süresi dolana kadar önbellekte tutulur ve eşzamanlı isteklerde tek sefer alınır.
/// <para>
/// Her metot için kurumun KPS yetkisi olmalıdır; yoksa <see cref="KpsException"/> (<see cref="KpsErrorKind.Unauthorized"/>) fırlatılır.
/// Yetkiler <see cref="YetkiListesiAsync"/> ile görülebilir. Yetkisi olmayan alanlar ise null döner.
/// </para>
/// </summary>
public interface IKpsClient
{
    // ---------------- Kps Servisi: kişi ----------------

    /// <summary>
    /// Kişinin kütük bilgisi. Kimlik türüne göre TCVatandasiKisiKutukleri, YabanciKisiKutukleri veya
    /// MaviKartliKisiKutukleri'nden yalnızca biri dolu gelir; kişi bulunamazsa üçü de null'dır.
    /// Ortak alanlar için <c>Data.ToSummary()</c>.
    /// </summary>
    Task<KpsResult<BilesikKisiBilgisi>> BilesikKisiSorgulaAsync(long identityNumber, CancellationToken ct = default);

    /// <summary>BilesikKisiSorgula + kişinin adresi (<see cref="BilesikKisiveAdresBilgisi.AdresBilgisi"/>).</summary>
    Task<KpsResult<BilesikKisiveAdresBilgisi>> BilesikKisiveAdresSorgulaAsync(long identityNumber, CancellationToken ct = default);

    /// <summary>BilesikKisiSorgula + yerleşim yeri ve adres geçmişi (<see cref="BilesikKisiveYerlesimYeriBilgisi.AdresBilgisi"/>).</summary>
    Task<KpsResult<BilesikKisiveYerlesimYeriBilgisi>> BilesikKisiveYerlesimYeriSorgulaAsync(long identityNumber, CancellationToken ct = default);

    /// <summary>
    /// Ad, soyad, anne/baba adı, cinsiyet, doğum yeri ve doğum tarihine uyan T.C. vatandaşını bulur.
    /// Bulunamazsa <see cref="KpsResult{T}.Error"/> Kod 2 döner.
    /// </summary>
    Task<KpsResult<TCKisiBilgisi>> TcKimlikNoSorgulaAsync(KisiSorgulaKisiBilgileriCO criteria, CancellationToken ct = default);

    // ---------------- Kps Servisi: adres ----------------

    /// <summary>Kişinin adres bilgisi (açık adres, adres tipi, il/ilçe/köy/belde/yurt dışı ayrıntıları).</summary>
    Task<KpsResult<KisiAdresBilgisi>> KimlikNoIleAdresBilgisiSorgulaAsync(long identityNumber, CancellationToken ct = default);

    /// <summary>Kişinin yerleşim yeri adresi ve tüm adres geçmişi.</summary>
    Task<KpsResult<TcKimlikNoIleAdresBilgisi>> KimlikNoIleYerlesimYeriSorgulaAsync(long identityNumber, CancellationToken ct = default);

    /// <summary>Adres numarasına (UAVT) göre açık adres.</summary>
    Task<KpsResult<AcikAdres>> AdresNoIleYerlesimYeriSorgulaAsync(long addressNumber, CancellationToken ct = default);

    // ---------------- Kps Servisi: nüfus kaydı ve aile ----------------

    /// <summary>Kişi tipine göre Vatandaş (<see cref="GenelNufusKayitOrnegi.NkoVatandas"/>) veya Mavi Kartlı nüfus kayıt örneği.</summary>
    Task<KpsResult<GenelNufusKayitOrnegi>> KimlikNoIleNufusKayitOrnegiSorgulaAsync(long identityNumber, CancellationToken ct = default);

    /// <summary>Kişinin aile bireyleri. <see cref="AileBireyleriSorgulaKriter.Tip"/>: Aile veya TumAile.</summary>
    Task<KpsResult<AileBilgisi>> AileBireyleriSorgulaAsync(AileBireyleriSorgulaKriter criteria, CancellationToken ct = default);

    /// <summary>T.C. vatandaşının aile bilgisi (anne, baba, eş, çocuklar). Kriterde kişinin doğum tarihi doğrulama için istenir.</summary>
    Task<KpsResult<YakinlikServisiAileBilgisi>> KimlikNoIleVatandasAileBilgisiSorgulaAsync(YakinlikSorgulaKriter criteria, CancellationToken ct = default);

    /// <summary><see cref="KimlikNoIleVatandasAileBilgisiSorgulaAsync"/> + kardeşler.</summary>
    Task<KpsResult<YakinlikServisiAileBilgisi>> KimlikNoIleVatandasAileBilgisiKardesDahilSorgulaAsync(YakinlikSorgulaKriter criteria, CancellationToken ct = default);

    /// <summary>Mavi kartlı kişinin aile bilgisi.</summary>
    Task<KpsResult<MaviKartliAileBilgisi>> KimlikNoIleMaviKartliAileBilgisiSorgulaAsync(YakinlikSorgulaKriter criteria, CancellationToken ct = default);

    /// <summary><see cref="KimlikNoIleMaviKartliAileBilgisiSorgulaAsync"/> + kardeşler.</summary>
    Task<KpsResult<MaviKartliAileBilgisi>> KimlikNoIleMaviKartliAileBilgisiKardesDahilSorgulaAsync(YakinlikSorgulaKriter criteria, CancellationToken ct = default);

    /// <summary>T.C. vatandaşının yakınlık dereceli aile bilgisi (kamu kurumları için).</summary>
    Task<KpsResult<KamuYakinlikServisiAileBilgisi>> KimlikNoIleVatandasAileYakinlikBilgisiSorgulaAsync(KamuYakinlikSorgulaKriter criteria, CancellationToken ct = default);

    /// <summary><see cref="KimlikNoIleVatandasAileYakinlikBilgisiSorgulaAsync"/> + kardeşler.</summary>
    Task<KpsResult<KamuYakinlikServisiAileBilgisi>> KimlikNoIleVatandasAileYakinlikBilgisiKardesDahilSorgulaAsync(KamuYakinlikSorgulaKriter criteria, CancellationToken ct = default);

    // ---------------- Kps Servisi: yetki ----------------

    /// <summary>
    /// Kurumun/kullanıcının servis ve metot bazında yetkileri (alan bazında ayrıntıyla).
    /// Düz liste için <c>Data.MethodPermissions()</c>, alan bazında <c>Data.FieldPermissions(metot)</c>.
    /// </summary>
    Task<KpsResult<WcfService[]>> YetkiListesiAsync(CancellationToken ct = default);

    // ---------------- Parametre Servisi ----------------

    /// <summary>Parametre servisindeki tüm parametre adları.</summary>
    Task<KpsResult<string[]>> ParametreAdListesiAsync(CancellationToken ct = default);

    /// <summary>Bir parametrenin değerleri (kod + açıklama). Parametre adları <see cref="ParametreAdListesiAsync"/>'ten.</summary>
    Task<KpsResult<Parametre[]>> ParametreDegerleriAsync(string parameterName, CancellationToken ct = default);

    // ---------------- Gelişmiş ----------------

    /// <summary>
    /// Router üzerinden herhangi bir metodu çağırır ve ham SOAP cevabını döner (bu istemcide tipli karşılığı olmayan
    /// servisler için: Uavt, Gizlilik, Olay, Degisim...). Token, yeniden deneme ve hata sınıflandırması uygulanır.
    /// <paramref name="service"/>: sözleşme adı (ör. <see cref="KpsService.Kps"/>);
    /// <paramref name="bodyXml"/>: metot elemanının iç XML'i (KPS namespace'inde, ön eksiz).
    /// </summary>
    Task<XDocument> CallAsync(string service, string method, string bodyXml, CancellationToken ct = default);
}

/// <summary>Router arkasındaki servis sözleşmeleri (SOAP Action'da kullanılır).</summary>
public static class KpsService
{
    /// <summary>Kps servisi (kişi, adres, aile...).</summary>
    public const string Kps = "IKpsServices";

    /// <summary>Parametre servisi.</summary>
    public const string Parameter = "IParametreServices";
}

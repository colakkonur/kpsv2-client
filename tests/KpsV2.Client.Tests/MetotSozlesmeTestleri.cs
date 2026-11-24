using System.Collections;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using KpsV2.Contracts;

namespace KpsV2.Client.Tests;

/// <summary>
/// 18 metodun hepsi için: doğru servis/SOAP Action ile çağrıldığını, istek gövdesinin WSDL'e uyduğunu ve
/// WSDL tipinin tüm alanları dolu bir cevabın kayıpsız okunduğunu doğrular.
/// </summary>
public class MetotSozlesmeTestleri
{
    private const long TcNo = 10774881040;

    public sealed record Durum(
        string Metot, string Servis, Type ZarfTipi, Type SonucTipi,
        Func<IKpsClient, Task<(object? Data, KpsError? Error)>> Cagir, string BeklenenGovde)
    {
        public override string ToString() => Metot;
    }

    private static Durum D<TZarf, T>(string metot, Func<IKpsClient, Task<KpsResult<T>>> cagir, string govde, string servis = KpsService.Kps)
        where T : class =>
        new(metot, servis, typeof(TZarf), typeof(T), async k => { var s = await cagir(k); return (s.Data, s.Error); }, govde);

    private static readonly DateOnly Dogum = new(1929, 7, 1);
    private const string KimlikNoGovde = "<kimlikNo>10774881040</kimlikNo>";
    private const string YakinlikGovde = "<DogumAy>7</DogumAy><DogumGun>1</DogumGun><DogumYil>1929</DogumYil><KimlikNo>10774881040</KimlikNo>";

    private static readonly Durum[] Durumlar =
    {
        D<KpsServisSonucuBilesikKisiBilgisi, BilesikKisiBilgisi>("BilesikKisiSorgula", k => k.BilesikKisiSorgulaAsync(TcNo), KimlikNoGovde),
        D<KpsServisSonucuBilesikKisiveAdresBilgisi, BilesikKisiveAdresBilgisi>("BilesikKisiveAdresSorgula", k => k.BilesikKisiveAdresSorgulaAsync(TcNo), KimlikNoGovde),
        D<KpsServisSonucuBilesikKisiveYerlesimYeriBilgisi, BilesikKisiveYerlesimYeriBilgisi>("BilesikKisiveYerlesimYeriSorgula", k => k.BilesikKisiveYerlesimYeriSorgulaAsync(TcNo), KimlikNoGovde),
        D<KpsServisSonucuKisiTemelBilgisi, TCKisiBilgisi>("TcKimlikNoSorgula",
            k => k.TcKimlikNoSorgulaAsync(KpsCriteria.Person("AHMET", "YILMAZ", "MEHMET", "AYŞE", ECinsiyet.ERKEK, new DateOnly(1980, 3, 15), "ANKARA")),
            "<Adi>AHMET</Adi><AnneAdi>AYŞE</AnneAdi><BabaAdi>MEHMET</BabaAdi><Cinsiyet>ERKEK</Cinsiyet><DogumTarihi>1980-03-15T00:00:00</DogumTarihi><DogumYeri>ANKARA</DogumYeri><Soyadi>YILMAZ</Soyadi>"),
        D<KpsServisSonucuKisiAdresBilgisi, KisiAdresBilgisi>("KimlikNoIleAdresBilgisiSorgula", k => k.KimlikNoIleAdresBilgisiSorgulaAsync(TcNo), KimlikNoGovde),
        D<KpsServisSonucuKisiYerlesimYeriBilgisi, TcKimlikNoIleAdresBilgisi>("KimlikNoIleYerlesimYeriSorgula", k => k.KimlikNoIleYerlesimYeriSorgulaAsync(TcNo), KimlikNoGovde),
        D<KpsServisSonucuAksAcikAdresServisBilgisi, AcikAdres>("AdresNoIleYerlesimYeriSorgula", k => k.AdresNoIleYerlesimYeriSorgulaAsync(1234567890), "<adresNo>1234567890</adresNo>"),
        D<KpsServisSonucuGenelNufusKayitOrnegi, GenelNufusKayitOrnegi>("KimlikNoIleNufusKayitOrnegiSorgula", k => k.KimlikNoIleNufusKayitOrnegiSorgulaAsync(TcNo), KimlikNoGovde),
        D<KpsServisSonucuAileBilgisi, AileBilgisi>("AileBireyleriSorgula", k => k.AileBireyleriSorgulaAsync(KpsCriteria.FamilyMembers(TcNo, wholeFamily: true)),
            "<TCKimlikNo>10774881040</TCKimlikNo><Tip>TumAile</Tip>"),
        D<KpsServisSonucuVatandasAileBilgisi, YakinlikServisiAileBilgisi>("KimlikNoIleVatandasAileBilgisiSorgula",
            k => k.KimlikNoIleVatandasAileBilgisiSorgulaAsync(KpsCriteria.Kinship(TcNo, Dogum)), YakinlikGovde),
        D<KpsServisSonucuVatandasAileBilgisi, YakinlikServisiAileBilgisi>("KimlikNoIleVatandasAileBilgisiKardesDahilSorgula",
            k => k.KimlikNoIleVatandasAileBilgisiKardesDahilSorgulaAsync(KpsCriteria.Kinship(TcNo, Dogum)), YakinlikGovde),
        D<KpsServisSonucuMaviKartliAileBilgisi, MaviKartliAileBilgisi>("KimlikNoIleMaviKartliAileBilgisiSorgula",
            k => k.KimlikNoIleMaviKartliAileBilgisiSorgulaAsync(KpsCriteria.Kinship(TcNo, Dogum)), YakinlikGovde),
        D<KpsServisSonucuMaviKartliAileBilgisi, MaviKartliAileBilgisi>("KimlikNoIleMaviKartliAileBilgisiKardesDahilSorgula",
            k => k.KimlikNoIleMaviKartliAileBilgisiKardesDahilSorgulaAsync(KpsCriteria.Kinship(TcNo, Dogum)), YakinlikGovde),
        D<KpsServisSonucuKamuYakinlikAileBilgisi, KamuYakinlikServisiAileBilgisi>("KimlikNoIleVatandasAileYakinlikBilgisiSorgula",
            k => k.KimlikNoIleVatandasAileYakinlikBilgisiSorgulaAsync(KpsCriteria.PublicKinship(TcNo, Dogum)), YakinlikGovde),
        D<KpsServisSonucuKamuYakinlikAileBilgisi, KamuYakinlikServisiAileBilgisi>("KimlikNoIleVatandasAileYakinlikBilgisiKardesDahilSorgula",
            k => k.KimlikNoIleVatandasAileYakinlikBilgisiKardesDahilSorgulaAsync(KpsCriteria.PublicKinship(TcNo, Dogum)), YakinlikGovde),
        D<KpsServisSonucuYetkiListesi, WcfService[]>("YetkiListesi", k => k.YetkiListesiAsync(), ""),
        D<KpsServisSonucuParametreListesi, string[]>("ParametreAdListesi", k => k.ParametreAdListesiAsync(), "", KpsService.Parameter),
        D<KpsServisSonucuParametreDegerleri, Parametre[]>("ParametreDegerleri", k => k.ParametreDegerleriAsync("Cinsiyet"),
            "<parametreAdi>Cinsiyet</parametreAdi>", KpsService.Parameter),
    };

    public static TheoryData<Durum> Metotlar => new(Durumlar);

    [Fact]
    public void Arayuzdeki_tum_KPS_metotlari_test_ediliyor()
    {
        var arayuz = typeof(IKpsClient).GetMethods().Select(m => m.Name).Where(n => n != nameof(IKpsClient.CallAsync))
            .Select(n => n[..^"Async".Length]).OrderBy(n => n);
        Assert.Equal(arayuz, Durumlar.Select(d => d.Metot).OrderBy(n => n));
        Assert.Equal(18, arayuz.Count());
    }

    [Theory]
    [MemberData(nameof(Metotlar))]
    public async Task Istek_dogru_action_ve_govde_ile_gonderilir(Durum d)
    {
        var h = new SahteKps();
        await d.Cagir(SahteKps.Istemci(h));

        var govde = h.SonKpsGovdesi;
        Assert.Contains($"<a:Action s:mustUnderstand=\"1\">{SahteKps.Ns}/{d.Servis}/{d.Metot}</a:Action>", govde);

        var metotEl = XDocument.Parse(govde).Descendants(XName.Get(d.Metot, SahteKps.Ns)).Single();
        Assert.Equal(Normalize(d.BeklenenGovde), Normalize(IcGovde(metotEl)));
    }

    [Theory]
    [MemberData(nameof(Metotlar))]
    public async Task Tum_alanlari_dolu_cevap_kayipsiz_okunur(Durum d)
    {
        var beklenen = OrnekNesne.Olustur(d.SonucTipi);
        d.SonucTipi.GetProperty("HataBilgisi")?.SetValue(beklenen, null); // iç hatalar ayrıca test ediliyor
        var zarf = Activator.CreateInstance(d.ZarfTipi)!;
        d.ZarfTipi.GetProperty("Sonuc")!.SetValue(zarf, beklenen);
        var h = new SahteKps { KpsCevabi = _ => (HttpStatusCode.OK, CevapZarfi(d.Metot, zarf)) };

        var (okunan, hata) = await d.Cagir(SahteKps.Istemci(h));

        Assert.Null(hata);
        Assert.NotNull(okunan);
        Assert.Equal(Xml(d.SonucTipi, beklenen), Xml(d.SonucTipi, okunan!));
        Assert.True(OrnekNesne.AlanSayisi(okunan) > 0);
    }

    [Theory]
    [MemberData(nameof(Metotlar))]
    public async Task Ust_seviye_HataBilgisi_Hata_alanina_tasinir(Durum d)
    {
        var cevap = SahteKps.Sonuc(d.Metot,
            "<HataBilgisi><Aciklama>Aradığınız kriterlere uygun kayıt bulunamadı.</Aciklama><Kod>2</Kod></HataBilgisi><Sonuc i:nil=\"true\"/>");
        var h = new SahteKps { KpsCevabi = _ => (HttpStatusCode.OK, cevap) };
        var (okunan, hata) = await d.Cagir(SahteKps.Istemci(h));

        Assert.Null(okunan);
        Assert.Equal(new KpsError(2, "Aradığınız kriterlere uygun kayıt bulunamadı."), hata);
        Assert.True(hata!.IsNotFound);
    }

    // ---------------- yardımcılar ----------------

    private static string CevapZarfi(string metot, object zarf)
    {
        var sb = new StringBuilder();
        using (var w = XmlWriter.Create(sb, new XmlWriterSettings { OmitXmlDeclaration = true }))
            new DataContractSerializer(zarf.GetType(), metot + "Result", SahteKps.Ns).WriteObject(w, zarf);
        return SahteKps.Zarf($"<{metot}Response xmlns=\"{SahteKps.Ns}\">{sb}</{metot}Response>");
    }

    private static string Xml(Type tip, object nesne)
    {
        var sb = new StringBuilder();
        using (var w = XmlWriter.Create(sb, new XmlWriterSettings { OmitXmlDeclaration = true }))
            new DataContractSerializer(tip, "Sonuc", SahteKps.Ns).WriteObject(w, nesne);
        return sb.ToString();
    }

    private static string IcGovde(XElement e)
    {
        // Kriter elemanı varsa onun çocukları, yoksa metot elemanının çocukları; namespace'ler atılır.
        var kriter = e.Element(XName.Get("kriter", SahteKps.Ns));
        var kaynak = kriter ?? e;
        return string.Concat(kaynak.Elements().Select(x => $"<{x.Name.LocalName}>{x.Value}</{x.Name.LocalName}>"));
    }

    private static string Normalize(string s) => s.Replace(" ", "");
}

/// <summary>Bir DataContract tipini, tüm alanları (iç içe, diziler dahil) dolu bir örnekle oluşturur.</summary>
internal static class OrnekNesne
{
    public static object Olustur(Type tip) => Deger(tip, 0, "Kok")!;

    private static object? Deger(Type tip, int derinlik, string ad)
    {
        var t = Nullable.GetUnderlyingType(tip) ?? tip;
        if (t == typeof(string)) return $"{ad}_ÇĞİÖŞÜ";
        if (t == typeof(long)) return 10774881040L + derinlik;
        if (t == typeof(int)) return 7 + derinlik;
        if (t == typeof(bool)) return true;
        if (t == typeof(DateTime)) return new DateTime(2020, 1, 2, 0, 0, 0, DateTimeKind.Unspecified);
        if (t == typeof(decimal)) return 1.5m;
        if (t == typeof(double)) return 1.5d;
        if (t.IsEnum) return Enum.GetValues(t).Cast<object>().Last();
        if (t.IsArray)
        {
            var eleman = t.GetElementType()!;
            var dizi = Array.CreateInstance(eleman, 1);
            dizi.SetValue(Deger(eleman, derinlik + 1, ad), 0);
            return dizi;
        }
        if (derinlik > 8 || t.GetCustomAttribute<DataContractAttribute>() is null) return null;

        var nesne = Activator.CreateInstance(t)!;
        foreach (var p in Uyeler(t))
            p.SetValue(nesne, Deger(p.PropertyType, derinlik + 1, p.Name));
        return nesne;
    }

    /// <summary>Nesnede (iç içe) dolu yaprak alan sayısı.</summary>
    public static int AlanSayisi(object? o, int derinlik = 0)
    {
        if (o is null || derinlik > 10) return 0;
        var t = o.GetType();
        if (t.IsPrimitive || o is string or DateTime or Enum or decimal) return 1;
        if (o is IEnumerable e) return e.Cast<object?>().Sum(x => AlanSayisi(x, derinlik + 1));
        return Uyeler(t).Sum(p => AlanSayisi(p.GetValue(o), derinlik + 1));
    }

    private static IEnumerable<PropertyInfo> Uyeler(Type t) =>
        t.GetProperties().Where(p => p.CanWrite && p.GetCustomAttribute<DataMemberAttribute>() is not null);
}

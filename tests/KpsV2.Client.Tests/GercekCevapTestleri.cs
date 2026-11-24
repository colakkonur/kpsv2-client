using System.Net;
using KpsV2.Contracts;

namespace KpsV2.Client.Tests;

/// <summary>
/// kpsv2test'ten alınmış gerçek cevaplar (Ornekler/*.xml, herkese açık test verisi) üzerinden ayrıştırma testleri.
/// Sentetik testlerin göremeyeceği şeyleri yakalar: gerçek eleman sırası, nil kullanımı, beklenmeyen alanlar.
/// </summary>
public class GercekCevapTestleri
{
    private static async Task<SahteKps> Sunucu(string dosya)
    {
        var govde = await File.ReadAllTextAsync(Path.Combine(AppContext.BaseDirectory, "Ornekler", dosya));
        return new SahteKps { KpsCevabi = _ => (HttpStatusCode.OK, SahteKps.Zarf(govde)) };
    }

    [Fact]
    public async Task Bulunamayan_kisi_bos_kutuklerle_doner()
    {
        var s = await SahteKps.Istemci(await Sunucu("BilesikKisiSorgula_yok.xml")).BilesikKisiSorgulaAsync(10000000146);

        Assert.Null(s.Error);
        Assert.Equal(10000000146, s.Data!.KimlikNo);
        Assert.Null(s.Data.TCVatandasiKisiKutukleri);
        Assert.Null(s.Data.YabanciKisiKutukleri);
        Assert.Null(s.Data.MaviKartliKisiKutukleri);
        Assert.Null(s.Data.ToSummary());
    }

    [Fact]
    public async Task Yurt_disi_yerlesim_yeri_ve_ic_hata_okunur()
    {
        var s = await SahteKps.Istemci(await Sunucu("KimlikNoIleYerlesimYeriSorgula_mk.xml")).KimlikNoIleYerlesimYeriSorgulaAsync(14233775406);

        Assert.True(s.IsSuccess, s.Error?.ToString());
        var adres = s.Data!.YerlesimYeriAdresi;
        Assert.Equal("st.45 KÖLN / ALMANYA FEDERAL CUMHURIYETI", adres.AcikAdres);
        Assert.Equal(4, adres.AdresTip.Kod);
        Assert.Equal("KÖLN", adres.YurtDisiAdresi.YabanciSehir);
        Assert.Equal("ALMANYA FEDERAL CUMHURIYETI", adres.YurtDisiAdresi.YabanciUlke.Aciklama);
        Assert.True(adres.BeyanTarihi.IsEmpty);

        var ic = Assert.Single(s.InnerErrors);
        Assert.Equal("Sonuc/TumAdresBilgileri/KisiAdresBilgisi", ic.Path);
        Assert.Equal(new KpsError(2, "KayitBulunamadi"), ic.Error);
    }

    [Fact]
    public async Task Bos_sonuc_hatasiz_null_doner()
    {
        var s = await SahteKps.Istemci(await Sunucu("KimlikNoIleYerlesimYeriSorgula_tc.xml")).KimlikNoIleYerlesimYeriSorgulaAsync(10774881040);

        Assert.Null(s.Error);
        Assert.Null(s.Data);
        Assert.False(s.IsSuccess);
        Assert.Throws<KpsException>(() => s.GetDataOrThrow());
    }

    [Fact]
    public async Task Vatandas_aile_bilgisi_yakinlik_dereceleriyle_okunur()
    {
        var kps = SahteKps.Istemci(await Sunucu("KimlikNoIleVatandasAileBilgisiKardesDahilSorgula_tc.xml"));
        var s = await kps.KimlikNoIleVatandasAileBilgisiKardesDahilSorgulaAsync(KpsCriteria.Kinship(26563360832, new DateOnly(1976, 3, 8)));

        Assert.True(s.IsSuccess, s.Error?.ToString());
        var bireyler = s.Data!.AileKisiListesi;
        Assert.Equal(5, bireyler.Length);
        var kendisi = Assert.Single(bireyler, b => b.Yakinlik.Aciklama == "Kendisi");
        Assert.Equal(26563360832, kendisi.TCKimlikNo);
        Assert.Equal("YEŞİM", kendisi.TemelBilgisi.Ad);
        Assert.Equal(new DateOnly(1976, 3, 8), kendisi.TemelBilgisi.DogumTarih.Date);
        // Test verisinde geçersiz ölüm tarihi (33.13.2017) var: TamTarih null, ham değerler korunur.
        Assert.Null(kendisi.DurumBilgisi.OlumTarih.Date);
        Assert.Equal("33.13.2017", kendisi.DurumBilgisi.OlumTarih.ToString());
        var baba = Assert.Single(bireyler, b => b.Yakinlik.Aciklama == "Babası");
        Assert.Equal(10774881040, baba.TCKimlikNo);
        Assert.Equal(3, bireyler.Count(b => b.Yakinlik.Aciklama == "Kardeşi"));
    }

    [Fact]
    public async Task Kardes_haric_aile_bilgisi_daha_az_kisi_icerir()
    {
        var kps = SahteKps.Istemci(await Sunucu("KimlikNoIleVatandasAileBilgisiSorgula_tc.xml"));
        var s = await kps.KimlikNoIleVatandasAileBilgisiSorgulaAsync(KpsCriteria.Kinship(26563360832, new DateOnly(1976, 3, 8)));

        Assert.True(s.IsSuccess);
        Assert.Equal(2, s.Data!.AileKisiListesi.Length);
        Assert.DoesNotContain(s.Data.AileKisiListesi, b => b.Yakinlik.Aciklama == "Kardeşi");
    }

    [Fact]
    public async Task TC_kimlik_no_sorgusu_bulunan_kisiyi_doner()
    {
        var kps = SahteKps.Istemci(await Sunucu("TcKimlikNoSorgula_tc.xml"));
        var s = await kps.TcKimlikNoSorgulaAsync(KpsCriteria.Person("YEŞİM", "YELBAY", "İZZET", "GÜZİDE", ECinsiyet.KADIN, new DateOnly(1976, 3, 8), "ADANA"));

        Assert.True(s.IsSuccess, s.Error?.ToString());
        Assert.Equal(26563360832, s.Data!.TCKimlikNo);
        var ozet = s.Data.ToSummary()!;
        Assert.Equal("YEŞİM YELBAY", ozet.FullName);
        Assert.Equal(PersonType.TurkishCitizen, ozet.Type);
    }

    [Fact]
    public async Task TC_kimlik_no_sorgusu_bulunamazsa_Hata_doner()
    {
        var kps = SahteKps.Istemci(await Sunucu("TcKimlikNoSorgula_yok.xml"));
        var s = await kps.TcKimlikNoSorgulaAsync(KpsCriteria.Person("YOK", "YOK", "YOK", "YOK", ECinsiyet.KADIN, new DateOnly(1976, 3, 8), "ADANA"));

        Assert.False(s.IsSuccess);
        Assert.True(s.Error!.IsNotFound);
        Assert.Equal("Aradığınız kriterlere uygun kayıt bulunamadı.", s.Error.Description);
        Assert.Null(s.Data.ToSummary());
    }

    [Fact]
    public async Task Gercek_yetki_listesi_metot_ve_alan_bazinda_okunur()
    {
        var s = await SahteKps.Istemci(await Sunucu("YetkiListesi.xml")).YetkiListesiAsync();

        Assert.True(s.IsSuccess);
        var metotlar = s.Data.MethodPermissions().Where(y => y.Service == "Kps Servisleri").ToList();
        // Test kullanıcısının listesinde 16 metodun 13'ü var; şu üçü hiç listelenmiyor (24.11.2026 itibarıyla).
        Assert.Equal(13, metotlar.Count);
        Assert.DoesNotContain(metotlar, y => y.Method is "AdresNoIleYerlesimYeriSorgula"
            or "KimlikNoIleVatandasAileYakinlikBilgisiSorgula" or "KimlikNoIleVatandasAileYakinlikBilgisiKardesDahilSorgula");
        Assert.Contains(new KpsPermission("Kps Servisleri", "BilesikKisiSorgula", true), metotlar);
        Assert.Contains(new KpsPermission("Kps Servisleri", "KimlikNoIleAdresBilgisiSorgula", false), metotlar);
        Assert.False(s.Data.IsAuthorized("ParametreDegerleri"));

        var alanlar = s.Data.FieldPermissions("BilesikKisiSorgula");
        Assert.Contains(alanlar, a => a.Path == "TCVatandasiKisiKutukleri/KisiBilgisi/TemelBilgisi/Ad");
        Assert.All(alanlar, a => Assert.False(string.IsNullOrEmpty(a.Type)));
    }
}

using KpsV2.Contracts;
using KpsV2.Nvi;

namespace KpsV2.Client.Tests;

/// <summary>
/// docs/kullanim.md içindeki kod örneklerinin birebir kopyası: API değişip doküman eskirse derleme kırılır.
/// Sahte sunucu boş cevap döndüğü için yalnızca derlenme ve akış (istisna fırlatmama) kontrol edilir.
/// </summary>
public class DokumanOrnekleriTestleri
{
    private readonly IKpsClient kps = SahteKps.Istemci(new SahteKps());

    private sealed class PatientIdentityService(IKpsClient kps)
    {
        public async Task<PersonSummary?> GetAsync(long identityNumber, CancellationToken ct)
        {
            var result = await kps.BilesikKisiSorgulaAsync(identityNumber, ct);
            if (!result.IsSuccess)
                return null;

            return result.Data.ToSummary();
        }
    }

    [Fact]
    public async Task Kullanim_ornegi()
    {
        Assert.Null(await new PatientIdentityService(kps).GetAsync(10774881040, default));
    }

    [Fact]
    public async Task Kriterli_sorgu_ornekleri()
    {
        var s1 = await kps.TcKimlikNoSorgulaAsync(
            KpsCriteria.Person("AHMET", "YILMAZ", "MEHMET", "AYŞE", ECinsiyet.ERKEK, new DateOnly(1980, 3, 15), "ANKARA"));
        long? identityNumber = s1.Data?.TCKimlikNo ?? 10774881040;

        var s2 = await kps.KimlikNoIleVatandasAileBilgisiKardesDahilSorgulaAsync(KpsCriteria.Kinship(identityNumber!.Value, new DateOnly(1980, 3, 15)));
        foreach (var member in s2.Data?.AileKisiListesi ?? [])
            _ = member.Yakinlik?.Aciklama;
    }

    [Fact]
    public async Task Tipli_alan_erisimi_ornegi()
    {
        var result = await kps.BilesikKisiSorgulaAsync(10774881040);
        var tc = result.Data?.TCVatandasiKisiKutukleri;
        _ = tc?.KisiBilgisi?.KayitYeriBilgisi?.Ilce?.Aciklama;
        _ = tc?.TCKKBilgisi?.SeriNo;

        await Assert.ThrowsAsync<KpsException>(async () =>
            _ = (await kps.KimlikNoIleYerlesimYeriSorgulaAsync(10774881040)).GetDataOrThrow().YerlesimYeriAdresi.AcikAdres);

        _ = (await kps.YetkiListesiAsync()).Data.FieldPermissions("BilesikKisiSorgula");
    }

    // ---------------- 8. NVİ KPS ----------------

    private readonly INviKpsClient nvi = SahteNvi.Istemci(new SahteNvi());

    private sealed class PatientVerificationService(INviKpsClient nvi)
    {
        public async Task<bool> VerifyAsync(long identityNumber, string firstName, string lastName, int birthYear, CancellationToken ct)
        {
            var result = await nvi.TCKimlikNoDogrulaAsync(identityNumber, firstName, lastName, birthYear, ct);
            return result.IsSuccess;
        }
    }

    [Fact]
    public async Task Nvi_ornekleri()
    {
        Assert.False(await new PatientVerificationService(nvi).VerifyAsync(10774881040, "AHMET", "YILMAZ", 1980, default));

        long identityNumber = 10774881040;
        var person = (await nvi.KisiSorgulaTCKimlikNoAsync(identityNumber)).Data;
        _ = person?.TemelBilgisi?.Ad;
        _ = person?.TemelBilgisi?.DogumTarih?.Date;
        _ = person?.DurumBilgisi?.Durum?.Aciklama;
    }
}

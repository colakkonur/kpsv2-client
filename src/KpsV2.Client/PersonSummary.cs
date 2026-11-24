using KpsV2.Contracts;

namespace KpsV2;

/// <summary>Sorgulanan kişinin kimlik türü.</summary>
public enum PersonType
{
    /// <summary>T.C. vatandaşı (KPS: TCVatandasiKisiKutukleri).</summary>
    TurkishCitizen,
    /// <summary>Yabancı, 99 ile başlayan yabancı kimlik numarası (KPS: YabanciKisiKutukleri).</summary>
    Foreigner,
    /// <summary>Mavi kartlı, doğumla vatandaş olup sonradan çıkma izni alan (KPS: MaviKartliKisiKutukleri).</summary>
    BlueCard,
}

/// <summary>
/// Üç kütük tipinin (TC / Yabancı / Mavi Kart) ortak alanlarını tek tipe indiren özet.
/// Tüm alanlar için kaynak tipe (<see cref="Source"/>) bakın. Yetkiniz olmayan alanlar KPS tarafından boş döner.
/// </summary>
public sealed class PersonSummary
{
    /// <summary>Kimlik türü.</summary>
    public PersonType Type { get; init; }
    /// <summary>T.C. / yabancı / mavi kart kimlik numarası.</summary>
    public long? IdentityNumber { get; init; }
    /// <summary>Ad.</summary>
    public string? FirstName { get; init; }
    /// <summary>Soyad.</summary>
    public string? LastName { get; init; }
    /// <summary>Baba adı.</summary>
    public string? FatherName { get; init; }
    /// <summary>Anne adı.</summary>
    public string? MotherName { get; init; }
    /// <summary>Annenin kimlik numarası.</summary>
    public long? MotherIdentityNumber { get; init; }
    /// <summary>Babanın kimlik numarası.</summary>
    public long? FatherIdentityNumber { get; init; }
    /// <summary>Cinsiyet (Kod 1: Erkek, 2: Kadın).</summary>
    public Parametre? Gender { get; init; }
    /// <summary>Doğum tarihi.</summary>
    public TarihBilgisi? BirthDate { get; init; }
    /// <summary>Doğum yeri.</summary>
    public string? BirthPlace { get; init; }
    /// <summary>Kişi durumu (Açık, Ölüm...).</summary>
    public Parametre? Status { get; init; }
    /// <summary>Medeni hal.</summary>
    public Parametre? MaritalStatus { get; init; }
    /// <summary>Ölüm tarihi (varsa).</summary>
    public TarihBilgisi? DeathDate { get; init; }
    /// <summary>Uyruk / ülke (yabancı ve mavi kartlılarda).</summary>
    public Parametre? Nationality { get; init; }
    /// <summary>Kaynak kişi kaydı: <see cref="TCKisiBilgisi"/>, <see cref="YabanciKisiBilgisi"/> veya <see cref="MaviKartKisiBilgisi"/>.</summary>
    public object Source { get; init; } = null!;

    /// <summary>Ad ve soyad.</summary>
    public string FullName => $"{FirstName} {LastName}".Trim();

    /// <summary>Kişi ölü olarak kayıtlı mı (ölüm tarihi dolu)?</summary>
    public bool IsDeceased => DeathDate is { IsEmpty: false };
}

/// <summary>KPS kişi tiplerini <see cref="PersonSummary"/>'ye çeviren uzantılar.</summary>
public static class PersonSummaryExtensions
{
    /// <summary>BilesikKisiSorgula sonucu: dolu olan kütükten özet; kişi bulunamadıysa null.</summary>
    public static PersonSummary? ToSummary(this BilesikKisiBilgisi? b) =>
        b is null ? null : From(b.TCVatandasiKisiKutukleri, b.YabanciKisiKutukleri, b.MaviKartliKisiKutukleri);

    /// <summary>BilesikKisiveAdresSorgula sonucu: kişi özeti (adres için <see cref="BilesikKisiveAdresBilgisi.AdresBilgisi"/>).</summary>
    public static PersonSummary? ToSummary(this BilesikKisiveAdresBilgisi? b) =>
        b is null ? null : From(b.TCVatandasiKisiKutukleri, b.YabanciKisiKutukleri, b.MaviKartliKisiKutukleri);

    /// <summary>BilesikKisiveYerlesimYeriSorgula sonucu: kişi özeti (adres için <see cref="BilesikKisiveYerlesimYeriBilgisi.AdresBilgisi"/>).</summary>
    public static PersonSummary? ToSummary(this BilesikKisiveYerlesimYeriBilgisi? b) =>
        b is null ? null : From(b.TCVatandasiKisiKutukleri, b.YabanciKisiKutukleri, b.MaviKartliKisiKutukleri);

    /// <summary>T.C. vatandaşı kişi kaydı (BilesikKisi*, TcKimlikNoSorgula, AileBireyleri...).</summary>
    public static PersonSummary? ToSummary(this TCKisiBilgisi? k)
    {
        if (k is null || (k.TCKimlikNo is null && k.TemelBilgisi is null)) return null;
        var t = k.TemelBilgisi;
        return new PersonSummary
        {
            Type = PersonType.TurkishCitizen,
            IdentityNumber = k.TCKimlikNo,
            FirstName = t?.Ad,
            LastName = t?.Soyad,
            FatherName = t?.BabaAd,
            MotherName = t?.AnneAd,
            MotherIdentityNumber = NonZero(k.AnneTCKimlikNo),
            FatherIdentityNumber = NonZero(k.BabaTCKimlikNo),
            Gender = t?.Cinsiyet,
            BirthDate = NonEmpty(t?.DogumTarih),
            BirthPlace = t?.DogumYer,
            Status = k.DurumBilgisi?.Durum,
            MaritalStatus = k.DurumBilgisi?.MedeniHal,
            DeathDate = NonEmpty(k.DurumBilgisi?.OlumTarih),
            Source = k,
        };
    }

    /// <summary>Yabancı kişi kaydı.</summary>
    public static PersonSummary? ToSummary(this YabanciKisiBilgisi? k)
    {
        if (k is null || (k.KimlikNo is null && k.TemelBilgisi is null)) return null;
        var t = k.TemelBilgisi;
        return new PersonSummary
        {
            Type = PersonType.Foreigner,
            IdentityNumber = k.KimlikNo,
            FirstName = t?.Ad,
            LastName = t?.Soyad,
            FatherName = t?.BabaAd,
            MotherName = t?.AnneAd,
            MotherIdentityNumber = NonZero(k.AnneKimlikNo),
            FatherIdentityNumber = NonZero(k.BabaKimlikNo),
            Gender = t?.Cinsiyet,
            BirthDate = NonEmpty(t?.DogumTarih) ?? NonEmpty(k.DogumTarih),
            BirthPlace = t?.DogumYer,
            Status = k.DurumBilgisi?.Durum,
            MaritalStatus = k.DurumBilgisi?.MedeniHal,
            DeathDate = NonEmpty(k.OlumTarih),
            Nationality = k.Uyruk,
            Source = k,
        };
    }

    /// <summary>Mavi kartlı kişi kaydı.</summary>
    public static PersonSummary? ToSummary(this MaviKartKisiBilgisi? k)
    {
        if (k is null || (k.KimlikNo is null && k.TemelBilgisi is null)) return null;
        var t = k.TemelBilgisi;
        return new PersonSummary
        {
            Type = PersonType.BlueCard,
            IdentityNumber = k.KimlikNo,
            FirstName = t?.Ad,
            LastName = t?.Soyad,
            FatherName = t?.BabaAd,
            MotherName = t?.AnneAd,
            MotherIdentityNumber = NonZero(k.AnneTCKimlikNo),
            FatherIdentityNumber = NonZero(k.BabaTCKimlikNo),
            Gender = t?.Cinsiyet,
            BirthDate = NonEmpty(t?.DogumTarih),
            BirthPlace = t?.DogumYer,
            Status = k.DurumBilgisi?.Durum,
            MaritalStatus = k.DurumBilgisi?.MedeniHal,
            DeathDate = NonEmpty(k.DurumBilgisi?.OlumTarih),
            Nationality = k.Ulke,
            Source = k,
        };
    }

    private static PersonSummary? From(TCVatandasiKisiKutukleri? tc, YabanciKisiKutukleri? yb, MaviKartliKisiKutukleri? mk) =>
        tc?.KisiBilgisi.ToSummary() ?? yb?.KisiBilgisi.ToSummary() ?? mk?.KisiBilgisi.ToSummary();

    private static long? NonZero(long? n) => n is null or 0 ? null : n;

    private static TarihBilgisi? NonEmpty(TarihBilgisi? t) => t is null || t.IsEmpty ? null : t;
}

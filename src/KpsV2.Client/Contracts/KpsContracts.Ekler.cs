using System.Globalization;

// Üretilen WSDL tiplerine (KpsContracts.g.cs) elle eklenen davranışlar. Üretilen dosya değişmez.
namespace KpsV2.Contracts;

/// <summary>Her KPS cevabının ortak kalıbı: üst seviye HataBilgisi + Sonuc.</summary>
internal interface IKpsServiceResult<out T>
{
    Parametre HataBilgisi { get; }
    T Sonuc { get; }
}

/// <summary>Kendi HataBilgisi alanı olan sonuç tipleri.</summary>
internal interface IHasError
{
    Parametre HataBilgisi { get; }
}

#pragma warning disable CS1591 // Üretilen tiplerin partial eşleri: belgeler üretilen tarafta.
// ---- cevap zarfları (WSDL'deki KpsServisSonucuOf{T}) ----
public partial class KpsServisSonucuOfBilesikKisiBilgisiZmZ26SC9 : IKpsServiceResult<BilesikKisiBilgisi> { }
public partial class KpsServisSonucuOfBilesikKisiveAdresBilgisiZmZ26SC9 : IKpsServiceResult<BilesikKisiveAdresBilgisi> { }
public partial class KpsServisSonucuOfBilesikKisiveYerlesimYeriBilgisiZmZ26SC9 : IKpsServiceResult<BilesikKisiveYerlesimYeriBilgisi> { }
public partial class KpsServisSonucuOfTCKisiBilgisiZmZ26SC9 : IKpsServiceResult<TCKisiBilgisi> { }
public partial class KpsServisSonucuOfKisiAdresBilgisiZmZ26SC9 : IKpsServiceResult<KisiAdresBilgisi> { }
public partial class KpsServisSonucuOfTcKimlikNoIleAdresBilgisiZmZ26SC9 : IKpsServiceResult<TcKimlikNoIleAdresBilgisi> { }
public partial class KpsServisSonucuOfAcikAdresZmZ26SC9 : IKpsServiceResult<AcikAdres> { }
public partial class KpsServisSonucuOfAileBilgisiZmZ26SC9 : IKpsServiceResult<AileBilgisi> { }
public partial class KpsServisSonucuOfGenelNufusKayitOrnegiZmZ26SC9 : IKpsServiceResult<GenelNufusKayitOrnegi> { }
public partial class KpsServisSonucuOfYakinlikServisiAileBilgisiZmZ26SC9 : IKpsServiceResult<YakinlikServisiAileBilgisi> { }
public partial class KpsServisSonucuOfMaviKartliAileBilgisiZmZ26SC9 : IKpsServiceResult<MaviKartliAileBilgisi> { }
public partial class KpsServisSonucuOfKamuYakinlikServisiAileBilgisiZmZ26SC9 : IKpsServiceResult<KamuYakinlikServisiAileBilgisi> { }
public partial class KpsServisSonucuOfArrayOfWcfServiceZmZ26SC9 : IKpsServiceResult<WcfService[]> { }
public partial class KpsServisSonucuOfArrayOfstringuHEDJ7Dj : IKpsServiceResult<string[]> { }
public partial class KpsServisSonucuOfArrayOfParametreZmZ26SC9 : IKpsServiceResult<Parametre[]> { }

// ---- kendi HataBilgisi olan sonuçlar ----
public partial class BilesikKisiBilgisi : IHasError { }
public partial class BilesikKisiveAdresBilgisi : IHasError { }
public partial class BilesikKisiveYerlesimYeriBilgisi : IHasError { }
public partial class TCKisiBilgisi : IHasError { }
public partial class KisiAdresBilgisi : IHasError { }
public partial class TcKimlikNoIleAdresBilgisi : IHasError { }
public partial class AcikAdres : IHasError { }
public partial class AileBilgisi : IHasError { }
public partial class YakinlikServisiAileBilgisi : IHasError { }
public partial class MaviKartliAileBilgisi : IHasError { }
public partial class KamuYakinlikServisiAileBilgisi : IHasError { }
#pragma warning restore CS1591

/// <summary>KPS'nin kod + açıklama çifti (Cinsiyet, MedeniHal, Durum, Uyruk, HataBilgisi...).</summary>
public partial class Parametre
{
    /// <summary>Yeni bir parametre.</summary>
    public Parametre() { }

    /// <summary>Kod ve açıklama ile yeni bir parametre.</summary>
    public Parametre(int? code, string description)
    {
        Kod = code;
        Aciklama = description;
    }

    /// <inheritdoc />
    public override string ToString() => Aciklama ?? Kod?.ToString(CultureInfo.InvariantCulture) ?? "";
}

/// <summary>KPS tarih bilgisi. Gün/ay/yıl ayrı ayrı boş gelebilir (ör. yalnızca doğum yılı bilinen kayıtlar).</summary>
public partial class TarihBilgisi
{
    /// <summary>Gün, ay ve yıl geçerli bir tarih oluşturuyorsa o tarih; değilse null.</summary>
    public DateOnly? Date
    {
        get
        {
            if (Gun is not > 0 || Ay is not (>= 1 and <= 12) || Yil is not > 0) return null;
            return Gun <= DateTime.DaysInMonth(Yil.Value, Ay.Value) ? new DateOnly(Yil.Value, Ay.Value, Gun.Value) : null;
        }
    }

    /// <summary>Gün, ay ve yılın hiçbiri dolu değil.</summary>
    public bool IsEmpty => Gun is not > 0 && Ay is not > 0 && Yil is not > 0;

    /// <summary>gg.aa.yyyy; bilinmeyen kısımlar ?? ile gösterilir.</summary>
    public override string ToString() =>
        $"{(Gun is > 0 ? Gun.Value.ToString("00", CultureInfo.InvariantCulture) : "??")}." +
        $"{(Ay is > 0 ? Ay.Value.ToString("00", CultureInfo.InvariantCulture) : "??")}." +
        $"{(Yil is > 0 ? Yil.Value.ToString(CultureInfo.InvariantCulture) : "????")}";
}

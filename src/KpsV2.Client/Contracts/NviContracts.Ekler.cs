using System.Globalization;

// Üretilen NVİ WSDL tiplerine (NviContracts.g.cs) elle eklenen davranışlar. Üretilen dosya değişmez.
namespace KpsV2.Nvi.Contracts;

/// <summary>NVİ'nin kod + açıklama çifti (Cinsiyet, Durum, MedeniHal, Il, HataBilgisi...).</summary>
public partial class Parametre
{
    /// <inheritdoc />
    public override string ToString() => Aciklama ?? Kod?.ToString(CultureInfo.InvariantCulture) ?? "";
}

/// <summary>NVİ tarih bilgisi. Gün/ay/yıl ayrı ayrı boş gelebilir (ör. yalnızca doğum yılı bilinen kayıtlar).</summary>
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

    /// <summary>gg.aa.yyyy; bilinmeyen kısımlar ?? ile gösterilir.</summary>
    public override string ToString() =>
        $"{(Gun is > 0 ? Gun.Value.ToString("00", CultureInfo.InvariantCulture) : "??")}." +
        $"{(Ay is > 0 ? Ay.Value.ToString("00", CultureInfo.InvariantCulture) : "??")}." +
        $"{(Yil is > 0 ? Yil.Value.ToString(CultureInfo.InvariantCulture) : "????")}";
}

using System.Globalization;

namespace KpsV2;

/// <summary>T.C., yabancı ve mavi kart kimlik numaraları için kontrol hanesi doğrulaması (hepsi aynı algoritmayı kullanır).</summary>
public static class IdentityNumberValidator
{
    /// <summary>11 hane, ilk hane 0 değil ve 10./11. kontrol haneleri tutuyorsa true.</summary>
    public static bool IsValid(long identityNumber) =>
        identityNumber is >= 10_000_000_000 and <= 99_999_999_999 && IsValid(identityNumber.ToString(CultureInfo.InvariantCulture));

    /// <summary>11 hane, ilk hane 0 değil ve 10./11. kontrol haneleri tutuyorsa true.</summary>
    public static bool IsValid(string? identityNumber)
    {
        if (identityNumber is not { Length: 11 } || identityNumber[0] == '0') return false;
        Span<int> d = stackalloc int[11];
        for (int i = 0; i < 11; i++)
        {
            if (!char.IsAsciiDigit(identityNumber[i])) return false;
            d[i] = identityNumber[i] - '0';
        }
        int odd = d[0] + d[2] + d[4] + d[6] + d[8];
        int even = d[1] + d[3] + d[5] + d[7];
        int check10 = ((odd * 7 - even) % 10 + 10) % 10;
        int sum = 0;
        for (int i = 0; i < 10; i++) sum += d[i];
        return check10 == d[9] && sum % 10 == d[10];
    }
}

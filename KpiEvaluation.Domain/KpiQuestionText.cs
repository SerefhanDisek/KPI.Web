using System.Globalization;
using System.Text.RegularExpressions;

namespace KpiEvaluation.Domain;

public static class KpiQuestionText
{
    private static readonly CultureInfo TurkishCulture = CultureInfo.GetCultureInfo("tr-TR");

    public static string CapitalizeWords(string value) => Regex.Replace(value, @"\p{L}[\p{L}\p{M}\p{N}'’]*",
        word => word.Value[..1].ToUpper(TurkishCulture) + word.Value[1..]);

    public static string NormalizeIdentity(string value) => Regex.Replace(value.Replace("\uF0B7", "").Normalize(), @"\s+", " ")
        .Trim().TrimEnd('.', '?', '!').TrimEnd().ToUpper(TurkishCulture);
}

using System.Globalization;
using System.IO.Compression;
using System.Xml;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using KpiEvaluation.Application;

namespace KpiEvaluation.Infrastructure;

internal static class SpreadsheetWorkbook
{
    internal const int MaxRows = 10000;
    internal const int MaxBytes = 10 * 1024 * 1024;
    internal static string SheetName(SpreadsheetResource resource) => resource switch
    {
        SpreadsheetResource.Employees => "Çalışanlar",
        SpreadsheetResource.Kpis => "KPI Yönetimi",
        SpreadsheetResource.Leaves => "İzin Takibi",
        SpreadsheetResource.Evaluations => "Değerlendirmeler",
        _ => throw new ValidationException("Geçersiz Excel sayfası.")
    };

    internal static string[] Headers(SpreadsheetResource resource) => resource switch
    {
        SpreadsheetResource.Employees => ["ID", "Ad Soyad", "Meslek ID", "Meslek", "Aktif"],
        SpreadsheetResource.Leaves => ["ID", "Çalışan ID", "Çalışan", "Ekip", "Başlangıç", "Bitiş", "İzin Türü", "Açıklama", "Aktif"],
        SpreadsheetResource.Kpis => ["ID", "KPI", "Açıklama", "Ekip ID'leri", "Ekipler", "Herkes", "Sıra", "Aktif", "Kategori",
            "Şirket Hedefi ID", "Şirket Hedefi", "Ölçüm Türü", "Birim", "Hedef Değer", "Hedef Yönü", "Ağırlık", "Veri Kaynağı",
            "Gözden Geçirme", "Geçerlilik Başlangıcı", "Geçerlilik Bitişi", "Belirli", "Ölçülebilir", "Ulaşılabilir", "İlgili", "Zaman Sınırlı"],
        SpreadsheetResource.Evaluations => ["Aktarım Grubu", "Çalışan ID", "Çalışan", "Ekip", "Dönem ID", "Dönem", "Değerlendirme Tarihi",
            "Genel Not", "KPI ID", "KPI", "Puan", "Gerçekleşen", "Madde Notu", "Durum", "Amir Görüşü", "Çalışan Görüşü", "Gelişim Aksiyonu", "Takip Tarihi", "Değerlendiren Kullanıcı ID", "Değerlendiren"],
        _ => throw new ValidationException("Geçersiz Excel sayfası.")
    };

    internal static XLWorkbook Create(SpreadsheetResource resource)
    {
        var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet(SheetName(resource));
        WriteRow(sheet, 1, Headers(resource));
        sheet.Row(1).Style.Font.Bold = true;
        sheet.Row(1).Style.Fill.BackgroundColor = XLColor.FromHtml("#E8EEF5");
        sheet.SheetView.FreezeRows(1);
        sheet.Columns(1, Headers(resource).Length).Width = 23;
        var instructions = workbook.AddWorksheet("Kullanım");
        string[] notes = [
            "Yalnızca ilk veri sayfası içe aktarılır. Başlıkları değiştirmeyin; sütunların sırası değişebilir.",
            "En fazla 10 MB ve 10.000 veri satırı yüklenebilir. Formül hücreleri desteklenmez.",
            "ID boşsa yeni kayıt oluşturulur; doluysa ilgili kayıt güncellenir. Referans ID'leri yardımcı sayfalardan alınır; ad sütunları bilgilendirme amaçlıdır.",
            "Başarılı kayıtlar saklanır; hatalı kayıtlar Excel satır numaralarıyla bildirilir. Aynı ID bir dosyada tekrar edemez.",
            "Aktif/Herkes/SMART alanları: Evet veya Hayır (1/0, TRUE/FALSE de geçerlidir). Boş Aktif alanı Evet kabul edilir.",
            "Tarihler: Excel tarih hücresi, yyyy-MM-dd veya dd.MM.yyyy. Ondalık sayılarda virgül veya nokta kullanılabilir.",
            "KPI Ekip ID'leri noktalı virgülle ayrılır (örnek: 1;2). Herkes=Evet olduğunda ekip seçilmez.",
            "Ölçüm Türü: 0=Manuel 1-5, 1=Yüzde, 2=Adet, 3=Süre, 4=Sayısal. Hedef Yönü: 0=En az, 1=En çok.",
            "Gözden Geçirme: 0=Aylık, 1=Üç aylık, 2=Altı aylık, 3=Yıllık. Ağırlık 0–100 arasında olmalıdır.",
            "Değerlendirmede her KPI ayrı satırdır. Aynı değerlendirmeye ait tüm satırlarda aynı Aktarım Grubu ve aynı üst bilgiler kullanılmalıdır.",
            "Değerlendirmeler yeni kayıt olarak alınır; aynı çalışan/dönem zaten varsa atlanır. Onaylı kayıtlar değiştirilmez.",
            "Değerlendirmeler varsayılan olarak geçici kaydedilir. Durum sütunu bilgilendirme amaçlıdır; kalıcı kayıt yükleme ekranından seçilir ve onay yetkisi gerektirir.",
            "Geçici kayıtta boş puan 0 kabul edilir. Kalıcı kayıtta tüm puanlar 1–5 olmalı, sayısal KPI puanı mevcut hedefe uygun olmalıdır."
        ];
        for (var i = 0; i < notes.Length; i++) instructions.Cell(i + 1, 1).Value = notes[i];
        instructions.Column(1).Width = 120;
        instructions.Column(1).Style.Alignment.WrapText = true;
        return workbook;
    }

    internal static void WriteRow(IXLWorksheet sheet, int row, params object?[] values)
    {
        for (var i = 0; i < values.Length; i++)
        {
            var cell = sheet.Cell(row, i + 1);
            switch (values[i])
            {
                case null: break;
                case bool flag: cell.Value = flag ? "Evet" : "Hayır"; break;
                case DateOnly date: cell.Value = date.ToDateTime(TimeOnly.MinValue); cell.Style.DateFormat.Format = "yyyy-mm-dd"; break;
                case DateTime date: cell.Value = date; cell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm:ss"; break;
                case int number: cell.Value = number; break;
                case long number: cell.Value = number; break;
                case decimal number: cell.Value = (double)number; break;
                default: cell.Value = values[i]!.ToString(); break;
            }
        }
    }

    internal static async Task<IReadOnlyList<SpreadsheetRow>> ReadAsync(SpreadsheetResource resource, Stream content,
        string fileName, CancellationToken ct)
    {
        if (!string.Equals(Path.GetExtension(fileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new ValidationException(".xlsx uzantılı bir Excel dosyası seçmelisiniz.");
        using var buffer = new MemoryStream();
        var block = new byte[81920];
        int length;
        while ((length = await content.ReadAsync(block, ct)) > 0)
        {
            if (buffer.Length + length > MaxBytes) throw new ValidationException("Excel dosyası en fazla 10 MB olabilir.");
            buffer.Write(block, 0, length);
        }
        if (buffer.Length == 0) throw new ValidationException("Excel dosyası boş.");
        try
        {
            buffer.Position = 0;
            using (var archive = new ZipArchive(buffer, ZipArchiveMode.Read, true))
                if (archive.Entries.Count > 1000 || archive.Entries.Sum(x => x.Length) > 64 * 1024 * 1024)
                    throw new ValidationException("Excel dosyasının açılmış boyutu izin verilen sınırı aşıyor.");
            buffer.Position = 0;
            using var workbook = new XLWorkbook(buffer);
            var sheet = workbook.Worksheets.FirstOrDefault()
                ?? throw new ValidationException("Excel dosyasında veri sayfası bulunamadı.");
            if (sheet.Name != SheetName(resource)) throw new ValidationException($"{SheetName(resource)} şablonunu kullanmalısınız.");
            if ((sheet.LastRowUsed()?.RowNumber() ?? 0) > MaxRows + 1 || (sheet.LastColumnUsed()?.ColumnNumber() ?? 0) > 40)
                throw new ValidationException("Excel dosyası en fazla 10.000 veri satırı ve 40 sütun içerebilir.");
            var headers = sheet.Row(1).CellsUsed().ToArray();
            if (headers.Any(x => x.HasFormula)) throw new ValidationException("Başlıklar formül içeremez.");
            var names = headers.Select(x => x.GetString().Trim()).ToArray();
            var requiredHeaders = Headers(resource).Where(name => resource != SpreadsheetResource.Evaluations
                || name is not ("Değerlendiren Kullanıcı ID" or "Değerlendiren"));
            if (names.Distinct(StringComparer.Ordinal).Count() != names.Length || requiredHeaders.Except(names).Any())
                throw new ValidationException("Excel sütunları şablonla eşleşmiyor. Şablonu yeniden indirip başlıkları koruyun.");
            var rows = new List<SpreadsheetRow>();
            foreach (var row in sheet.RowsUsed().Where(x => x.RowNumber() > 1))
            {
                ct.ThrowIfCancellationRequested();
                var values = headers.ToDictionary(x => x.GetString().Trim(), x =>
                {
                    var cell = row.Cell(x.Address.ColumnNumber);
                    if (cell.HasFormula) throw new ValidationException($"Satır {row.RowNumber()}: Formül yerine hücre değerini yapıştırmalısınız.");
                    return cell.DataType == XLDataType.DateTime ? cell.GetDateTime().ToString("s", CultureInfo.InvariantCulture)
                        : cell.Value.ToString(CultureInfo.InvariantCulture).Trim();
                });
                if (values.Values.Any(x => x.Length > 0)) rows.Add(new(row.RowNumber(), values));
            }
            if (rows.Count == 0) throw new ValidationException("Excel dosyasında aktarılacak veri bulunamadı.");
            return rows;
        }
        catch (Exception ex) when (ex is InvalidDataException or XmlException or OpenXmlPackageException or FormatException or ArgumentException)
        {
            throw new ValidationException("Excel dosyası okunamadı. Geçerli, şifresiz bir .xlsx dosyası yüklemelisiniz.");
        }
    }
}

internal sealed record SpreadsheetRow(int Number, IReadOnlyDictionary<string, string> Values)
{
    internal string Text(string column, int maxLength = 2000, bool required = false)
    {
        var value = Values[column];
        if (value.Length > maxLength || (required && string.IsNullOrWhiteSpace(value)))
            throw new ValidationException($"{column} {(required ? "zorunludur ve " : "")}en fazla {maxLength} karakter olabilir.");
        return value;
    }

    internal long? Id(string column = "ID")
    {
        var value = Text(column);
        if (value == "") return null;
        if (!long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var id) || id <= 0)
            throw new ValidationException($"{column} pozitif bir tam sayı olmalıdır.");
        return id;
    }

    internal int Int(string column, int? fallback = null)
    {
        var value = Text(column);
        if (value == "" && fallback is not null) return fallback.Value;
        if (!int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number))
            throw new ValidationException($"{column} bir tam sayı olmalıdır.");
        return number;
    }

    internal int? OptionalInt(string column) => Text(column) == "" ? null : Int(column);

    internal bool Bool(string column, bool fallback = false) => Text(column).ToLowerInvariant() switch
    {
        "" => fallback, "evet" or "true" or "1" => true, "hayır" or "hayir" or "false" or "0" => false,
        _ => throw new ValidationException($"{column} Evet veya Hayır olmalıdır.")
    };

    internal decimal? Decimal(string column)
    {
        var value = Text(column);
        if (value == "") return null;
        if (decimal.TryParse(value.Replace(',', '.'), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
            CultureInfo.InvariantCulture, out var number)) return number;
        throw new ValidationException($"{column} geçerli bir sayı olmalıdır.");
    }

    internal DateTime? Date(string column)
    {
        var value = Text(column);
        if (value == "") return null;
        if (DateTime.TryParseExact(value, ["yyyy-MM-dd", "dd.MM.yyyy", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-dd HH:mm:ss"],
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)) return date;
        throw new ValidationException($"{column} geçerli bir tarih olmalıdır (yyyy-MM-dd veya dd.MM.yyyy).");
    }

    internal DateOnly? Day(string column) => Date(column) is { } date ? DateOnly.FromDateTime(date) : null;

    internal T Enum<T>(string column, T fallback) where T : struct, Enum
    {
        var number = Int(column, Convert.ToInt32(fallback));
        var value = (T)System.Enum.ToObject(typeof(T), number);
        if (!System.Enum.IsDefined(value)) throw new ValidationException($"{column} kodu geçersiz.");
        return value;
    }
}

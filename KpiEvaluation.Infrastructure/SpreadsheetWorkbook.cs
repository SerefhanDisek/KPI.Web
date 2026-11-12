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
        SpreadsheetResource.Calibration => "Onaylı Amir Sonuçları",
        _ => throw new ValidationException("Geçersiz Excel sayfası.")
    };

    internal static string[] Headers(SpreadsheetResource resource) => resource switch
    {
        SpreadsheetResource.Employees => ["Ad Soyad", "Meslek", "Aktiflik durumu"],
        SpreadsheetResource.Leaves => ["Adı Soyadı", "Kalan İzin Gün Sayısı", "İzin Hak Ediş Tarihi"],
        SpreadsheetResource.Kpis => ["KPI", "Açıklama", "Ekipler", "Herkes", "Kategori", "Bağlı olduğu şirket Hedefi"],
        SpreadsheetResource.Evaluations => ["Çalışan", "Ekip", "Dönem", "Not", "KPI", "Puan", "Amir Görüşü", "Değerlendiren Amir"],
        SpreadsheetResource.Calibration => ["Çalışan", "Ekip", "Dönem", "Amir Sonuçları", "Ortalama Puan"],
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
        if (resource == SpreadsheetResource.Calibration)
        {
            sheet.Column(4).Width = 50;
            sheet.Column(4).Style.Alignment.WrapText = true;
            sheet.Column(5).Style.NumberFormat.Format = "0.00";
        }
        if (resource == SpreadsheetResource.Leaves)
        {
            sheet.Column(1).Width = 32;
            sheet.Column(2).Style.NumberFormat.Format = "0.##";
            sheet.Column(3).Style.DateFormat.Format = "dd.mm.yyyy";
        }
        if (resource == SpreadsheetResource.Kpis)
        {
            sheet.Column(1).Width = 45;
            sheet.Column(2).Width = 60;
            sheet.Columns(1, 2).Style.Alignment.WrapText = true;
            sheet.Column(6).Width = 35;
        }
        var instructions = workbook.AddWorksheet("Kullanım");
        string[] notes = resource == SpreadsheetResource.Calibration ? [
            "Bu dosya Onaylı Amir Sonuçları raporudur. Şablon indir aynı sütunlarla boş veri sayfası oluşturur; dosya içe aktarma amacıyla kullanılmaz.",
            "Veri sayfasında yalnızca Çalışan, Ekip, Dönem, Amir Sonuçları ve Ortalama Puan sütunları bulunur; her çalışan ve dönem için bir satır yazılır.",
            "Seçili dönemlerdeki ve dosyayı indiren kullanıcının erişebildiği onaylı değerlendirmeler dışa aktarılır.",
            "Puanlar 100 üzerinden, amir etki oranları yüzde olarak gösterilir. Ortalama Puan güncel ekip oranlarıyla hesaplanır.",
            "Atanmış amirlerin onaylı değerlendirmeleri tamamlanmamışsa Ortalama Puan hücresi boş bırakılır; eksik puan sıfır kabul edilmez.",
            "Güncel atamaya dahil olmayan veya değerlendiren kimliği bilinmeyen kayıtlar Amir Sonuçları içinde belirtilir; birleşik hesaba katılmaz."
        ] : resource == SpreadsheetResource.Evaluations ? [
            "İlk veri sayfasındaki sütunlar: Çalışan, Ekip, Dönem, Not, KPI, Puan, Amir Görüşü, Değerlendiren Amir. Başlıkların sırası değişebilir.",
            "Her KPI ayrı satırdır. Aynı değerlendirmeye ait satırlarda Çalışan, Ekip, Dönem ve Amir Görüşü aynı olmalıdır.",
            "Çalışan ve ekip adları birlikte eşleştirilir. Dönem ve KPI adlarını yardımcı sayfalardan alın; teknik ID veya aktarım grubu gerekmez.",
            "Aynı adla birden fazla çalışan veya uygun KPI bulunursa kayıt alınmaz; satır hatası bildirilir. Tüm güncel KPI soruları bulunmalıdır.",
            "Puanlar her soru için 1–5 arasındadır. Geçici kayıtta boş puan 0 kabul edilir; kalıcı kayıtta tüm sorular puanlanmalıdır.",
            "Not, soruya ait kısa nottur. Amir Görüşü değerlendirmeye ait ortak açıklamadır; aynı değerlendirmenin tüm satırlarında aynı olmalıdır.",
            "Değerlendiren Amir bilgilendirme ve satırları gruplama amaçlıdır. Kayıt her zaman dosyayı yükleyen kullanıcıya ait oluşturulur; başka biri adına kayıt yapılmaz.",
            "Mevcut çalışan/dönem/değerlendiren kayıtları atlanır. İçe aktarma var olan değerlendirmeleri değiştirmez; yeni kayıtlar varsayılan olarak taslaktır.",
            "Kalıcı kayıt yükleme ekranından seçilir ve oluşturma/onaylama yetkileri gerektirir. Sayısal KPI için gereken gerçekleşen değer sade dosyada yoksa uygulamadan değerlendirme yapılmalıdır.",
            "En fazla 10 MB ve 10.000 veri satırı yüklenebilir. Formül yerine değer kullanın; başarılı kayıtlar saklanır, hatalar kaynak satırıyla gösterilir.",
            "Önceki teknik ID içeren değerlendirme şablonları da okunabilir; yeni indirmelerde yalnızca sade sütunlar yer alır."
        ] : resource == SpreadsheetResource.Kpis ? [
            "Veri sayfasında yalnızca KPI, Açıklama, Ekipler, Herkes, Kategori ve Bağlı olduğu şirket Hedefi sütunları bulunur. Teknik ID gerekmez; sütun sırası değişebilir.",
            "Herkes: Evet veya Hayır (1/0 ve TRUE/FALSE da geçerlidir). Herkes=Evet tüm ekipleri kapsar; Ekipler alanı dikkate alınmaz. Herkes=Hayır olduğunda en az bir aktif ekip adı gerekir.",
            "Birden fazla ekip adını noktalı virgülle ayırın (örnek: DevOps; Yazılım). Adları Ekip Referansları sayfasından alabilirsiniz.",
            "Bağlı olduğu şirket Hedefi isteğe bağlıdır. Hedef Referansları sayfasındaki aktif hedef adlarından birini kullanın; birden fazla hedefle eşleşen adlar alınmaz.",
            "KPI başlığı ve ekip kapsamı tek bir mevcut kayıtla eşleşirse açıklama, kategori ve şirket hedefi güncellenir; eşleşme yoksa yeni KPI oluşturulur. Belirsiz eşleşmeler ve dosyada tekrarlanan KPI/kapsam çiftleri alınmaz.",
            "Başlık veya ekip kapsamı değişikliği için uygulamadaki Düzenle işlemini kullanın. Yeni KPI standart manuel 1–5 puanlama, sıfır ağırlık, sıra 1 ve aktif durumla oluşturulur; mevcut kayıtların dosyada olmayan ayarları ve aktiflik durumu korunur.",
            "KPI en fazla 1.000, Açıklama 2.000, Kategori 150 karakter olabilir. Çok satırlı metinler desteklenir. Formül yerine hücre değeri kullanın.",
            "En fazla 10 MB ve 10.000 veri satırı yüklenebilir. Başarılı kayıtlar saklanır, hatalar kaynak satırıyla bildirilir. Eski teknik ID içeren KPI dosyaları da okunabilir."
        ] : resource == SpreadsheetResource.Leaves ? [
            "Veri sayfasında yalnızca Adı Soyadı, Kalan İzin Gün Sayısı ve İzin Hak Ediş Tarihi sütunları bulunur; teknik ID gerekmez.",
            "İlk sayfa okunur. Başlıklar ilk dolu satırda olmalıdır; üstte boş satır ve farklı sayfa adı kullanılabilir. Sütunların sırası değişebilir.",
            "Adı Soyadı tek bir aktif çalışanla eşleşirse mevcut kayıt kullanılır. Sistemde bulunmayan ad için Meslek atanmadı mesleğiyle yeni çalışan oluşturulur; eksik meslek kaydı da oluşturulur. Giriş hesabı açılmaz.",
            "Adı Soyadı birden fazla aktif çalışanla eşleşirse satır alınmaz. Bu adla yalnızca pasif çalışan varsa yeni kayıt açılmaz; önce mevcut çalışanı etkinleştirin.",
            "Her çalışan için tek bir güncel bakiye saklanır. Tekrar yüklemede kalan gün ve hak ediş tarihi güncellenir; dosyada bulunmayan çalışanlar değişmez. Aynı çalışan adı bir dosyada tekrar edemez.",
            "Kalan İzin Gün Sayısı zorunlu sayısal değerdir. Yarım gün ve negatif bakiyeler desteklenir; ondalık ayırıcı virgül veya nokta olabilir.",
            "İzin Hak Ediş Tarihi zorunludur: Excel tarih hücresi, dd.MM.yyyy veya yyyy-MM-dd kullanın. Tarih ve kalan gün dosyadan aynen alınır; otomatik hak ediş veya iş günü hesabı yapılmaz.",
            "En fazla 10 MB ve 10.000 veri satırı yüklenebilir. Formül yerine değer kullanın; başarılı kayıtlar saklanır, hatalar özgün Excel satır numarasıyla gösterilir.",
            "Eski başlangıç/bitiş tarihli izin kayıtları korunur. Önceki ID ve Çalışan ID içeren dosyalar eski kayıt biçiminde okunmaya devam eder."
        ] : resource == SpreadsheetResource.Employees ? [
            "İlk veri sayfasında yalnızca Ad Soyad, Meslek ve Aktiflik durumu sütunları bulunur; teknik ID gerekmez.",
            "Meslek adı sistemde yoksa yeni aktif meslek oluşturulur. Meslek alanı boşsa veya sütun yoksa yeni çalışan Meslek atanmadı mesleğiyle eklenir; mevcut çalışanın mesleği korunur.",
            "Ad Soyad tek bir mevcut kayıtla eşleşirse aynı çalışan güncellenir; eşleşme yoksa yeni çalışan oluşturulur. Meslek belirtilmişse mevcut çalışana uygulanır. Yalnızca personel kaydı oluşturulur; giriş hesabı açılmaz.",
            "Pasif çalışanlar da eşleştirmeye dahildir. Aynı adla birden fazla çalışan varsa belirtilen meslek tek bir kayıtla eşleşmelidir; belirsiz eşleşmeler alınmaz. Pasif meslekler otomatik etkinleştirilmez.",
            "Aynı Ad Soyad dosyada tekrar edemez. Ad değişikliği için uygulamadaki Düzenle işlemini kullanın.",
            "Aktiflik durumu: Evet veya Hayır; 1/0 ve TRUE/FALSE de kabul edilir. Boş değer Evet kabul edilir. Hayır mevcut kaydı pasife alır.",
            "Yeni veya mesleği değişen çalışanlar mesleğin ekip amirlerini devralır. Mesleği değişmeyen çalışanların amir bağlılıkları ve geçmiş değerlendirmeler korunur.",
            "En fazla 10 MB ve 10.000 veri satırı yüklenebilir. Formül yerine değer kullanın; başarılı satırlar saklanır, hatalar kaynak satırıyla bildirilir.",
            "Önceki ID, Meslek ID ve Aktif sütunlarını içeren çalışan dosyaları da okunabilir; yeni şablon ve çıktılar yalnızca sade üç sütunu içerir."
        ] : [
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
            if (resource != SpreadsheetResource.Leaves && sheet.Name != SheetName(resource))
                throw new ValidationException($"{SheetName(resource)} şablonunu kullanmalısınız.");
            var headerRow = resource == SpreadsheetResource.Leaves ? sheet.FirstRowUsed(XLCellsUsedOptions.Contents)?.RowNumber() ?? 1 : 1;
            if ((sheet.LastRowUsed()?.RowNumber() ?? 0) > MaxRows + headerRow || (sheet.LastColumnUsed()?.ColumnNumber() ?? 0) > 40)
                throw new ValidationException("Excel dosyası en fazla 10.000 veri satırı ve 40 sütun içerebilir.");
            var headers = sheet.Row(headerRow).CellsUsed(XLCellsUsedOptions.Contents).ToArray();
            if (headers.Any(x => x.HasFormula)) throw new ValidationException("Başlıklar formül içeremez.");
            var names = headers.Select(x => x.GetString().Trim()).ToArray();
            var legacyEvaluation = resource == SpreadsheetResource.Evaluations && names.Contains("Aktarım Grubu");
            string[] legacyHeaders = ["Aktarım Grubu", "Çalışan ID", "Çalışan", "Ekip", "Dönem ID", "Dönem", "Değerlendirme Tarihi",
                "Genel Not", "KPI ID", "KPI", "Puan", "Gerçekleşen", "Madde Notu", "Durum", "Amir Görüşü", "Çalışan Görüşü", "Gelişim Aksiyonu", "Takip Tarihi"];
            var legacyEmployees = resource == SpreadsheetResource.Employees && names.Contains("Meslek ID");
            string[] legacyEmployeeHeaders = ["ID", "Ad Soyad", "Meslek ID", "Meslek", "Aktif"];
            var legacyLeaves = resource == SpreadsheetResource.Leaves && names.Contains("Çalışan ID");
            string[] legacyLeaveHeaders = ["ID", "Çalışan ID", "Çalışan", "Ekip", "Başlangıç", "Bitiş", "İzin Türü", "Açıklama", "Aktif"];
            var legacyKpis = resource == SpreadsheetResource.Kpis && names.Contains("Ekip ID'leri");
            string[] legacyKpiHeaders = ["ID", "KPI", "Açıklama", "Ekip ID'leri", "Ekipler", "Herkes", "Sıra", "Aktif", "Kategori",
                "Şirket Hedefi ID", "Şirket Hedefi", "Ölçüm Türü", "Birim", "Hedef Değer", "Hedef Yönü", "Ağırlık", "Veri Kaynağı",
                "Gözden Geçirme", "Geçerlilik Başlangıcı", "Geçerlilik Bitişi", "Belirli", "Ölçülebilir", "Ulaşılabilir", "İlgili", "Zaman Sınırlı"];
            var requiredHeaders = legacyKpis ? legacyKpiHeaders : legacyLeaves ? legacyLeaveHeaders : legacyEmployees ? legacyEmployeeHeaders : legacyEvaluation ? legacyHeaders : Headers(resource).Where(name => (resource != SpreadsheetResource.Evaluations
                || name != "Değerlendiren Amir") && (resource != SpreadsheetResource.Employees || name != "Meslek"));
            if (names.Distinct(StringComparer.Ordinal).Count() != names.Length || requiredHeaders.Except(names).Any())
                throw new ValidationException("Excel sütunları şablonla eşleşmiyor. Şablonu yeniden indirip başlıkları koruyun.");
            var rows = new List<SpreadsheetRow>();
            foreach (var row in sheet.RowsUsed().Where(x => x.RowNumber() > headerRow))
            {
                ct.ThrowIfCancellationRequested();
                var values = headers.ToDictionary(x => x.GetString().Trim(), x =>
                {
                    var cell = row.Cell(x.Address.ColumnNumber);
                    if (cell.HasFormula) throw new ValidationException($"Satır {row.RowNumber()}: Formül yerine hücre değerini yapıştırmalısınız.");
                    return cell.DataType == XLDataType.DateTime ? cell.GetDateTime().ToString("s", CultureInfo.InvariantCulture)
                        : cell.Value.ToString(CultureInfo.InvariantCulture).Trim();
                });
                if (resource == SpreadsheetResource.Employees && !legacyEmployees) values.TryAdd("Meslek", "");
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

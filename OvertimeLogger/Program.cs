using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

const string dataFileName = "overtime_records.json";

var storage = new OvertimeStorage(dataFileName);
var records = storage.Load();

Console.OutputEncoding = System.Text.Encoding.UTF8;

while (true)
{
    Console.WriteLine();
    Console.WriteLine("ثبت اضافه کاری - منو");
    Console.WriteLine("1) ثبت رکورد جدید");
    Console.WriteLine("2) مشاهده همه رکوردها");
    Console.WriteLine("3) خلاصه ماهانه");
    Console.WriteLine("4) خروجی CSV");
    Console.WriteLine("5) خروج");
    Console.Write("انتخاب شما: ");

    var choice = Console.ReadLine();
    Console.WriteLine();

    switch (choice)
    {
        case "1":
            var record = PromptForRecord();
            records.Add(record);
            storage.Save(records);
            Console.WriteLine("رکورد ذخیره شد.");
            break;
        case "2":
            DisplayRecords(records);
            break;
        case "3":
            DisplayMonthlySummary(records);
            break;
        case "4":
            var csvPath = storage.ExportCsv(records);
            Console.WriteLine($"فایل CSV ذخیره شد: {csvPath}");
            break;
        case "5":
            return;
        default:
            Console.WriteLine("گزینه نامعتبر است.");
            break;
    }
}

static OvertimeRecord PromptForRecord()
{
    var date = PromptForDate("تاریخ (مثال 1402/05/10 یا 2024-01-20): ");
    var hours = PromptForDecimal("تعداد ساعت اضافه کاری: ");
    Console.Write("شرح/توضیح: ");
    var description = Console.ReadLine() ?? string.Empty;

    return new OvertimeRecord
    {
        Date = date,
        Hours = hours,
        Description = description.Trim(),
    };
}

static DateTime PromptForDate(string label)
{
    while (true)
    {
        Console.Write(label);
        var input = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine("تاریخ الزامی است.");
            continue;
        }

        if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
        {
            return result.Date;
        }

        if (DateTime.TryParse(input, CultureInfo.CurrentCulture, DateTimeStyles.None, out result))
        {
            return result.Date;
        }

        Console.WriteLine("فرمت تاریخ نامعتبر است.");
    }
}

static decimal PromptForDecimal(string label)
{
    while (true)
    {
        Console.Write(label);
        var input = Console.ReadLine()?.Trim();
        if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) && value > 0)
        {
            return value;
        }

        if (decimal.TryParse(input, NumberStyles.Number, CultureInfo.CurrentCulture, out value) && value > 0)
        {
            return value;
        }

        Console.WriteLine("عدد نامعتبر است. لطفا دوباره تلاش کنید.");
    }
}

static void DisplayRecords(IEnumerable<OvertimeRecord> records)
{
    if (!records.Any())
    {
        Console.WriteLine("هیچ رکوردی ثبت نشده است.");
        return;
    }

    Console.WriteLine("لیست رکوردها:");
    foreach (var record in records.OrderBy(r => r.Date))
    {
        Console.WriteLine($"- {record.Date:yyyy-MM-dd} | {record.Hours} ساعت | {record.Description}");
    }
}

static void DisplayMonthlySummary(IEnumerable<OvertimeRecord> records)
{
    if (!records.Any())
    {
        Console.WriteLine("هیچ رکوردی ثبت نشده است.");
        return;
    }

    var summary = records
        .GroupBy(r => new { r.Date.Year, r.Date.Month })
        .OrderBy(g => g.Key.Year)
        .ThenBy(g => g.Key.Month);

    Console.WriteLine("خلاصه ماهانه:");
    foreach (var group in summary)
    {
        var total = group.Sum(r => r.Hours);
        Console.WriteLine($"- {group.Key.Year}/{group.Key.Month:00}: {total} ساعت");
    }
}

public sealed class OvertimeRecord
{
    public DateTime Date { get; set; }
    public decimal Hours { get; set; }
    public string Description { get; set; } = string.Empty;
}

public sealed class OvertimeStorage
{
    private readonly string _filePath;
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public OvertimeStorage(string fileName)
    {
        var baseDirectory = AppContext.BaseDirectory;
        _filePath = Path.Combine(baseDirectory, fileName);
    }

    public List<OvertimeRecord> Load()
    {
        if (!File.Exists(_filePath))
        {
            return new List<OvertimeRecord>();
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<List<OvertimeRecord>>(json, Options) ?? new List<OvertimeRecord>();
        }
        catch (JsonException)
        {
            return new List<OvertimeRecord>();
        }
    }

    public void Save(List<OvertimeRecord> records)
    {
        var json = JsonSerializer.Serialize(records, Options);
        File.WriteAllText(_filePath, json);
    }

    public string ExportCsv(IEnumerable<OvertimeRecord> records)
    {
        var csvPath = Path.Combine(Path.GetDirectoryName(_filePath) ?? AppContext.BaseDirectory, "overtime_records.csv");
        using var writer = new StreamWriter(csvPath, false, System.Text.Encoding.UTF8);
        writer.WriteLine("Date,Hours,Description");
        foreach (var record in records.OrderBy(r => r.Date))
        {
            var line = string.Join(',',
                record.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                record.Hours.ToString(CultureInfo.InvariantCulture),
                EscapeCsv(record.Description));
            writer.WriteLine(line);
        }

        return csvPath;
    }

    private static string EscapeCsv(string input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return "";
        }

        if (input.Contains(',') || input.Contains('"') || input.Contains('\n'))
        {
            return '"' + input.Replace("\"", "\"\"") + '"';
        }

        return input;
    }
}

using System.Globalization;
using System.Text;
using ExpenseTracker.Application.Features.Expenses;

namespace ExpenseTracker.Api.Http;

/// <summary>RFC 4180 CSV for spreadsheets, hardened against formula injection.</summary>
internal static class ExpenseCsvWriter
{
    private static readonly string[] Header = ["Date", "Description", "Category", "Amount", "Notes", "Created (UTC)"];

    public static byte[] Write(IEnumerable<ExpenseDto> expenses)
    {
        var csv = new StringBuilder();
        csv.AppendJoin(',', Header).Append("\r\n");

        foreach (var e in expenses)
        {
            csv.AppendJoin(',',
                e.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                Escape(e.Description),
                Escape(e.CategoryName),
                e.Amount.ToString("0.00", CultureInfo.InvariantCulture),
                Escape(e.Notes),
                e.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            csv.Append("\r\n");
        }

        // UTF-8 BOM so Excel detects the encoding (₹, accents, etc.).
        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(csv.ToString())];
    }

    internal static string Escape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";

        // Cells starting with these are evaluated as formulas by spreadsheet apps.
        if (value[0] is '=' or '+' or '-' or '@' or '\t' or '\r') value = "'" + value;

        return value.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"" : value;
    }
}

using System.Text;

namespace FinanceTracker.Application.Features.Transactions;

/// <summary>Minimal RFC-4180 CSV reader/writer — handles quoted fields, embedded commas, quotes and newlines.</summary>
public static class Csv
{
    public static string Escape(string? field)
    {
        var value = field ?? string.Empty;
        if (value.Contains('"') || value.Contains(',') || value.Contains('\n') || value.Contains('\r'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    public static string Row(params string?[] fields)
        => string.Join(",", fields.Select(Escape));

    /// <summary>Parses CSV content into rows of fields. Skips a trailing empty line.</summary>
    public static List<List<string>> Parse(string content)
    {
        var rows = new List<List<string>>();
        var field = new StringBuilder();
        var row = new List<string>();
        var inQuotes = false;
        var i = 0;

        void EndField() { row.Add(field.ToString()); field.Clear(); }
        void EndRow() { EndField(); rows.Add(row); row = new List<string>(); }

        while (i < content.Length)
        {
            var c = content[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < content.Length && content[i + 1] == '"') { field.Append('"'); i += 2; continue; }
                    inQuotes = false; i++; continue;
                }
                field.Append(c); i++; continue;
            }

            switch (c)
            {
                case '"': inQuotes = true; i++; break;
                case ',': EndField(); i++; break;
                case '\r': i++; break;
                case '\n': EndRow(); i++; break;
                default: field.Append(c); i++; break;
            }
        }

        // flush trailing field/row if any content remains
        if (field.Length > 0 || row.Count > 0) EndRow();

        // drop rows that are entirely empty (e.g. trailing blank line)
        rows.RemoveAll(r => r.Count == 1 && string.IsNullOrWhiteSpace(r[0]));
        return rows;
    }
}

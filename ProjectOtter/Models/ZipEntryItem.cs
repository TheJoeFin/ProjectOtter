using System.IO.Compression;
using System.Text.RegularExpressions;

namespace ProjectOtter.Models;

public partial class ZipEntryItem
{
    public ZipArchiveEntry Entry { get; set; }

    public string Name => Entry.FullName;

    public string FirstChar => Name[0].ToString();

    public bool IsJSON => Path.GetExtension(Entry.FullName) == ".json";

    public string Content { get; set; } = string.Empty;

    public bool IsEmpty => InitialLength == 0;

    public int InitialLength { get; set; }

    public DateTime? CreationDate { get; set; }

    public ZipEntryItem(ZipArchiveEntry entry)
    {
        Entry = entry;
        InitialLength = (int)Entry.Length;

        // Regex to match YYYY-MM-DD
        Match match = DateRegex().Match(Name);
        if (match.Success)
        {
            bool success = DateTime.TryParse(match.Value, out DateTime result);

            if (success)
                CreationDate = result;
        }
    }

    [GeneratedRegex("20\\d{2}-\\d{2}-\\d{2}")]
    private static partial Regex DateRegex();
}

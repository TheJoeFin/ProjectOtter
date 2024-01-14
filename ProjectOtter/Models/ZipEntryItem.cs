using System.IO.Compression;

namespace ProjectOtter.Models;

public class ZipEntryItem
{
    public ZipArchiveEntry Entry { get; set; }

    public string Name => Entry.FullName;

    public string FirstChar => Name[0].ToString();

    public bool IsJSON => Path.GetExtension(Entry.FullName) == ".json";

    public string Content { get; set; } = string.Empty;

    public bool IsEmpty => InitalLength == 0;

    public int InitalLength { get; set; }

    public ZipEntryItem(ZipArchiveEntry entry)
    {
        Entry = entry;
        InitalLength = (int)Entry.Length;
    }
}

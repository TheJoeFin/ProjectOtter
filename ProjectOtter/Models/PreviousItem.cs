using Humanizer;

namespace ProjectOtter.Models;

public class PreviousItem
{
    public string DisplayText { get; set; } = string.Empty;
    public string ZipPath { get; set; } = string.Empty;

    public string FileName => Path.GetFileNameWithoutExtension(ZipPath);

    public DateTimeOffset AccessedDateTime { get; set; } = DateTimeOffset.Now;

    public string TimeAgo => AccessedDateTime.Humanize();

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
        {
            return false;
        }

        PreviousItem other = (PreviousItem)obj;
        return ZipPath == other.ZipPath;
    }

    public override int GetHashCode()
    {
        return ZipPath.GetHashCode();
    }
}

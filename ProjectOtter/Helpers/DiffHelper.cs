using System.Text;

namespace ProjectOtter.Helpers;

public enum DiffLineType
{
    Unchanged,
    Added,
    Deleted,
    Modified
}

public class DiffLine
{
    public DiffLineType Type { get; set; }
    public string OriginalContent { get; set; } = string.Empty;
    public string ModifiedContent { get; set; } = string.Empty;
    public int OriginalLineNumber { get; set; }
    public int ModifiedLineNumber { get; set; }
}

public static class DiffHelper
{
    public static List<DiffLine> ComputeDiff(string originalText, string modifiedText)
    {
        var originalLines = originalText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var modifiedLines = modifiedText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        return ComputeDiffLines(originalLines, modifiedLines);
    }

    private static List<DiffLine> ComputeDiffLines(string[] original, string[] modified)
    {
        var result = new List<DiffLine>();
        var lcs = LongestCommonSubsequence(original, modified);

        int i = 0, j = 0, k = 0;

        while (i < original.Length || j < modified.Length)
        {
            if (k < lcs.Count && i < original.Length && j < modified.Length &&
                original[i] == lcs[k] && modified[j] == lcs[k])
            {
                // Unchanged line
                result.Add(new DiffLine
                {
                    Type = DiffLineType.Unchanged,
                    OriginalContent = original[i],
                    ModifiedContent = modified[j],
                    OriginalLineNumber = i + 1,
                    ModifiedLineNumber = j + 1
                });
                i++;
                j++;
                k++;
            }
            else if (k < lcs.Count && j < modified.Length && modified[j] == lcs[k])
            {
                // Line deleted from original
                result.Add(new DiffLine
                {
                    Type = DiffLineType.Deleted,
                    OriginalContent = original[i],
                    ModifiedContent = string.Empty,
                    OriginalLineNumber = i + 1,
                    ModifiedLineNumber = -1
                });
                i++;
            }
            else if (k < lcs.Count && i < original.Length && original[i] == lcs[k])
            {
                // Line added in modified
                result.Add(new DiffLine
                {
                    Type = DiffLineType.Added,
                    OriginalContent = string.Empty,
                    ModifiedContent = modified[j],
                    OriginalLineNumber = -1,
                    ModifiedLineNumber = j + 1
                });
                j++;
            }
            else if (i < original.Length && j < modified.Length)
            {
                // Line modified
                result.Add(new DiffLine
                {
                    Type = DiffLineType.Modified,
                    OriginalContent = original[i],
                    ModifiedContent = modified[j],
                    OriginalLineNumber = i + 1,
                    ModifiedLineNumber = j + 1
                });
                i++;
                j++;
            }
            else if (i < original.Length)
            {
                // Remaining lines deleted
                result.Add(new DiffLine
                {
                    Type = DiffLineType.Deleted,
                    OriginalContent = original[i],
                    ModifiedContent = string.Empty,
                    OriginalLineNumber = i + 1,
                    ModifiedLineNumber = -1
                });
                i++;
            }
            else if (j < modified.Length)
            {
                // Remaining lines added
                result.Add(new DiffLine
                {
                    Type = DiffLineType.Added,
                    OriginalContent = string.Empty,
                    ModifiedContent = modified[j],
                    OriginalLineNumber = -1,
                    ModifiedLineNumber = j + 1
                });
                j++;
            }
        }

        return result;
    }

    private static List<string> LongestCommonSubsequence(string[] original, string[] modified)
    {
        int m = original.Length;
        int n = modified.Length;
        int[,] dp = new int[m + 1, n + 1];

        // Build LCS table
        for (int i = 1; i <= m; i++)
        {
            for (int j = 1; j <= n; j++)
            {
                if (original[i - 1] == modified[j - 1])
                {
                    dp[i, j] = dp[i - 1, j - 1] + 1;
                }
                else
                {
                    dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
                }
            }
        }

        // Backtrack to find LCS
        var lcs = new List<string>();
        int x = m, y = n;

        while (x > 0 && y > 0)
        {
            if (original[x - 1] == modified[y - 1])
            {
                lcs.Insert(0, original[x - 1]);
                x--;
                y--;
            }
            else if (dp[x - 1, y] > dp[x, y - 1])
            {
                x--;
            }
            else
            {
                y--;
            }
        }

        return lcs;
    }

    public static string FormatDiffAsText(List<DiffLine> diffLines)
    {
        var sb = new StringBuilder();

        foreach (var line in diffLines)
        {
            switch (line.Type)
            {
                case DiffLineType.Unchanged:
                    sb.AppendLine($"  {line.OriginalContent}");
                    break;
                case DiffLineType.Added:
                    sb.AppendLine($"+ {line.ModifiedContent}");
                    break;
                case DiffLineType.Deleted:
                    sb.AppendLine($"- {line.OriginalContent}");
                    break;
                case DiffLineType.Modified:
                    sb.AppendLine($"- {line.OriginalContent}");
                    sb.AppendLine($"+ {line.ModifiedContent}");
                    break;
            }
        }

        return sb.ToString();
    }

    public static string FormatDiffAsSideBySide(List<DiffLine> diffLines)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Original | Modified");
        sb.AppendLine("---------|----------");

        foreach (var line in diffLines)
        {
            string leftPart = line.OriginalLineNumber > 0
                ? $"{line.OriginalLineNumber,4}: {line.OriginalContent}"
                : "     ";
            string rightPart = line.ModifiedLineNumber > 0
                ? $"{line.ModifiedLineNumber,4}: {line.ModifiedContent}"
                : "     ";

            string marker = line.Type switch
            {
                DiffLineType.Added => "[+]",
                DiffLineType.Deleted => "[-]",
                DiffLineType.Modified => "[~]",
                _ => "   "
            };

            sb.AppendLine($"{marker} {leftPart} | {rightPart}");
        }

        return sb.ToString();
    }
}

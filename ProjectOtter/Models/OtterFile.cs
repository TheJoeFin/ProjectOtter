namespace ProjectOtter.Models;

public class OtterFile
{
    public string FriendlyName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string GitHubIssueUrl => $"https://github.com/microsoft/PowerToys/issues/{GitHubIssueNumber}";
    public int GitHubIssueNumber { get; set; }
    public IEnumerable<string> RelatedUtilities { get; set; } = Enumerable.Empty<string>();
    public IEnumerable<Version> RelatedVersions { get; set; } = Enumerable.Empty<Version>();
    public string FilteringText { get; set; } = string.Empty;

    public OtterFile()
    {
    }

    public bool IsFileRelevant(string relativePath)
    {
        foreach (string utilityName in RelatedUtilities)
            if (relativePath.Contains(utilityName))
                return true;

        foreach (Version version in RelatedVersions)
            if (relativePath.Contains(version.ToString()))
                return true;

        return false;
    }
}

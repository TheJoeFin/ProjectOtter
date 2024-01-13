namespace ProjectOtter.Models;

public class OtterFile
{
    public string FriendlyName { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public string GitHubIssueUrl { get; set; } = string.Empty;
    public IEnumerable<string> RelatedUtilities { get; set; } = Enumerable.Empty<string>();
    public IEnumerable<Version> RelatedVersions { get; set; } = Enumerable.Empty<Version>();

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

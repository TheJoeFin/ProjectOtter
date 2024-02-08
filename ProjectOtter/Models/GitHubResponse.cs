using System.Collections.Generic;

namespace ProjectOtter.Models;

public record GitHubResponse
{
    public string Title { get; set; } = string.Empty;
    public int IssueNumber { get; set; }
    public List<string> Labels { get; set; } = new List<string>();
}

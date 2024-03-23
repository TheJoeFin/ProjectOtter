using CliWrap;
using CliWrap.Buffered;
using ProjectOtter.Models;
using System.Text;

namespace ProjectOtter.Helpers;
public class GitHubCliHelper
{
    public static async Task<bool> IsGhCliInstalled()
    {
        Command cmd = Cli.Wrap("gh").WithArguments("--version");
        CommandResult result = await cmd.ExecuteAsync();

        return result.ExitCode == 0;
    }

    public static async Task<GitHubResponse?> GetIssueDetails(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        StringBuilder standardOutput = new();
        StringBuilder standardError = new();

        Command cmd = Cli.Wrap("gh")
            .WithArguments($"search issues {fileName}")
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(standardOutput))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(standardError));
        CommandResult result = await cmd.ExecuteBufferedAsync(Encoding.UTF8);

        if (result.ExitCode != 0 || standardOutput.Length == 0)
            return null;

        var lines = standardOutput.ToString().Split(new char[] { '\n' });
        if (lines.Length < 2)
            return null;

        string firstDataLine = lines[0];

        var data = firstDataLine.Split(new char[] { '\t' });

        if (data.Length < 4)
            return null;

        GitHubResponse ghResponse = new()
        {
            IssueNumber = int.Parse(data[1]),
            Title = data[3],
            Labels = data[4].Split(new char[] { ',' }).ToList()
        };

        return ghResponse;
    }
}

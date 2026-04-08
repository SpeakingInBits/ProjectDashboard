using SQLite;

namespace ProjectDashboard.Models;

public class GitHubProject
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string Owner { get; set; } = string.Empty;

    public string RepoName { get; set; } = string.Empty;

    public int OpenIssues { get; set; }

    public string? LatestCommitDate { get; set; }
}

using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ProjectDashboard.Services;

public class GitHubService
{
    private readonly HttpClient _httpClient;

    public GitHubService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(int openIssues, DateTime? latestCommit)> GetRepoInfoAsync(string owner, string repo)
    {
        var issuesTask = GetOpenIssueCountAsync(owner, repo);
        var commitTask = GetLatestCommitDateAsync(owner, repo);
        await Task.WhenAll(issuesTask, commitTask);
        return (await issuesTask, await commitTask);
    }

    private async Task<int> GetOpenIssueCountAsync(string owner, string repo)
    {
        var query = Uri.EscapeDataString($"repo:{owner}/{repo} type:issue state:open");
        var result = await _httpClient.GetFromJsonAsync<SearchResult>(
            $"https://api.github.com/search/issues?q={query}");
        return result?.TotalCount ?? 0;
    }

    private async Task<DateTime?> GetLatestCommitDateAsync(string owner, string repo)
    {
        var commits = await _httpClient.GetFromJsonAsync<CommitInfo[]>(
            $"https://api.github.com/repos/{owner}/{repo}/commits?per_page=1");
        return commits?.Length > 0 ? commits[0].Commit?.Author?.Date : null;
    }

    public static bool TryParseGitHubUrl(string url, out string owner, out string repo)
    {
        owner = string.Empty;
        repo = string.Empty;
        try
        {
            var uri = new Uri(url.Trim().TrimEnd('/'));
            if (!uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
                return false;

            var segments = uri.AbsolutePath.Trim('/').Split('/');
            if (segments.Length < 2)
                return false;

            owner = segments[0];
            repo = segments[1];
            return !string.IsNullOrWhiteSpace(owner) && !string.IsNullOrWhiteSpace(repo);
        }
        catch
        {
            return false;
        }
    }

    private sealed class SearchResult
    {
        [JsonPropertyName("total_count")]
        public int TotalCount { get; set; }
    }

    private sealed class CommitInfo
    {
        [JsonPropertyName("commit")]
        public CommitDetail? Commit { get; set; }
    }

    private sealed class CommitDetail
    {
        [JsonPropertyName("author")]
        public CommitAuthor? Author { get; set; }
    }

    private sealed class CommitAuthor
    {
        [JsonPropertyName("date")]
        public DateTime Date { get; set; }
    }
}

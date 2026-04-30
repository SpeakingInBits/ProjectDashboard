using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ProjectDashboard.Services;

public class GitHubService
{
    private readonly HttpClient _httpClient;
    private readonly SettingsService _settingsService;

    public GitHubService(HttpClient httpClient, SettingsService settingsService)
    {
        _httpClient = httpClient;
        _settingsService = settingsService;
    }

    private async Task<T?> GetAsync<T>(string url, string? owner = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        var token = owner is not null
            ? await _settingsService.GetTokenForOwnerAsync(owner)
            : await _settingsService.GetTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);

        if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized
                                or System.Net.HttpStatusCode.Forbidden)
            throw new GitHubAuthException();

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    private async Task SendAsync(HttpMethod method, string url, string? owner = null)
    {
        using var request = new HttpRequestMessage(method, url);
        var token = owner is not null
            ? await _settingsService.GetTokenForOwnerAsync(owner)
            : await _settingsService.GetTokenAsync();
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.SendAsync(request);

        if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized)
            throw new GitHubAuthException();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException(
                $"GitHub API returned {(int)response.StatusCode} {response.ReasonPhrase}",
                inner: null,
                statusCode: response.StatusCode);
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
        var result = await GetAsync<SearchResult>($"https://api.github.com/search/issues?q={query}", owner);
        return result?.TotalCount ?? 0;
    }

    private async Task<DateTime?> GetLatestCommitDateAsync(string owner, string repo)
    {
        var commits = await GetAsync<CommitInfo[]>(
            $"https://api.github.com/repos/{owner}/{repo}/commits?per_page=1", owner);
        return commits?.Length > 0 ? commits[0].Commit?.Author?.Date : null;
    }

    public async Task<IReadOnlyList<OwnerRepo>> GetOwnerReposAsync(string owner)
    {
        var token = await _settingsService.GetTokenForOwnerAsync(owner);
        List<OwnerRepo> all = [];
        int page = 1;

        if (!string.IsNullOrWhiteSpace(token))
        {
            // GET /user/repos is the only endpoint that returns private repos.
            // Fetch the authenticated user's own repos and filter by the requested owner name.
            while (true)
            {
                var pageResults = await GetAsync<OwnerRepo[]>(
                    $"https://api.github.com/user/repos?affiliation=owner,organization_member&per_page=100&page={page}", owner);
                if (pageResults is null || pageResults.Length == 0) break;

                all.AddRange(pageResults.Where(r =>
                    (r.Owner?.Login ?? string.Empty).Equals(owner, StringComparison.OrdinalIgnoreCase)));

                if (pageResults.Length < 100) break;
                page++;
            }

            if (all.Count > 0)
                return all;
        }

        // No token, or owner is a different account -- public repos only.
        while (true)
        {
            var pageResults = await GetAsync<OwnerRepo[]>(
                $"https://api.github.com/users/{owner}/repos?type=public&per_page=100&page={page}", owner);
            if (pageResults is null || pageResults.Length == 0) break;

            all.AddRange(pageResults);
            if (pageResults.Length < 100) break;
            page++;
        }

        return all;
    }

    /// <summary>
    /// Fetches repos the authenticated user owns (including private). Requires a token.
    /// </summary>
    public async Task<IReadOnlyList<OwnerRepo>> GetAuthenticatedUserReposAsync()
    {
        List<OwnerRepo> all = [];
        int page = 1;

        while (true)
        {
            var page_results = await GetAsync<OwnerRepo[]>(
                $"https://api.github.com/user/repos?affiliation=owner&per_page=100&page={page}");
            if (page_results is null || page_results.Length == 0) break;

            all.AddRange(page_results);
            if (page_results.Length < 100) break;
            page++;
        }

        return all;
    }

    public async Task DeleteRepositoryAsync(string owner, string repo)
    {
        await SendAsync(HttpMethod.Delete, $"https://api.github.com/repos/{owner}/{repo}", owner);
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

    /// <summary>
    /// Parses a GitHub owner URL (https://github.com/owner) or plain username into just the owner name.
    /// </summary>
    public static bool TryParseOwnerUrl(string input, out string owner)
    {
        owner = string.Empty;
        var trimmed = input.Trim().TrimEnd('/');

        // Plain username with no slashes
        if (!trimmed.Contains('/'))
        {
            owner = trimmed;
            return !string.IsNullOrWhiteSpace(owner);
        }

        try
        {
            var uri = new Uri(trimmed);
            if (!uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
                return false;

            var segments = uri.AbsolutePath.Trim('/').Split('/');
            if (segments.Length != 1 || string.IsNullOrWhiteSpace(segments[0]))
                return false;

            owner = segments[0];
            return true;
        }
        catch
        {
            return false;
        }
    }

    public sealed class OwnerRepo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("owner")]
        public RepoOwner? Owner { get; set; }
    }

    public sealed class RepoOwner
    {
        [JsonPropertyName("login")]
        public string Login { get; set; } = string.Empty;
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

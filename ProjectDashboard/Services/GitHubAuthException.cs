namespace ProjectDashboard.Services;

public class GitHubAuthException : Exception
{
    public GitHubAuthException()
        : base("Your GitHub token is invalid or has expired.") { }
}

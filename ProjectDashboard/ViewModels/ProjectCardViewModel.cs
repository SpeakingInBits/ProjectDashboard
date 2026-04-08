using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectDashboard.Models;

namespace ProjectDashboard.ViewModels;

public partial class ProjectCardViewModel : ObservableObject
{
    public GitHubProject Project { get; }

    [ObservableProperty]
    private int openIssues;

    [ObservableProperty]
    private string lastUpdatedText = "Last Updated: Unknown";

    public string DisplayName => $"{Project.Owner}/{Project.RepoName}";

    public Color CardAccentColor { get; }

    public IAsyncRelayCommand DeleteCommand { get; }

    public ProjectCardViewModel(GitHubProject project, Func<ProjectCardViewModel, Task> onDelete)
    {
        Project = project;
        openIssues = project.OpenIssues;
        SetLastUpdatedText(project.LatestCommitDate);
        DeleteCommand = new AsyncRelayCommand(() => onDelete(this));
        CardAccentColor = Color.FromArgb(project.CardColor);
    }

    public void UpdateData(int issues, DateTime? latestCommit)
    {
        OpenIssues = issues;
        Project.OpenIssues = issues;
        Project.LatestCommitDate = latestCommit?.ToString("O");
        SetLastUpdatedText(Project.LatestCommitDate);
    }

    private void SetLastUpdatedText(string? dateStr)
    {
        if (DateTime.TryParse(dateStr, out var date))
            LastUpdatedText = $"Last Updated {date:MMM dd, yyyy}";
        else
            LastUpdatedText = "Last Updated: Unknown";
    }
}

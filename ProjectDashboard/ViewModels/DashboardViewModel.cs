using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProjectDashboard.Models;
using ProjectDashboard.Services;
using System.Collections.ObjectModel;

namespace ProjectDashboard.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly DatabaseService _databaseService;
    private readonly GitHubService _gitHubService;
    private bool _loaded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? errorMessage;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor("AddProjectCommand")]
    [NotifyCanExecuteChangedFor("RefreshAllCommand")]
    private bool isLoading;

    [ObservableProperty]
    private string newProjectUrl = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public ObservableCollection<ProjectCardViewModel> Projects { get; } = [];

    public DashboardViewModel(DatabaseService databaseService, GitHubService gitHubService)
    {
        _databaseService = databaseService;
        _gitHubService = gitHubService;
    }

    public async Task LoadProjectsAsync()
    {
        if (_loaded)
            return;
        _loaded = true;

        var projects = await _databaseService.GetProjectsAsync();
        foreach (var project in projects)
            Projects.Add(CreateCard(project));
    }

    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    private async Task AddProjectAsync()
    {
        ErrorMessage = null;
        var url = NewProjectUrl.Trim();

        if (!GitHubService.TryParseGitHubUrl(url, out var owner, out var repo))
        {
            ErrorMessage = "Invalid GitHub URL. Please use: https://github.com/owner/repo";
            return;
        }

        if (Projects.Any(p => p.Project.Owner.Equals(owner, StringComparison.OrdinalIgnoreCase)
                            && p.Project.RepoName.Equals(repo, StringComparison.OrdinalIgnoreCase)))
        {
            ErrorMessage = "This project has already been added.";
            return;
        }

        IsLoading = true;
        try
        {
            var (issues, latestCommit) = await _gitHubService.GetRepoInfoAsync(owner, repo);

            var project = new GitHubProject
            {
                Owner = owner,
                RepoName = repo,
                OpenIssues = issues,
                LatestCommitDate = latestCommit?.ToString("O")
            };

            await _databaseService.SaveProjectAsync(project);
            Projects.Add(CreateCard(project));
            NewProjectUrl = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to fetch project info: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanExecuteCommands))]
    private async Task RefreshAllAsync()
    {
        ErrorMessage = null;
        IsLoading = true;
        try
        {
            foreach (var card in Projects.ToList())
            {
                try
                {
                    var (issues, latestCommit) = await _gitHubService.GetRepoInfoAsync(
                        card.Project.Owner, card.Project.RepoName);
                    card.UpdateData(issues, latestCommit);
                    await _databaseService.SaveProjectAsync(card.Project);
                }
                catch { /* continue refreshing remaining projects */ }
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task DeleteProjectAsync(ProjectCardViewModel card)
    {
        await _databaseService.DeleteProjectAsync(card.Project);
        Projects.Remove(card);
    }

    private bool CanExecuteCommands() => !IsLoading;

    private ProjectCardViewModel CreateCard(GitHubProject project) =>
        new(project, DeleteProjectAsync);
}

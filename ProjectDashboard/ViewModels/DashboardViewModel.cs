using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using ProjectDashboard.Messages;
using ProjectDashboard.Models;
using ProjectDashboard.Services;
using ProjectDashboard.Views;
using System.Collections.ObjectModel;

namespace ProjectDashboard.ViewModels;

public partial class DashboardViewModel : ObservableObject, IRecipient<TokenUpdatedMessage>
{
    private static readonly string[] CardColorPalette =
    [
        "#4A90D9", // Blue
        "#9B59B6", // Purple
        "#27AE60", // Green
        "#E67E22", // Orange
        "#E91E63", // Pink
        "#00BCD4", // Cyan
        "#FF5722", // Deep Orange
        "#009688", // Teal
        "#795548", // Brown
        "#607D8B", // Blue Grey
    ];

    private readonly DatabaseService _databaseService;
    private readonly GitHubService _gitHubService;
    private readonly SettingsService _settingsService;
    private readonly IServiceProvider _serviceProvider;
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

    [ObservableProperty]
    private bool isTokenBannerVisible;

    [ObservableProperty]
    private bool isAuthErrorVisible;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public ObservableCollection<ProjectCardViewModel> Projects { get; } = [];

    public DashboardViewModel(DatabaseService databaseService, GitHubService gitHubService, SettingsService settingsService, IServiceProvider serviceProvider)
    {
        _databaseService = databaseService;
        _gitHubService = gitHubService;
        _settingsService = settingsService;
        _serviceProvider = serviceProvider;
        WeakReferenceMessenger.Default.Register(this);
    }

    public async Task LoadProjectsAsync()
    {
        if (_loaded)
            return;
        _loaded = true;

        var projects = await _databaseService.GetProjectsAsync();
        foreach (var project in projects)
            Projects.Add(CreateCard(project));

        var hasToken = !string.IsNullOrWhiteSpace(await _settingsService.GetTokenAsync());
        IsTokenBannerVisible = !hasToken && !_settingsService.IsBannerDismissed();
    }

    public void Receive(TokenUpdatedMessage message)
    {
        if (message.HasToken)
        {
            _settingsService.DismissBanner();
            IsTokenBannerVisible = false;
            IsAuthErrorVisible = false;
        }
        else
        {
            IsAuthErrorVisible = false;
            IsTokenBannerVisible = !_settingsService.IsBannerDismissed();
        }
    }

    [RelayCommand]
    private async Task OpenSettingsAsync()
    {
        var settingsPage = _serviceProvider.GetRequiredService<SettingsPage>();
        await Shell.Current.Navigation.PushModalAsync(new NavigationPage(settingsPage));
    }

    [RelayCommand]
    private void DismissTokenBanner()
    {
        _settingsService.DismissBanner();
        IsTokenBannerVisible = false;
    }

    [RelayCommand]
    private void DismissAuthError() => IsAuthErrorVisible = false;

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
        IsAuthErrorVisible = false;
        try
        {
            var (issues, latestCommit) = await _gitHubService.GetRepoInfoAsync(owner, repo);

            var project = new GitHubProject
            {
                Owner = owner,
                RepoName = repo,
                OpenIssues = issues,
                LatestCommitDate = latestCommit?.ToString("O"),
                CardColor = CardColorPalette[Projects.Count % CardColorPalette.Length]
            };

            await _databaseService.SaveProjectAsync(project);
            Projects.Add(CreateCard(project));
            NewProjectUrl = string.Empty;
        }
        catch (GitHubAuthException)
        {
            IsAuthErrorVisible = true;
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
        IsAuthErrorVisible = false;
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
                catch (GitHubAuthException)
                {
                    IsAuthErrorVisible = true;
                    break;
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

    private async Task RefreshProjectAsync(ProjectCardViewModel card)
    {
        if (card.IsRefreshing) return;
        card.IsRefreshing = true;
        IsAuthErrorVisible = false;
        try
        {
            var (issues, latestCommit) = await _gitHubService.GetRepoInfoAsync(
                card.Project.Owner, card.Project.RepoName);
            card.UpdateData(issues, latestCommit);
            await _databaseService.SaveProjectAsync(card.Project);
        }
        catch (GitHubAuthException)
        {
            IsAuthErrorVisible = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Failed to refresh {card.DisplayName}: {ex.Message}";
        }
        finally
        {
            card.IsRefreshing = false;
        }
    }

    private async Task OpenProjectSettingsAsync(ProjectCardViewModel card)
    {
        var page = _serviceProvider.GetRequiredService<ProjectSettingsPage>();
        page.Initialize(card);
        await Shell.Current.Navigation.PushModalAsync(new NavigationPage(page));
    }

    public async Task ReorderProjectsAsync(int oldIndex, int newIndex)
    {
        if (oldIndex == newIndex) return;

        var card = Projects[oldIndex];
        Projects.Move(oldIndex, newIndex);

        // Reassign sequential sort order values and persist
        for (int i = 0; i < Projects.Count; i++)
            Projects[i].Project.SortOrder = i;

        await _databaseService.SaveSortOrderAsync(Projects.Select(c => c.Project));
    }

    // Called after CollectionView has already reordered the collection via drag-drop
    public async Task PersistCurrentSortOrderAsync()
    {
        for (int i = 0; i < Projects.Count; i++)
            Projects[i].Project.SortOrder = i;

        await _databaseService.SaveSortOrderAsync(Projects.Select(c => c.Project));
    }

    [RelayCommand]
    private async Task SortByCompletionAsync()
    {
        var sorted = Projects.OrderBy(c => c.IsCompleted ? 1 : 0).ToList();
        for (int i = 0; i < sorted.Count; i++)
        {
            var currentIndex = Projects.IndexOf(sorted[i]);
            if (currentIndex != i)
                Projects.Move(currentIndex, i);
            sorted[i].Project.SortOrder = i;
        }
        await _databaseService.SaveSortOrderAsync(Projects.Select(c => c.Project));
    }

    private bool CanExecuteCommands() => !IsLoading;

    private ProjectCardViewModel CreateCard(GitHubProject project) =>
        new(project, DeleteProjectAsync, OpenProjectSettingsAsync, RefreshProjectAsync);
}

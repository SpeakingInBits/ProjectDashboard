using ProjectDashboard.ViewModels;

namespace ProjectDashboard.Views;

public partial class ProjectSettingsPage : ContentPage
{
    private readonly ProjectSettingsViewModel _viewModel;

    public ProjectSettingsPage(ProjectSettingsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    public void Initialize(ProjectCardViewModel card, Func<ProjectCardViewModel, Task> onDeleteFromGitHub) =>
        _viewModel.Initialize(card, onDeleteFromGitHub);
}

namespace ProjectDashboard.Views;

public partial class DeleteFromGitHubPage : ContentPage
{
    private readonly TaskCompletionSource<bool> _tcs = new();
    private string _repoName = string.Empty;

    public Task<bool> Result => _tcs.Task;

    public DeleteFromGitHubPage()
    {
        InitializeComponent();
    }

    public void Initialize(string owner, string repoName)
    {
        _repoName = repoName;
        RepoNameLabel.Text = $"{owner}/{repoName}";
        ConfirmPromptLabel.Text = $"To confirm deletion, type \"{repoName}\" below:";
    }

    private void OnConfirmEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        DeleteButton.IsEnabled = e.NewTextValue.Trim()
            .Equals(_repoName, StringComparison.OrdinalIgnoreCase);
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        _tcs.TrySetResult(false);
        await Navigation.PopModalAsync();
    }

    private async void OnDeleteClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
        _tcs.TrySetResult(true);
    }

    protected override bool OnBackButtonPressed()
    {
        _tcs.TrySetResult(false);
        return base.OnBackButtonPressed();
    }
}

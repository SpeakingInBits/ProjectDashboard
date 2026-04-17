using ProjectDashboard.ViewModels;

namespace ProjectDashboard
{
    public partial class MainPage : ContentPage
    {
        private readonly DashboardViewModel _viewModel;

        public MainPage(DashboardViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadProjectsAsync();
        }

        private async void OnReorderCompleted(object sender, EventArgs e)
        {
            // CollectionView has already updated the ObservableCollection;
            // just persist the new order to SQLite.
            await _viewModel.PersistCurrentSortOrderAsync();
        }
    }
}


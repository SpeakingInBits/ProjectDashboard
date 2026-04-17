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
            await _viewModel.PersistCurrentSortOrderAsync();
        }

        private void OnRepoLabelPointerEntered(object sender, PointerEventArgs e)
        {
#if WINDOWS
            if (sender is VisualElement element &&
                element.Handler?.PlatformView is Microsoft.UI.Xaml.UIElement native)
            {
                SetCursor(native, Microsoft.UI.Input.InputSystemCursorShape.Hand);
            }
#endif
        }

        private void OnRepoLabelPointerExited(object sender, PointerEventArgs e)
        {
#if WINDOWS
            if (sender is VisualElement element &&
                element.Handler?.PlatformView is Microsoft.UI.Xaml.UIElement native)
            {
                SetCursor(native, Microsoft.UI.Input.InputSystemCursorShape.Arrow);
            }
#endif
        }

#if WINDOWS
        private static void SetCursor(Microsoft.UI.Xaml.UIElement element, Microsoft.UI.Input.InputSystemCursorShape shape)
        {
            // UIElement.ProtectedCursor is protected, so we use a helper subclass to set it.
            CursorHelper.SetCursor(element, Microsoft.UI.Input.InputSystemCursor.Create(shape));
        }
#endif
    }
}


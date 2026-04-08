using Microsoft.Extensions.DependencyInjection;

namespace ProjectDashboard
{
    public partial class App : Application
    {
        private readonly IServiceProvider _services;

        public App(IServiceProvider services)
        {
            InitializeComponent();
            _services = services;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(_services.GetRequiredService<AppShell>())
            {
                Title = "Project Dashboard"
            };
        }
    }
}
namespace Timeline_Note_Taker
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new MainPage();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = base.CreateWindow(activationState);
            
            // Handle window close to minimize to tray instead
            window.Destroying += (s, e) =>
            {
                // This will be handled by the Windows-specific App to minimize to tray
                System.Diagnostics.Debug.WriteLine("Window closing event triggered");
            };

            return window;
        }
    }
}

using Microsoft.UI.Xaml;
using H.NotifyIcon;
using Timeline_Note_Taker.Services;
using Timeline_Note_Taker.Platforms.Windows;
using WinUIWindow = Microsoft.UI.Xaml.Window;
namespace Timeline_Note_Taker.WinUI
{   
    public partial class App : MauiWinUIApplication
    {
        private TaskbarIcon? _trayIcon;
        private GlobalHotkeyService? _hotkeyService;
        private QuickNoteWindow? _quickNoteWindow;
        private WinUIWindow? _mainWindow;

        public App()
        {
            this.InitializeComponent();
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);

            try
            {
                // Get the main window
                var windows = Microsoft.Maui.Controls.Application.Current?.Windows;
                if (windows != null && windows.Count > 0)
                {
                    _mainWindow = windows[0].Handler?.PlatformView as WinUIWindow;
                }

                if (_mainWindow == null)
                {
                    System.Diagnostics.Debug.WriteLine("ERROR: Main window is null!");
                    return;
                }

                // Hook the window closed event to minimize instead
                _mainWindow.Closed += OnMainWindowClosed;

                // Initialize system tray
                InitializeSystemTray();

                // Initialize global hotkey
                InitializeGlobalHotkey();

                // TODO: For debugging, show the window initially
                // Comment this out once tray is working
                _mainWindow.Show();
                _mainWindow.Activate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR in OnLaunched: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void OnMainWindowClosed(object sender, WindowEventArgs args)
        {
            // Prevent the window from actually closing
            args.Handled = true;
            
            // Hide the window instead
            if (_mainWindow != null)
            {
                var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(_mainWindow);
                var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
                appWindow?.Hide();
            }
            
            System.Diagnostics.Debug.WriteLine("Window minimized to tray instead of closing");
        }

        private void InitializeSystemTray()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("Initializing system tray...");
                
                _trayIcon = new TaskbarIcon
                {
                    ToolTipText = "Timeline Note-Taker",
                    IconSource = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(
                        new Uri("ms-appx:///Resources/AppIcon/appicon.svg")
                    )
                };

                // Create context menu
                var contextMenu = new Microsoft.UI.Xaml.Controls.MenuFlyout();

                var openItem = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = "Open Timeline" };
                openItem.Click += (s, e) => ShowMainWindow();
                contextMenu.Items.Add(openItem);

                var quickNoteItem = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = "Quick Note (Win+Shift+N)" };
                quickNoteItem.Click += (s, e) => ShowQuickNoteWindow();
                contextMenu.Items.Add(quickNoteItem);

                contextMenu.Items.Add(new Microsoft.UI.Xaml.Controls.MenuFlyoutSeparator());

                var exitItem = new Microsoft.UI.Xaml.Controls.MenuFlyoutItem { Text = "Exit" };
                exitItem.Click += (s, e) => ExitApplication();
                contextMenu.Items.Add(exitItem);

                _trayIcon.ContextFlyout = contextMenu;
                
                System.Diagnostics.Debug.WriteLine("System tray initialized successfully!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR initializing system tray: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void InitializeGlobalHotkey()
        {
            try
            {
                if (_mainWindow == null)
                {
                    System.Diagnostics.Debug.WriteLine("Cannot initialize hotkey: main window is null");
                    return;
                }

                System.Diagnostics.Debug.WriteLine("Registering global hotkey Win+Shift+N...");
                
                _hotkeyService = new GlobalHotkeyService();
                _hotkeyService.RegisterHotkey(_mainWindow, ShowQuickNoteWindow);

                System.Diagnostics.Debug.WriteLine("Global hotkey registered successfully!");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR registering hotkey: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void ShowMainWindow()
        {
            if (_mainWindow != null)
            {
                _mainWindow.Show();
                _mainWindow.Activate();
            }
        }

        private void ShowQuickNoteWindow()
        {
            if (_quickNoteWindow == null)
            {
                // Get services from DI
                var services = IPlatformApplication.Current?.Services;
                var databaseService = services?.GetService<DatabaseService>();
                var clipboardService = services?.GetService<IClipboardService>();
                var slashCommandParser = services?.GetService<SlashCommandParser>();
                var noteEventService = services?.GetService<NoteEventService>();

                if (databaseService != null && clipboardService != null && slashCommandParser != null && noteEventService != null)
                {
                    _quickNoteWindow = new QuickNoteWindow(databaseService, clipboardService, slashCommandParser, noteEventService);
                }
            }

            if (_quickNoteWindow != null)
            {
                _quickNoteWindow.Show();
                _quickNoteWindow.Activate();
            }
        }

        private void ExitApplication()
        {
            _hotkeyService?.UnregisterHotkey();
            _trayIcon?.Dispose();
            Microsoft.Maui.Controls.Application.Current?.Quit();
        }
    }
}

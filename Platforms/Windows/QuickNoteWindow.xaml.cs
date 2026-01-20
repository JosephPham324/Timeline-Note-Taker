using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Timeline_Note_Taker.Models;
using Timeline_Note_Taker.Services;
using System.Runtime.InteropServices;
using WinUIWindow = Microsoft.UI.Xaml.Window;
namespace Timeline_Note_Taker.Platforms.Windows;

public sealed partial class QuickNoteWindow : WinUIWindow
{
    private readonly DatabaseService _databaseService;
    private readonly IClipboardService _clipboardService;
    private readonly SlashCommandParser _slashCommandParser;
    private readonly NoteEventService _noteEventService;

    public QuickNoteWindow(
        DatabaseService databaseService,
        IClipboardService clipboardService,
        SlashCommandParser slashCommandParser,
        NoteEventService noteEventService)
    {
        this.InitializeComponent();
        
        _databaseService = databaseService;
        _clipboardService = clipboardService;
        _slashCommandParser = slashCommandParser;
        _noteEventService = noteEventService;

        // Center the window on screen
        CenterWindow();
        
        // Make window topmost
        SetWindowTopmost();
    }

    private void CenterWindow()
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        
        if (appWindow != null)
        {
            // Set window size - increased height for better UX
            appWindow.Resize(new global::Windows.Graphics.SizeInt32(600, 250));
            
            var displayArea = Microsoft.UI.Windowing.DisplayArea.GetFromWindowId(windowId, Microsoft.UI.Windowing.DisplayAreaFallback.Nearest);
            if (displayArea != null)
            {
                var centerX = (displayArea.WorkArea.Width - 600) / 2;
                var centerY = (displayArea.WorkArea.Height - 250) / 2;
                appWindow.Move(new global::Windows.Graphics.PointInt32(centerX, centerY));
            }
        }
    }

    private void SetWindowTopmost()
    {
        // WinUI 3 windows are topmost by default when shown
        // We can rely on the Activate() method to bring it to front
    }

    public new void Activate()
    {
        base.Activate();
        InputTextBox.Focus(FocusState.Programmatic);
    }

    [DllImport("user32.dll")]
    private static extern short GetKeyState(int nVirtKey);
    private const int VK_SHIFT = 0x10;

    private async void InputTextBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        // Check if Enter is pressed
        if (e.Key == global::Windows.System.VirtualKey.Enter)
        {
            // Check if Shift is pressed using GetKeyState
            bool isShiftPressed = (GetKeyState(VK_SHIFT) & 0x8000) != 0;

            if (isShiftPressed)
            {
                // Shift+Enter: allow new line (don't handle the event)
                return;
            }

            // Enter alone: save the note
            await SaveNoteAsync();
            e.Handled = true;
        }
        else if (e.Key == global::Windows.System.VirtualKey.Escape)
        {
            HideWindow();
            InputTextBox.Text = string.Empty;
            e.Handled = true;
        }
    }

    private void HideWindow()
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
        var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        appWindow?.Hide();
    }

    private async void InputTextBox_Paste(object sender, TextControlPasteEventArgs e)
    {
        // Prevent default paste behavior
        e.Handled = true;

        var clipboardContent = await _clipboardService.DetectClipboardContentAsync();
        
        switch (clipboardContent.Type)
        {
            case ClipboardContentType.Text:
                InputTextBox.Text = clipboardContent.TextContent ?? string.Empty;
                break;
                
            case ClipboardContentType.Url:
                InputTextBox.Text = clipboardContent.UrlTitle ?? clipboardContent.Url ?? string.Empty;
                break;
                
            case ClipboardContentType.Image:
                // For images, just show a placeholder in the input
                InputTextBox.Text = $"Screenshot pasted: {Path.GetFileName(clipboardContent.ImagePath)}";
                break;
        }

        // Move cursor to end
        InputTextBox.SelectionStart = InputTextBox.Text.Length;
    }

    private async Task SaveNoteAsync()
    {
        var input = InputTextBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            HideWindow();
            InputTextBox.Text = string.Empty;
            return;
        }

        // Check if it's a screenshot paste operation
        var clipboardContent = await _clipboardService.DetectClipboardContentAsync();
        
        var note = new Note();

        // Parse slash command
        var (hasCommand, topic, content) = _slashCommandParser.Parse(input);
        
        if (hasCommand)
        {
            note.Topic = topic;
            note.Content = content ?? string.Empty;
            
            // Extract all topics for tags (supports semicolon-separated topics)
            var allTopics = _slashCommandParser.ExtractAllTopics(input);
            if (allTopics.Count > 1)
            {
                // Store all topics as tags
                note.Tags = string.Join(",", allTopics);
            }
        }
        else
        {
            note.Content = input;
        }

        // If we just pasted an image, use that
        if (clipboardContent.Type == ClipboardContentType.Image && input.StartsWith("Screenshot pasted:"))
        {
            note.Type = NoteType.Image;
            note.AttachmentPath = clipboardContent.ImagePath;
        }
        else if (clipboardContent.Type == ClipboardContentType.Url && input == clipboardContent.UrlTitle)
        {
            note.Type = NoteType.Link;
            note.AttachmentPath = clipboardContent.Url;
            note.Content = clipboardContent.UrlTitle ?? clipboardContent.Url ?? string.Empty;
        }
        else
        {
            note.Type = NoteType.Text;
        }

        await _databaseService.SaveNoteAsync(note);

        // Notify that a note was saved (to refresh timeline)
        _noteEventService.TriggerNoteSaved();

        // Clear and hide
        InputTextBox.Text = string.Empty;
        HideWindow();
    }
}

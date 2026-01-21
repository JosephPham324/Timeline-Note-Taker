using System.Text.RegularExpressions;
using Timeline_Note_Taker.Models;

namespace Timeline_Note_Taker.Services;

public class ClipboardService : IClipboardService
{
    private readonly IUrlMetadataService _urlMetadataService;
    private readonly ISettingsService _settingsService;
    private static readonly Regex UrlRegex = new Regex(
        @"^https?://[^\s/$.?#].[^\s]*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    public ClipboardService(IUrlMetadataService urlMetadataService, ISettingsService settingsService)
    {
        _urlMetadataService = urlMetadataService;
        _settingsService = settingsService;
    }

    public async Task<ClipboardContent> DetectClipboardContentAsync()
    {
        // Try Windows-specific image detection first (only on Windows)
        #if WINDOWS
        System.Diagnostics.Debug.WriteLine("[ClipboardService] Checking for Windows clipboard image...");
        if (Timeline_Note_Taker.Platforms.Windows.WindowsClipboard.HasImage())
        {
            System.Diagnostics.Debug.WriteLine("[ClipboardService] Image detected, saving...");
            var imagesDir = _settingsService.AttachmentsDirectory;
            var imagePath = await Timeline_Note_Taker.Platforms.Windows.WindowsClipboard.SaveClipboardImageAsync(imagesDir);
            
            if (!string.IsNullOrEmpty(imagePath))
            {
                System.Diagnostics.Debug.WriteLine($"[ClipboardService] Image saved to: {imagePath}");
                return new ClipboardContent
                {
                    Type = ClipboardContentType.Image,
                    ImagePath = imagePath
                };
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("[ClipboardService] Image save failed");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[ClipboardService] No image in clipboard");
        }
        #endif

        // Try to get text (works cross-platform)
        try
        {
            // In MAUI, we need to check if clipboard has content first
            var hasContent = Clipboard.Default.HasText;
            
            // For Windows, we'll try to get text first, then check if it's special
            var text = await Clipboard.Default.GetTextAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                text = text.Trim();

                // Check if it's a URL
                if (UrlRegex.IsMatch(text))
                {
                    var title = await _urlMetadataService.FetchPageTitleAsync(text);
                    return new ClipboardContent
                    {
                        Type = ClipboardContentType.Url,
                        Url = text,
                        UrlTitle = title ?? text
                    };
                }

                // Plain text
                return new ClipboardContent
                {
                    Type = ClipboardContentType.Text,
                    TextContent = text
                };
            }
        }
        catch
        {
            // Clipboard access failed or no content
        }

        // Default to empty text
        return new ClipboardContent
        {
            Type = ClipboardContentType.Text,
            TextContent = string.Empty
        };
    }

    private async Task<string> SaveImageToAppDataAsync(Stream imageStream)
    {
        var fileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        var imagesDir = Path.Combine(FileSystem.AppDataDirectory, "images");
        Directory.CreateDirectory(imagesDir);
        
        var filePath = Path.Combine(imagesDir, fileName);
        
        using var fileStream = File.Create(filePath);
        await imageStream.CopyToAsync(fileStream);
        
        return filePath;
    }
}

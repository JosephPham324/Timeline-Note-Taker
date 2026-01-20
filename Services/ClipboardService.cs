using System.Text.RegularExpressions;
using Timeline_Note_Taker.Models;

namespace Timeline_Note_Taker.Services;

public class ClipboardService : IClipboardService
{
    private readonly IUrlMetadataService _urlMetadataService;
    private static readonly Regex UrlRegex = new Regex(
        @"^https?://[^\s/$.?#].[^\s]*$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    public ClipboardService(IUrlMetadataService urlMetadataService)
    {
        _urlMetadataService = urlMetadataService;
    }

    public async Task<ClipboardContent> DetectClipboardContentAsync()
    {
        // Try to get image first (will throw if no image)
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

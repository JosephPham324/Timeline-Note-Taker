namespace Timeline_Note_Taker.Models;

public class ClipboardContent
{
    public ClipboardContentType Type { get; set; }
    public string? TextContent { get; set; }
    public string? ImagePath { get; set; }
    public string? Url { get; set; }
    public string? UrlTitle { get; set; }
}

public enum ClipboardContentType
{
    Text,
    Url,
    Image
}

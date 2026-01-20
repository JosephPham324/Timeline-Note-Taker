using Timeline_Note_Taker.Models;

namespace Timeline_Note_Taker.Services;

public interface IClipboardService
{
    Task<ClipboardContent> DetectClipboardContentAsync();
}

namespace Timeline_Note_Taker.Services;

public class SettingsService : ISettingsService
{
    private const string AttachmentsDirectoryKey = "AttachmentsDirectory";
    private readonly string _defaultAttachmentsDirectory;

    public SettingsService()
    {
        _defaultAttachmentsDirectory = Path.Combine(FileSystem.AppDataDirectory, "attachments");
        
        // Ensure default directory exists
        if (!Directory.Exists(_defaultAttachmentsDirectory))
        {
            Directory.CreateDirectory(_defaultAttachmentsDirectory);
        }
    }

    public string AttachmentsDirectory
    {
        get => Preferences.Get(AttachmentsDirectoryKey, _defaultAttachmentsDirectory);
        set
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            Preferences.Set(AttachmentsDirectoryKey, value);
            
            // Ensure directory exists when set
            if (!Directory.Exists(value))
            {
                try
                {
                    Directory.CreateDirectory(value);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Settings] Failed to create directory: {ex.Message}");
                }
            }
        }
    }
}

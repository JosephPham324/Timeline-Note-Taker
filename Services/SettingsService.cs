namespace Timeline_Note_Taker.Services;

public class SettingsService : ISettingsService
{
    private const string AttachmentsDirectoryKey = "AttachmentsDirectory";
    private const string LanguageKey = "Language";
    private const string TopicSeparatorKey = "TopicSeparator";
    
    private readonly string _defaultAttachmentsDirectory;
    private readonly string _defaultLanguage = "en";
    private readonly string _defaultTopicSeparator = ";";

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

    public string Language
    {
        get => Preferences.Get(LanguageKey, _defaultLanguage);
        set => Preferences.Set(LanguageKey, value);
    }

    public string TopicSeparator
    {
        get => Preferences.Get(TopicSeparatorKey, _defaultTopicSeparator);
        set
        {
            if (string.IsNullOrEmpty(value)) return; // Cannot be blank
            Preferences.Set(TopicSeparatorKey, value);
        }
    }
}

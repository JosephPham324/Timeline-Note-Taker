using System.Text.Json;

namespace Timeline_Note_Taker.Services;

public class LocalizationService
{
    private readonly ISettingsService _settingsService;
    private Dictionary<string, string> _currentStrings = new();
    private string _currentLanguage = "en";

    //public LocalizationService(ISettingsService settingsService)
    //{
    //    _settingsService = settingsService;
    //    LoadLanguage(_settingsService.Language);
    //}

    public string this[string key] => _currentStrings.TryGetValue(key, out var value) ? value : key;

   
    // We'll change the constructor to NOT load immediately or load default async in background.

    public event Action? OnLanguageChanged;

    public LocalizationService(ISettingsService settingsService)
    {
        _settingsService = settingsService;
        // Fire and forget load
        _ = InitializeAsync();
    }
    
    private async Task InitializeAsync()
    {
        await LoadLanguageAsync(_settingsService.Language);
    }

    public async Task LoadLanguageAsync(string languageCode)
    {
        _currentLanguage = languageCode;
        try
        {
            var fileName = $"Languages/{languageCode}.json";
            
            using var stream = await FileSystem.OpenAppPackageFileAsync(fileName);
            using var reader = new StreamReader(stream);
            var jsonContent = await reader.ReadToEndAsync();
            
            _currentStrings = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent) ?? new();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading language: {ex.Message}");
            _currentStrings = new();
        }
        
        OnLanguageChanged?.Invoke();
    }
}

using Microsoft.Extensions.Logging;
using Timeline_Note_Taker.Services;

namespace Timeline_Note_Taker;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

        // Register services
        builder.Services.AddSingleton<DatabaseService>();
        builder.Services.AddSingleton<IUrlMetadataService, UrlMetadataService>();
        builder.Services.AddSingleton<SlashCommandParser>();
        builder.Services.AddSingleton<NoteEventService>();
        builder.Services.AddSingleton<ISettingsService, SettingsService>();
        builder.Services.AddSingleton<LocalizationService>();
        builder.Services.AddSingleton<IClipboardService, ClipboardService>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}

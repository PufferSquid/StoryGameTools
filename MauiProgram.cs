using Microsoft.Extensions.Logging;
using StoryGameTools.Components;

namespace StoryGameTools
{
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
            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddSingleton<ILoggerService, LoggerService>();
            builder.Services.AddSingleton<CharacterDatabaseService>();
            builder.Services.AddSingleton<ScriptParserService>();
            builder.Services.AddTransient<FileService>();

#if DEBUG
    		builder.Services.AddBlazorWebViewDeveloperTools();
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}

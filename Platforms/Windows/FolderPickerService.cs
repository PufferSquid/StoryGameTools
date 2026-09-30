// Platforms/Windows/FolderPickerService.cs
using StoryGameTools.Components.Services;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace StoryGameTools.Platforms.Windows
{
    public class FolderPickerService : IFolderPickerService
    {
        public async Task<string?> PickFolderAsync()
        {
            var picker = new FolderPicker();
            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            picker.FileTypeFilter.Add("*");

            // MAUI on Windows requires the window handle to be passed to WinUI pickers
            var application = Application.Current
                ?? throw new InvalidOperationException("The current application is unavailable.");
            if (application.Windows.Count == 0)
                throw new InvalidOperationException("No application window is available.");

            var window = application.Windows[0]
                ?? throw new InvalidOperationException("The application window is unavailable.");
            var platformView = window.Handler?.PlatformView as MauiWinUIWindow
                ?? throw new InvalidOperationException("The native Windows window is unavailable.");
            var hwnd = platformView.WindowHandle;
            InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();
            return folder?.Path;
        }
    }
}
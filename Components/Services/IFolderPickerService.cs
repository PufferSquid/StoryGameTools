namespace StoryGameTools.Components.Services
{
    public interface IFolderPickerService
    {
        Task<string?> PickFolderAsync();
    }
}
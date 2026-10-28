using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace StoryGameTools.Components.Services
{
    public record SliceGridInfo(int ImageWidth, int ImageHeight, int SliceSize, int Columns, int Rows);

    public class ImageSlicerService
    {
        private readonly ProjectService _projectService;
        private readonly ILoggerService _logger;

        public ImageSlicerService(ProjectService projectService, ILoggerService logger)
        {
            _projectService = projectService;
            _logger = logger;
        }

        public SliceGridInfo BuildGridInfo(int width, int height, int sliceSize)
        {
            int cols = (int)Math.Ceiling(width / (double)sliceSize);
            int rows = (int)Math.Ceiling(height / (double)sliceSize);
            return new SliceGridInfo(width, height, sliceSize, cols, rows);
        }

        public async Task<List<string>> SliceAndSaveAsync(Image<Rgba32> source, int sliceSize)
        {
            var unhandledFolder = _projectService.GetProjectPath("images/unhandled")
                ?? throw new InvalidOperationException("No active project.");
            Directory.CreateDirectory(unhandledFolder);

            var saved = new List<string>();
            int cols = (int)Math.Ceiling(source.Width / (double)sliceSize);
            int rows = (int)Math.Ceiling(source.Height / (double)sliceSize);

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    int x = col * sliceSize;
                    int y = row * sliceSize;
                    int w = Math.Min(sliceSize, source.Width - x);
                    int h = Math.Min(sliceSize, source.Height - y);

                    using var cell = source.Clone(ctx => ctx.Crop(new Rectangle(x, y, w, h)));
                    if (!HasOpaquePixels(cell))
                        continue;

                    var fileName = $"slice_{Guid.NewGuid():N}.png";
                    await cell.SaveAsPngAsync(Path.Combine(unhandledFolder, fileName));
                    saved.Add(fileName);
                }
            }

            _logger.LogInformation($"Image slicer produced {saved.Count} non-empty slice(s).");
            return saved;
        }

        private static bool HasOpaquePixels(Image<Rgba32> cell)
        {
            bool found = false;
            cell.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height && !found; y++)
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = 0; x < row.Length; x++)
                    {
                        if (row[x].A > 0) { found = true; break; }
                    }
                }
            });
            return found;
        }
    }
}
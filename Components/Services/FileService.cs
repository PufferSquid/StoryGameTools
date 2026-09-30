using Microsoft.AspNetCore.Components.Forms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StoryGameTools.Components.Services;

namespace StoryGameTools.Components.Services
{
    public class FileService
    {
        public bool filesUploaded = false;
        const int MAX_FILESIZE = 10000 * 1024;

        private readonly ILoggerService _logger;

        private List<IBrowserFile> browserFiles = new List<IBrowserFile>();

        public FileService(ILoggerService logger)
        {
            _logger = logger;
        }


        public async Task FileUploaded(InputFileChangeEventArgs e)
        {
            browserFiles = (List<IBrowserFile>)e.GetMultipleFiles();
        }



        public async Task ConvertDocuments(List<IBrowserFile> browserFiles)
        {
            foreach (var browserFile in browserFiles)
            {
                _logger.LogInformation(browserFile.Name);


                if (browserFile == null)
                {
                    continue;
                }

                try
                {
                    var fileStream = browserFile.OpenReadStream(MAX_FILESIZE);
                    var randomFile = Path.GetRandomFileName();
                    var extension = Path.GetExtension(randomFile);
                    var targetFilePath = Path.ChangeExtension(randomFile, extension);
                    // above creates temporary file? Not needed? From: https://www.telerik.com/blogs/blazor-basics-uploading-files-blazor-server-web-applications

                    var destinationStream = new FileStream(targetFilePath, FileMode.Create);
                    await fileStream.CopyToAsync(destinationStream);
                    destinationStream.Close();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex);
                    continue;
                }
            }
        }

    }
}

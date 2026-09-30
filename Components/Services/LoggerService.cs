using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace StoryGameTools.Components.Services
{
    //Custom Logger service to wrap around builtin Logger
    public interface ILoggerService
    {
        void LogInformation(string message);
        void LogError(string message);
        void LogError(Exception exception, string message = "");
    }

    public class LoggerService : ILoggerService
    {
        private readonly ILogger<LoggerService> _logger;
        private readonly string _logDirectory;
        private readonly object _lockObject = new object();
        private const string PREFIX = "APP"; // Prefix for all log messages from this app


        public LoggerService(ILogger<LoggerService> logger)
        {
            _logger = logger;
            _logDirectory = Path.Combine(FileSystem.AppDataDirectory, "Logs");  // this ensures the log is placed inside this project from my understanding

            // make sure log directory exists and create one if there isn't
            Directory.CreateDirectory(_logDirectory);
            

            // could add a function that cleans up old logs (like - have a maximum of three logs at a time?)
        }


        public void LogInformation(string message)
        {
            var formattedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO: {message}";
            _logger.LogInformation($"{PREFIX} {message}");
            WriteToFile(formattedMessage);
        }


        public void LogError(string message)
        {
            var formattedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {message}";
            _logger.LogError($"{PREFIX} {message}");
            WriteToFile(formattedMessage);
        }


        // handles exceptions specifically
        public void LogError(Exception exception, string message = "")
        {
            var logMessage = message ?? exception.Message; // if input message is empty, use exception message
            var formattedMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR: {logMessage}{Environment.NewLine}Exception: {exception}";
            _logger.LogError(exception, $"{PREFIX} {message}" ?? exception.Message);
            WriteToFile(formattedMessage);
        }


        // Write logs to a file (this makes tracking logs and testing much much easier, since it allows you to "go back in time" and not be dependent on the consol
        // Good resource: https://learn.microsoft.com/en-us/dotnet/standard/io/how-to-write-text-to-a-file
        private void WriteToFile(string message)
        {
            lock (_lockObject)
            {
                try
                {
                    var logFile = Path.Combine(_logDirectory, $"app-{DateTime.Now:yyyyMMdd}.log");
                    File.AppendAllText(logFile, message + Environment.NewLine);
                }
                catch (Exception ex)
                {
                    // Fallback to console if file writing fails
                    _logger.LogError(ex, "Failed to write to log file");
                }
            }
        }
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;
using Serilog;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DirectoryCopy
{
    class Program
    {
        static AppSettings appSettings = new AppSettings();
        static RetryPolicy _fileAccessRetryPolicy;

        static void Main(string[] args)
        {

            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            var configuration = builder.Build();
            ConfigurationBinder.Bind(configuration.GetSection("AppSettings"), appSettings);

            Log.Logger = new LoggerConfiguration()
                .WriteTo.File("consoleapp.log")
                .CreateLogger();

            _fileAccessRetryPolicy = Policy
                .Handle<FileLoadException>()
                .Or<FileNotFoundException>()
                .Or<ArgumentException>()
                .Or<OutOfMemoryException>()
                .Or<IOException>()
                .WaitAndRetry(retryCount: 5, retryNumber => TimeSpan.FromMilliseconds(5000));


            //string[] files = Directory.GetFiles(appSettings.SourceDirectory, "*.pdf", SearchOption.AllDirectories);
            string[] files = Directory.GetFiles(appSettings.SourceDirectory, "*.jpg", SearchOption.AllDirectories);

            Parallel.ForEach(files, (f => {
                try
                {
                    string[] fileParts = f.Split('\\');
                    string fileName = fileParts[fileParts.Length - 1];

                    string[] matchExisting = Directory.GetFiles(appSettings.TargetDirectory, fileName);
                    if (matchExisting.Length > 0)
                    {
                        Log.Warning($"Found duplicate in target directory: {fileName}");
                    }
                    while (matchExisting.Length > 0)
                    {
                        string[] newfileNames = fileName.Split('.');
                        string newfileName = string.Empty;
                        for (int x = 0; x < newfileNames.Length - 1; x++)
                        {
                            newfileName = $"{newfileNames[x]}";
                        }
                        fileName = $"{newfileName}_1.{newfileNames[newfileNames.Length - 1]}";
                        matchExisting = Directory.GetFiles(appSettings.TargetDirectory, fileName);
                    }

                    _fileAccessRetryPolicy.Execute(() => { File.Copy(f, $"{appSettings.TargetDirectory}\\{fileName}", true); });
                    Log.Information($"Copied file: {f} to {appSettings.TargetDirectory}\\{fileName}");
                }
                catch (Exception ex)
                {
                    Log.Error($"Error: {ex.Message}");
                };
            }));

        }
    }
}

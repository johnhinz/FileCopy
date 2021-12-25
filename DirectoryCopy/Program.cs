using Microsoft.Extensions.Configuration;
using Polly;
using Polly.Retry;
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

            _fileAccessRetryPolicy = Policy
                .Handle<FileLoadException>()
                .Or<FileNotFoundException>()
                .Or<ArgumentException>()
                .Or<OutOfMemoryException>()
                .Or<IOException>()
                .WaitAndRetry(retryCount: 5, retryNumber => TimeSpan.FromMilliseconds(5000));


            string[] files = Directory.GetFiles(appSettings.SourceDirectory, "*.*", SearchOption.AllDirectories);

            Parallel.ForEach(files, (f => {
                try
                {
                    string[] fileParts = f.Split('\\');
                    string fileName = fileParts[fileParts.Length - 1];
                    _fileAccessRetryPolicy.Execute(() => { File.Copy(f, $"{appSettings.TargetDirectory}\\{fileName}", true); });
                }
                catch
                {

                };
            }));

        }
    }
}

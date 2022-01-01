using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using TriggeredFileCopy;


AppSettings appSettings = new AppSettings();

var builder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
var configuration = builder.Build();

ConfigurationBinder.Bind(configuration.GetSection("AppSettings"), appSettings);

Policy _fileAccessRetryPolicy = Policy
                .Handle<FileLoadException>()
                .Or<FileNotFoundException>()
                .Or<ArgumentException>()
                .Or<OutOfMemoryException>()
                .Or<IOException>()
                .WaitAndRetry(5, retryNumber => TimeSpan.FromMilliseconds(5000));


string[] files = Directory.GetFiles(appSettings.SourceDirectory, "*.*", SearchOption.AllDirectories);
string[] extensions = new string[] { ".jpg", ".pdf" };

List<FileInfo> histories = new List<FileInfo>();

Parallel.ForEach(files, file =>
{
    FileInfo fi = new FileInfo(file);
    if (extensions.Contains(fi.Extension))
    {
        if (fi.DirectoryName.IndexOf("recycle", StringComparison.OrdinalIgnoreCase) == -1)
        {
            histories.Add(fi);
        }
    }
});

FileCopyContext context = new FileCopyContext();

MqttAIPublish _mqttQueue = new MqttAIPublish("cthost.johnhinz.com", "TriggeredFileCopy", "\\.", 0, "FileCopy");

foreach (var file in histories)
{
    if (file == null)
        continue;

    var foundFile = context.Histories
        .Where(x => x.filename == file.Name)
        .Where(x => x.filesize == file.Length)
        .Where(x => x.filepath == file.DirectoryName).FirstOrDefault();

    if (foundFile == null)
    {
        _fileAccessRetryPolicy.Execute(() => { File.Copy(file.FullName, $"{appSettings.TargetDirectory}\\{file.Name}", true); });
        context.Histories.Add(
            new History()
            {
                filename = file.Name,
                filepath = file.DirectoryName,
                filesize = file.Length,
                verify = true
            });
        try
        {
            context.SaveChanges();
            Console.WriteLine($"{file.FullName} saved");
            Message message = new Message()
            {
                FileName = file.FullName
            };
            _mqttQueue.PublishAsync(message, "File", CancellationToken.None);
        }
        catch
        { }
    }
}

Console.WriteLine("Completed!");

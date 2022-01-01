using Microsoft.EntityFrameworkCore;
using TriggeredFileCopy;

_fileAccessRetryPolicy = Policy
                .Handle<FileLoadException>()
                .Or<FileNotFoundException>()
                .Or<ArgumentException>()
                .Or<OutOfMemoryException>()
                .Or<IOException>()
                .WaitAndRetry(retryCount: retryCount, retryNumber => TimeSpan.FromMilliseconds(retryDelay));

string[] files = Directory.GetFiles("Z:\\", "*.*", SearchOption.AllDirectories);
string[] extensions = new string[] { ".jpg", ".pdf" };

List<History> histories = new List<History>();

foreach (var file in files)
{
    FileInfo fi = new FileInfo(file);
    if (extensions.Contains(fi.Extension))
    {
        var historyFile = new History()
        {
            filename = fi.Name,
            filepath = fi.DirectoryName.Substring(2),
            filesize = fi.Length,
            verify = true
        };
        histories.Add(historyFile);
    }
}

FileCopyContext context = new FileCopyContext();

foreach (var file in histories)
{
    var foundFile = context.Histories
        .Where(x => x.filename == file.filename)
        .Where(x => x.filesize == file.filesize)
        .Where(x => x.filepath == file.filepath ).FirstOrDefault();

    if (foundFile != null)
    {
        Console.WriteLine($"{file.filename} found");
    }
    else
    {
        _fileAccessRetryPolicy.Execute(() => { File.Copy(e.FullPath, $"{_destPath}\\{e.Name}", true); });
        context.Histories.Add(file);
        try
        {
            context.SaveChanges();
            Console.WriteLine($"{file.filename} saved");
        } catch
        { }
    }
}

Console.ReadKey();

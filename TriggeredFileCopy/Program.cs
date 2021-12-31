// See https://aka.ms/new-console-template for more information
using TriggeredFileCopy;

Console.WriteLine("Hello, World!");

FileCopyContext context = new FileCopyContext();

History history = new History()
{
    filename = "XXX",
    filepath = "YYY",
    filesize = 123,
};

context.Histories.Add(history);
context.SaveChanges();

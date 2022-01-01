using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace TriggeredFileCopy
{
    internal class FileCopyContext : DbContext
    {
        public DbSet<History> Histories { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseMySql("server=cthost.johnhinz.com;database=FileCopy;user=XXX;password=YYY",
                    new MySqlServerVersion(new Version(10, 4, 17)))
                .UseLoggerFactory(LoggerFactory.Create(b => b
                    .AddConsole()
                    .AddFilter(level => level >= LogLevel.Error)))
                .EnableSensitiveDataLogging()
                .EnableDetailedErrors();
        }
    }
}

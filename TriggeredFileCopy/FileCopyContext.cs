using Microsoft.EntityFrameworkCore;

namespace TriggeredFileCopy
{
    internal class FileCopyContext : DbContext
    {
        public DbSet<History> Histories { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySQL("server=cthost.johnhinz.com;database=FileCopy;user=xxx;password=xxx");
        }
    }
}

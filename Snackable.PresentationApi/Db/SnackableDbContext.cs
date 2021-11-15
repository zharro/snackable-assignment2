using Microsoft.EntityFrameworkCore;

namespace Snackable.PresentationApi.Db
{
    public class SnackableDbContext : DbContext
    {
        public const string DbName = "PresentationApi";

        public SnackableDbContext(DbContextOptions<SnackableDbContext> options)
            : base(options)
        { }

        public DbSet<File> Files { get; set; }
        public DbSet<Tenant> Tenants { get; set; }
        public DbSet<FileSegment> Segments { get; set; }
    }
}
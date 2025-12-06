using Microsoft.EntityFrameworkCore;

namespace VirtualList.Datas;

public class MainDbContext(string connectionString, ILoggerFactory loggerFactory) : DbContext
{
    public DbSet<UserInfo> Users { get; set; }
    public DbSet<FileNamespaceInfo> FileNamespaces { get; set; }
    public DbSet<SharedFileInfo> SharedFiles { get; set; }
    public DbSet<LoginInfo> LoginInfos { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseSqlite(connectionString)
            .UseLoggerFactory(loggerFactory);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileNamespaceInfo>()
            .HasOne(e => e.Owner)
            .WithMany(e => e.OwnNamespaces)
            .HasForeignKey(e => e.OwnerName)
            .IsRequired();
        modelBuilder.Entity<FileNamespaceInfo>()
            .HasMany(e => e.ReadAccessUsers)
            .WithMany(e => e.ReadableNamespaces);
        modelBuilder.Entity<FileNamespaceInfo>()
            .HasMany(e => e.WriteAccessUsers)
            .WithMany(e => e.WriteableNamespaces);
    }
}

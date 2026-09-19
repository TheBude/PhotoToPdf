using Microsoft.EntityFrameworkCore;
using PhotoToPdf.Api.Models;

namespace PhotoToPdf.Api.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<UserHistoryItem> UserHistory => Set<UserHistoryItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.FullName).HasMaxLength(200).IsRequired();
            entity.Property(u => u.Email).HasMaxLength(200).IsRequired();
            entity.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
        });

        modelBuilder.Entity<UserHistoryItem>(entity =>
        {
            entity.Property(h => h.Operation).HasMaxLength(50).IsRequired();
            entity.Property(h => h.SourceFileName).HasMaxLength(255).IsRequired();
            entity.Property(h => h.OutputFileNames).HasMaxLength(2000);
            entity.HasOne(h => h.User)
                .WithMany(u => u.History)
                .HasForeignKey(h => h.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

using AuthSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace AuthSystem.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Refresh> Refreshs => Set<Refresh>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Name);
            e.HasIndex(u => u.Phone).IsUnique();
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Role).HasDefaultValue("user");

            e.HasMany(u => u.RefreshTokens)
             .WithOne(t => t.User)
             .HasForeignKey(t => t.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        b.Entity<RefreshToken>(e =>
        {
            e.HasKey(t => t.Id);

            // Lookup theo hash khi validate — phải nhanh
            e.HasIndex(t => t.TokenHash).IsUnique();

            // Revoke toàn bộ family theo FamilyId — phải nhanh
            e.HasIndex(t => t.FamilyId);

            // Cleanup job: tìm token hết hạn theo UserId
            e.HasIndex(t => new { t.UserId, t.ExpiresAt });

            // DeviceId nullable — client có thể không gửi
            e.Property(t => t.DeviceId).HasMaxLength(128);
            e.Property(t => t.IpAddress).HasMaxLength(45);   // IPv6 max
            e.Property(t => t.UserAgent).HasMaxLength(512);
            e.Property(t => t.RevokeReason).HasMaxLength(64);

            // Không map computed properties sang DB
            e.Ignore(t => t.IsExpired);
            e.Ignore(t => t.IsActive);
        });
    }
}
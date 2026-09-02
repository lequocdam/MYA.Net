public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Registration> Registrations => Set<Registration>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);

            e.Property(u => u.Name)
                .HasMaxLength(100)
                .IsRequired();

            e.Property(u => u.Phone)
                .HasMaxLength(16)
                .IsRequired();

            e.HasIndex(u => u.Phone)
                .IsUnique();
                .HasFilter("[IsDeleted] = 0");

            e.Property(u => u.Email)
                .HasMaxLength(255)
                .IsRequired();

            e.HasIndex(u => u.Email)
                .IsUnique();
                .HasFilter("[IsDeleted] = 0");

            e.Property(u => u.HashPassword)
                .HasMaxLength(512)
                .IsRequired();
        });

        b.Entity<Registration>(e =>
        {
            e.HasKey(x => x.Id);

            e.Property(x => x.Name)
                .HasMaxLength(100)
                .IsRequired();

            e.Property(x => x.Phone)
                .HasMaxLength(12)
                .IsRequired();

            e.HasIndex(x => x.Phone)
                .IsUnique();

            e.Property(x => x.Email)
                .HasMaxLength(255)
                .IsRequired();

            e.HasIndex(x => x.Email)
                .IsUnique();

            e.Property(x => x.HashPassword)
                .HasMaxLength(255)
                .IsRequired();
        });
    }
}
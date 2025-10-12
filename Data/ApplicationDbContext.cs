using Microsoft.EntityFrameworkCore;
using WebApp.Models;

namespace WebApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        // Add your DbSets here
        public DbSet<Banner> Banners { get; set; }
        // Example:
        // public DbSet<User> Users { get; set; }
        // public DbSet<Product> Products { get; set; }
        // public DbSet<Order> Orders { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // Configure your entities here
        modelBuilder.Entity<Banner>(entity =>
        {
            entity.ToTable("banners"); // Map to lowercase table name
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").IsRequired().HasMaxLength(120);
            entity.Property(e => e.Image).HasColumnName("image").IsRequired().HasMaxLength(255);
            entity.Property(e => e.Link).HasColumnName("link").HasMaxLength(255);
            entity.Property(e => e.Status).HasColumnName("status").HasDefaultValue((short)1);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("NOW()");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("NOW()");
        });
            // Example:
            // modelBuilder.Entity<User>().HasKey(u => u.Id);
            // modelBuilder.Entity<User>().Property(u => u.Name).IsRequired().HasMaxLength(100);
        }
    }
}

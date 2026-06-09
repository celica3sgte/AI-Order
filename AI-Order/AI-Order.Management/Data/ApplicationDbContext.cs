using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AI_Order.Management.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<MenuItemEntity> MenuItems => Set<MenuItemEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<MenuItemEntity>(e =>
        {
            e.HasKey(m => m.Id);
            e.Property(m => m.Id).HasMaxLength(36);
            e.Property(m => m.AspNetUserId).HasMaxLength(450).IsRequired();
            e.Property(m => m.Name).HasMaxLength(200).IsRequired();
            e.Property(m => m.Description).HasMaxLength(1000);
            e.Property(m => m.Price).HasColumnType("decimal(18,2)");
            e.Property(m => m.Category).HasMaxLength(100);
            e.Property(m => m.MainImage).HasMaxLength(500);
            e.Property(m => m.Image1).HasMaxLength(500);
            e.Property(m => m.Image2).HasMaxLength(500);
            e.Property(m => m.Image3).HasMaxLength(500);
            e.HasIndex(m => m.AspNetUserId);
        });

        // SQL Server cannot index nvarchar(max) — cap Identity key columns
        builder.Entity<ApplicationUser>().Property(u => u.Id).HasMaxLength(450);
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityRole>().Property(r => r.Id).HasMaxLength(450);
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>()
            .Property(l => l.LoginProvider).HasMaxLength(128);
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>()
            .Property(l => l.ProviderKey).HasMaxLength(128);
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>()
            .Property(t => t.LoginProvider).HasMaxLength(128);
        builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>()
            .Property(t => t.Name).HasMaxLength(128);
    }
}

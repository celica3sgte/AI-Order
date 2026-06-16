using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AI_Order.Management.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<MenuItemEntity> MenuItems => Set<MenuItemEntity>();
    public DbSet<RestaurantSettingsEntity> RestaurantSettings => Set<RestaurantSettingsEntity>();
    public DbSet<ModifierGroupEntity> ModifierGroups => Set<ModifierGroupEntity>();
    public DbSet<ModifierOptionEntity> ModifierOptions => Set<ModifierOptionEntity>();
    public DbSet<MenuItemModifierGroupEntity> MenuItemModifierGroups => Set<MenuItemModifierGroupEntity>();

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
            e.Property(m => m.IngredientsJsonAlt).HasDefaultValue("[]");
            e.Property(m => m.NameAlt).HasMaxLength(200);
            e.Property(m => m.DescriptionAlt).HasMaxLength(1000);
            e.HasIndex(m => m.AspNetUserId);
        });

        builder.Entity<ModifierGroupEntity>(e =>
        {
            e.ToTable("ModifierGroups");
            e.HasKey(g => g.Id);
            e.Property(g => g.AspNetUserId).HasMaxLength(450).IsRequired();
            e.Property(g => g.Name).HasMaxLength(200).IsRequired();
            e.Property(g => g.NameAlt).HasMaxLength(200);
            e.Property(g => g.DisplayName).HasMaxLength(200);
            e.Property(g => g.DisplayNameAlt).HasMaxLength(200);
            e.Property(g => g.SortOrder).HasDefaultValue(0);
            e.HasIndex(g => g.AspNetUserId);
        });

        builder.Entity<ModifierOptionEntity>(e =>
        {
            e.ToTable("ModifierOptions");
            e.HasKey(o => o.Id);
            e.Property(o => o.Name).HasMaxLength(200).IsRequired();
            e.Property(o => o.NameAlt).HasMaxLength(200);
            e.Property(o => o.PriceModifier).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
            e.Property(o => o.SortOrder).HasDefaultValue(0);
            e.HasOne(o => o.Group)
                .WithMany(g => g.Options)
                .HasForeignKey(o => o.ModifierGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<MenuItemModifierGroupEntity>(e =>
        {
            e.ToTable("MenuItemModifierGroups");
            e.HasKey(x => new { x.MenuItemId, x.ModifierGroupId });
            e.Property(x => x.MenuItemId).HasMaxLength(36);
            e.Property(x => x.SortOrder).HasDefaultValue(0);
            e.HasOne(x => x.MenuItem)
                .WithMany(m => m.ModifierGroupLinks)
                .HasForeignKey(x => x.MenuItemId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(x => x.ModifierGroup)
                .WithMany(g => g.MenuItemLinks)
                .HasForeignKey(x => x.ModifierGroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<RestaurantSettingsEntity>(e =>
        {
            e.HasKey(s => s.AspNetUserId);
            e.Property(s => s.AspNetUserId).HasMaxLength(450);
            e.Property(s => s.PrimaryLanguageCode).HasMaxLength(10).HasDefaultValue("en");
            e.Property(s => s.SecondaryLanguageCode).HasMaxLength(10);
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

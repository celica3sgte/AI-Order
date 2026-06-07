using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AI_Order.Management.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

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

using FieldOps.Identity.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FieldOps.Identity.Api.Data;

public sealed class IdentityDbContext : IdentityDbContext<ApplicationUser,IdentityRole<Guid>,Guid>
{
    public IdentityDbContext(
        DbContextOptions<IdentityDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(
        ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ApplicationUser>()
            .Property(x => x.FirstName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Entity<ApplicationUser>()
            .Property(x => x.LastName)
            .HasMaxLength(100)
            .IsRequired();
    }
}
using FieldOps.Technicians.Api.Models;
using Microsoft.EntityFrameworkCore;
namespace FieldOps.Technicians.Api.Data
{
    public sealed class TechniciansDbContext :DbContext
    {
        public TechniciansDbContext(
       DbContextOptions<TechniciansDbContext> options) : base(options)
        {
        }

        public DbSet<Technician> Technicians => Set<Technician>();

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var technician =
                modelBuilder.Entity<Technician>();

            technician.HasKey(x => x.Id);

            technician.Property(x => x.FirstName)
                .HasMaxLength(100)
                .IsRequired();

            technician.Property(x => x.LastName)
                .HasMaxLength(100)
                .IsRequired();

            technician.Property(x => x.Email)
                .HasMaxLength(320)
                .IsRequired();

            technician.Property(x => x.PhoneNumber)
                .HasMaxLength(30);

            technician.Property(x => x.Skills)
                .HasMaxLength(500);

            technician.HasIndex(x => x.Email)
                .IsUnique();

            technician.HasIndex(x => x.IsActive);
        }
    }
}

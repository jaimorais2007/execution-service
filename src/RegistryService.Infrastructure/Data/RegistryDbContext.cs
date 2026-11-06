using Microsoft.EntityFrameworkCore;
using RegistryService.Domain.Entities;

namespace RegistryService.Infrastructure.Data;

public class RegistryDbContext(DbContextOptions<RegistryDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.ToTable("Customers");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(80);
            entity.Property(e => e.Inactive);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.DateOfBirth);
            entity.Property(e => e.PersonType).HasConversion<string>().IsRequired();
            entity.OwnsOne(e => e.Document, doc =>
            {
                doc.Property(d => d.Value)
                   .HasColumnName("Document")
                   .IsRequired()
                   .HasMaxLength(50);
            });
            entity.HasMany(c => c.Vehicles)
                  .WithOne(v => v.Customer)
                  .HasForeignKey(v => v.CustomerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.ToTable("Vehicles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Brand).IsRequired().HasMaxLength(80);
            entity.Property(e => e.Model).IsRequired().HasMaxLength(80);
            entity.Property(e => e.Year).IsRequired();
            entity.Property(e => e.Inactive);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.OwnsOne(e => e.Plate, plate =>
            {
                plate.Property(p => p.Value)
                     .HasColumnName("Plate")
                     .IsRequired()
                     .HasMaxLength(10);
            });
            entity.HasOne(v => v.Customer)
                  .WithMany(c => c.Vehicles)
                  .HasForeignKey(v => v.CustomerId);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(150);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.Role).HasMaxLength(50);
            entity.Property(e => e.Inactive);
            entity.Property(e => e.CreatedAt).IsRequired();
        });
    }
}

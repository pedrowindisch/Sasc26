using Microsoft.EntityFrameworkCore;
using Sasc26.Models;

namespace Sasc26.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Session> Sessions => Set<Session>();
    public DbSet<Attendee> Attendees => Set<Attendee>();
    public DbSet<CheckIn> CheckIns => Set<CheckIn>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Attendee>(entity =>
        {
            entity.HasKey(e => e.Email);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Course).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Shift).IsRequired().HasMaxLength(50);
            entity.HasMany(e => e.CheckIns)
                  .WithOne(c => c.Attendee)
                  .HasForeignKey(c => c.AttendeeEmail)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Session>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.HasMany(e => e.CheckIns)
                  .WithOne(c => c.Session)
                  .HasForeignKey(c => c.SessionId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CheckIn>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OtpCode).IsRequired().HasMaxLength(6);
            entity.HasIndex(e => new { e.AttendeeEmail, e.SessionId });
        });
    }
}

using Microsoft.EntityFrameworkCore;
using Sasc26.Models;

namespace Sasc26.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<TimeSlot> TimeSlots => Set<TimeSlot>();
    public DbSet<Lecture> Lectures => Set<Lecture>();
    public DbSet<Attendee> Attendees => Set<Attendee>();
    public DbSet<CheckIn> CheckIns => Set<CheckIn>();
    public DbSet<PreRegistration> PreRegistrations => Set<PreRegistration>();
    public DbSet<Volunteer> Volunteers => Set<Volunteer>();
    public DbSet<VolunteerCheckIn> VolunteerCheckIns => Set<VolunteerCheckIn>();
    public DbSet<CertificateConfig> CertificateConfigs => Set<CertificateConfig>();
    public DbSet<IssuedCertificate> IssuedCertificates => Set<IssuedCertificate>();

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

        modelBuilder.Entity<TimeSlot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasMany(e => e.Lectures)
                  .WithOne(l => l.TimeSlot)
                  .HasForeignKey(l => l.TimeSlotId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Lecture>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Speaker).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Keyword1).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Keyword2).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Keyword3).IsRequired().HasMaxLength(50);
            entity.HasMany(e => e.CheckIns)
                  .WithOne(c => c.Lecture)
                  .HasForeignKey(c => c.LectureId)
                  .OnDelete(DeleteBehavior.SetNull);
            entity.HasMany(e => e.PreRegistrations)
                  .WithOne(p => p.Lecture)
                  .HasForeignKey(p => p.LectureId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CheckIn>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OtpCode).IsRequired().HasMaxLength(6);
        });

        modelBuilder.Entity<PreRegistration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AttendeeEmail).IsRequired().HasMaxLength(200);
            entity.Property(e => e.OtpCode).IsRequired().HasMaxLength(6);
            entity.HasIndex(e => new { e.LectureId, e.AttendeeEmail }).IsUnique();
        });

        modelBuilder.Entity<Volunteer>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Course).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Shift).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasMany(e => e.CheckIns)
                  .WithOne(c => c.Volunteer)
                  .HasForeignKey(c => c.VolunteerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<VolunteerCheckIn>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.VolunteerId, e.TimeSlotId }).IsUnique();
        });

        modelBuilder.Entity<CertificateConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TemplateMessage).IsRequired();
            entity.Property(e => e.BackgroundImageContentType).HasMaxLength(100);
        });

        modelBuilder.Entity<IssuedCertificate>(entity =>
        {
            entity.HasKey(e => e.ValidationCode);
            entity.Property(e => e.ValidationCode).HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Course).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Phase).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Email).IsUnique();
        });
    }
}

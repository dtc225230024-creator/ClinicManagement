using ClinicManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace ClinicManagement.Data;

public class ClinicDbContext(DbContextOptions<ClinicDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<AiSymptomRule> AiSymptomRules => Set<AiSymptomRule>();
    public DbSet<DoctorProfile> Doctors => Set<DoctorProfile>();
    public DbSet<WorkSchedule> WorkSchedules => Set<WorkSchedule>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<ClinicService> Services => Set<ClinicService>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceDetail> InvoiceDetails => Set<InvoiceDetail>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.HasKey(x => x.UserId);
            entity.HasIndex(x => x.Username).IsUnique();
            entity.HasIndex(x => x.DoctorId).IsUnique();
            entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(20);
            entity.Property(x => x.Username).HasMaxLength(50);
            entity.Property(x => x.Password).HasMaxLength(100);
            entity.Property(x => x.MustChangePassword).HasDefaultValue(false);
            entity.Property(x => x.ManualSeenVersion).HasMaxLength(20);
            entity.HasOne<DoctorProfile>()
                .WithMany()
                .HasForeignKey(x => x.DoctorId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(x => x.DepartmentId);
            entity.Property(x => x.DepartmentName).HasMaxLength(100);
        });

        modelBuilder.Entity<AiSymptomRule>(entity =>
        {
            entity.HasKey(x => x.AiSymptomRuleId);
            entity.Property(x => x.Term).HasMaxLength(120);
            entity.Property(x => x.NormalizedTerm).HasMaxLength(120);
            entity.Property(x => x.Score).HasDefaultValue(10);
            entity.Property(x => x.IsActive).HasDefaultValue(true);
            entity.HasIndex(x => new { x.DepartmentId, x.NormalizedTerm }).IsUnique();
            entity.HasOne<Department>()
                .WithMany()
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DoctorProfile>(entity =>
        {
            entity.HasKey(x => x.DoctorId);
            entity.Property(x => x.FullName).HasMaxLength(100);
            entity.Property(x => x.Gender).HasMaxLength(10);
            entity.Property(x => x.Phone).HasMaxLength(15);
            entity.Property(x => x.Email).HasMaxLength(100);
            entity.HasOne<Department>()
                .WithMany()
                .HasForeignKey(x => x.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<WorkSchedule>(entity =>
        {
            entity.HasKey(x => x.ScheduleId);
            entity.HasOne<DoctorProfile>()
                .WithMany()
                .HasForeignKey(x => x.DoctorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Patient>(entity =>
        {
            entity.HasKey(x => x.PatientId);
            entity.HasIndex(x => x.Phone).IsUnique();
            entity.Property(x => x.FullName).HasMaxLength(100);
            entity.Property(x => x.Gender).HasMaxLength(10);
            entity.Property(x => x.Phone).HasMaxLength(15);
            entity.Property(x => x.Address).HasMaxLength(255);
            entity.Property(x => x.IdentityNumber).HasMaxLength(20);
        });

        modelBuilder.Entity<Appointment>(entity =>
        {
            entity.HasKey(x => x.AppointmentId);
            entity.Property(x => x.TimeSlot).HasMaxLength(20);
            entity.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
            entity.HasIndex(x => new { x.DoctorId, x.AppointmentDate, x.TimeSlot });
            entity.HasOne<Patient>().WithMany().HasForeignKey(x => x.PatientId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<DoctorProfile>().WithMany().HasForeignKey(x => x.DoctorId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Department>().WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<MedicalRecord>(entity =>
        {
            entity.HasKey(x => x.RecordId);
            entity.HasIndex(x => x.AppointmentId).IsUnique();
            entity.HasOne<Appointment>().WithOne().HasForeignKey<MedicalRecord>(x => x.AppointmentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ClinicService>(entity =>
        {
            entity.HasKey(x => x.ServiceId);
            entity.ToTable("Services");
            entity.Property(x => x.ServiceName).HasMaxLength(100);
            entity.Property(x => x.Price).HasPrecision(12, 2);
        });

        modelBuilder.Entity<Invoice>(entity =>
        {
            entity.HasKey(x => x.InvoiceId);
            entity.HasIndex(x => x.AppointmentId).IsUnique();
            entity.Property(x => x.TotalAmount).HasPrecision(12, 2);
            entity.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(20);
            entity.HasOne<Appointment>().WithOne().HasForeignKey<Invoice>(x => x.AppointmentId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<InvoiceDetail>(entity =>
        {
            entity.HasKey(x => x.InvoiceDetailId);
            entity.Property(x => x.ServiceName).HasMaxLength(100);
            entity.Property(x => x.UnitPrice).HasPrecision(12, 2);
            entity.Property(x => x.LineTotal).HasPrecision(12, 2);
            entity.HasOne<Invoice>().WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<ClinicService>().WithMany().HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}

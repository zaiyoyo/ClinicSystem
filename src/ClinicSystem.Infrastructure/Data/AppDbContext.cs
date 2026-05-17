using ClinicSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicSystem.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();
    public DbSet<SpecialSchedule> SpecialSchedules => Set<SpecialSchedule>();
    public DbSet<ChargeItem> ChargeItems => Set<ChargeItem>();
    public DbSet<Drug> Drugs => Set<Drug>();
    public DbSet<MedicalRecord> MedicalRecords => Set<MedicalRecord>();
    public DbSet<Prescription> Prescriptions => Set<Prescription>();
    public DbSet<PrescriptionItem> PrescriptionItems => Set<PrescriptionItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentItem> PaymentItems => Set<PaymentItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // === User ===
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Username).IsUnique();
            e.Property(u => u.Username).HasMaxLength(50).IsRequired();
            e.Property(u => u.PasswordHash).IsRequired();
            e.Property(u => u.DisplayName).HasMaxLength(50).IsRequired();
            e.Property(u => u.Phone).HasMaxLength(20);
            e.Property(u => u.Email).HasMaxLength(100);
            e.Property(u => u.Title).HasMaxLength(50);
            e.Property(u => u.Introduction).HasMaxLength(500);
            e.HasOne(u => u.Department).WithMany(d => d.Doctors).HasForeignKey(u => u.DepartmentId).OnDelete(DeleteBehavior.SetNull);
        });

        // === Department ===
        modelBuilder.Entity<Department>(e =>
        {
            e.Property(d => d.Name).HasMaxLength(50).IsRequired();
            e.Property(d => d.Description).HasMaxLength(200);
        });

        // === Patient ===
        modelBuilder.Entity<Patient>(e =>
        {
            e.HasIndex(p => p.Phone);
            e.HasIndex(p => p.IdCard);
            e.Property(p => p.Name).HasMaxLength(50).IsRequired();
            e.Property(p => p.Phone).HasMaxLength(20);
            e.Property(p => p.IdCard).HasMaxLength(18);
            e.Property(p => p.Address).HasMaxLength(200);
            e.Property(p => p.MedicalInsuranceNo).HasMaxLength(50);
            e.Property(p => p.Allergies).HasMaxLength(500);
            e.Property(p => p.MedicalHistory).HasMaxLength(1000);
            e.Property(p => p.Remark).HasMaxLength(500);
            e.HasQueryFilter(p => !p.IsDeleted); // 软删除全局过滤
        });

        // === Appointment ===
        modelBuilder.Entity<Appointment>(e =>
        {
            e.HasIndex(a => a.AppointmentNo).IsUnique();
            e.Property(a => a.AppointmentNo).HasMaxLength(20).IsRequired();
            e.Property(a => a.TimeSlot).HasMaxLength(20);
            e.Property(a => a.Source).HasMaxLength(20);
            e.Property(a => a.Remark).HasMaxLength(200);
            e.Property(a => a.CancelReason).HasMaxLength(200);
            e.HasOne(a => a.Patient).WithMany(p => p.Appointments).HasForeignKey(a => a.PatientId);
            e.HasOne(a => a.Doctor).WithMany(u => u.Appointments).HasForeignKey(a => a.DoctorId).OnDelete(DeleteBehavior.NoAction);
            e.HasOne(a => a.Department).WithMany().HasForeignKey(a => a.DepartmentId).OnDelete(DeleteBehavior.SetNull);
        });

        // === DoctorSchedule ===
        modelBuilder.Entity<DoctorSchedule>(e =>
        {
            e.Property(d => d.TimeSlot).HasMaxLength(20);
        });

        // === SpecialSchedule ===
        modelBuilder.Entity<SpecialSchedule>(e =>
        {
            e.Property(s => s.TimeSlot).HasMaxLength(20);
            e.Property(s => s.Reason).HasMaxLength(200);
        });

        // === ChargeItem ===
        modelBuilder.Entity<ChargeItem>(e =>
        {
            e.HasIndex(c => c.Code).IsUnique();
            e.Property(c => c.Code).HasMaxLength(20).IsRequired();
            e.Property(c => c.Name).HasMaxLength(100).IsRequired();
            e.Property(c => c.Category).HasMaxLength(20);
            e.Property(c => c.Unit).HasMaxLength(10);
            e.Property(c => c.Remark).HasMaxLength(200);
        });

        // === Drug ===
        modelBuilder.Entity<Drug>(e =>
        {
            e.HasIndex(d => d.Code).IsUnique();
            e.Property(d => d.Code).HasMaxLength(20).IsRequired();
            e.Property(d => d.Name).HasMaxLength(100).IsRequired();
            e.Property(d => d.CommonName).HasMaxLength(100);
            e.Property(d => d.Specification).HasMaxLength(100);
            e.Property(d => d.Manufacturer).HasMaxLength(100);
            e.Property(d => d.Category).HasMaxLength(20);
            e.Property(d => d.DosageForm).HasMaxLength(20);
            e.Property(d => d.Unit).HasMaxLength(10);
            e.Property(d => d.Remark).HasMaxLength(200);
        });

        // === MedicalRecord ===
        modelBuilder.Entity<MedicalRecord>(e =>
        {
            e.HasIndex(m => m.RecordNo).IsUnique();
            e.Property(m => m.RecordNo).HasMaxLength(20).IsRequired();
            e.Property(m => m.ChiefComplaint).HasMaxLength(2000);
            e.Property(m => m.PresentIllness).HasMaxLength(4000);
            e.Property(m => m.PastHistory).HasMaxLength(2000);
            e.Property(m => m.PhysicalExamination).HasMaxLength(4000);
            e.Property(m => m.AuxiliaryExamination).HasMaxLength(2000);
            e.Property(m => m.TcmObservation).HasMaxLength(2000);
            e.Property(m => m.TcmAuscultation).HasMaxLength(2000);
            e.Property(m => m.TcmInquiry).HasMaxLength(2000);
            e.Property(m => m.TcmPalpation).HasMaxLength(2000);
            e.Property(m => m.WesternDiagnosis).HasMaxLength(500);
            e.Property(m => m.TcmDiagnosis).HasMaxLength(500);
            e.Property(m => m.TcmSyndrome).HasMaxLength(500);
            e.Property(m => m.DoctorAdvice).HasMaxLength(2000);
            e.Property(m => m.Remark).HasMaxLength(500);

            // SQL Server 不支持多重级联路径，全部改为 NoAction
            e.HasOne(m => m.Patient)
                .WithMany(p => p.MedicalRecords)
                .HasForeignKey(m => m.PatientId)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(m => m.Doctor)
                .WithMany(u => u.MedicalRecords)
                .HasForeignKey(m => m.DoctorId)
                .OnDelete(DeleteBehavior.NoAction);
            e.HasOne(m => m.Appointment)
                .WithMany()
                .HasForeignKey(m => m.AppointmentId)
                .OnDelete(DeleteBehavior.NoAction);
        });

        // === Prescription ===
        modelBuilder.Entity<Prescription>(e =>
        {
            e.HasIndex(p => p.PrescriptionNo).IsUnique();
            e.Property(p => p.PrescriptionNo).HasMaxLength(20).IsRequired();
            e.Property(p => p.Type).HasMaxLength(20);
            e.Property(p => p.DecoctingMethod).HasMaxLength(200);
            e.Property(p => p.Direction).HasMaxLength(500);
            e.Property(p => p.Status).HasMaxLength(20);
        });

        // === PrescriptionItem ===
        modelBuilder.Entity<PrescriptionItem>(e =>
        {
            e.Property(p => p.DrugName).HasMaxLength(100).IsRequired();
            e.Property(p => p.Specification).HasMaxLength(100);
            e.Property(p => p.Dosage).HasMaxLength(50);
            e.Property(p => p.Frequency).HasMaxLength(20);
            e.Property(p => p.Usage).HasMaxLength(50);
            e.Property(p => p.Days).HasMaxLength(20);
            e.Property(p => p.Remark).HasMaxLength(200);
        });

        // === Payment ===
        modelBuilder.Entity<Payment>(e =>
        {
            e.HasIndex(p => p.PaymentNo).IsUnique();
            e.Property(p => p.PaymentNo).HasMaxLength(20).IsRequired();
            e.Property(p => p.PaymentMethod).HasMaxLength(20);
            e.Property(p => p.Status).HasMaxLength(20);
            e.Property(p => p.Remark).HasMaxLength(200);
        });

        // === PaymentItem ===
        modelBuilder.Entity<PaymentItem>(e =>
        {
            e.Property(p => p.ItemType).HasMaxLength(20).IsRequired();
            e.Property(p => p.ItemName).HasMaxLength(100).IsRequired();
        });

        // Seed data: 默认管理员
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = 1,
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            DisplayName = "系统管理员",
            Role = Domain.Enums.UserRole.Admin,
            IsActive = true,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}

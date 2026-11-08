using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using RentCar.Models.Database;

namespace RentCar.Data;

public partial class RentCarDbContext : DbContext
{
    public RentCarDbContext(DbContextOptions<RentCarDbContext> options)
        : base(options)
    {
    }

    public DbSet<Brand> Brands { get; set; }

    public virtual DbSet<AppUser> AppUsers { get; set; }

    public virtual DbSet<AuditLog> AuditLogs { get; set; }

    public virtual DbSet<ContractHistory> ContractHistories { get; set; }

    public virtual DbSet<Customer> Customers { get; set; }

    public virtual DbSet<DriverLicense> DriverLicenses { get; set; }

    public virtual DbSet<HandoverImage> HandoverImages { get; set; }

    public virtual DbSet<HandoverRecord> HandoverRecords { get; set; }

    public virtual DbSet<MaintenanceRecord> MaintenanceRecords { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<PaymentTransaction> PaymentTransactions { get; set; }

    public virtual DbSet<PickupLocation> PickupLocations { get; set; }

    public virtual DbSet<PriceRule> PriceRules { get; set; }

    public virtual DbSet<Promotion> Promotions { get; set; }

    public virtual DbSet<RentalContract> RentalContracts { get; set; }

    public virtual DbSet<RentalRequest> RentalRequests { get; set; }

    public virtual DbSet<SurchargePolicy> SurchargePolicies { get; set; }

    public virtual DbSet<Vehicle> Vehicles { get; set; }

    public virtual DbSet<VehicleImage> VehicleImages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Brand>().ToTable("Brand");

        modelBuilder.Entity<AppUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AppUser__3214EC07A7D791F6");

            entity.ToTable("AppUser");

            entity.HasIndex(e => e.Email, "UQ__AppUser__A9D10534A61AE0A6").IsUnique();

            entity.Property(e => e.Email).HasMaxLength(254);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.LockedUntil).HasPrecision(0);
            entity.Property(e => e.PasswordHash).HasMaxLength(512);
            entity.Property(e => e.ResetTokenExpiresAt).HasPrecision(0);
            entity.Property(e => e.ResetTokenHash).HasMaxLength(512);
            entity.Property(e => e.Role).HasMaxLength(40);

            entity.HasOne(d => d.Customer).WithMany(p => p.AppUsers)
                .HasForeignKey(d => d.CustomerId)
                .HasConstraintName("FK__AppUser__Custome__48CFD27E");
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__AuditLog__3214EC074C7D64C8");

            entity.ToTable("AuditLog");

            entity.Property(e => e.Action).HasMaxLength(40);
            entity.Property(e => e.CreatedAt).HasPrecision(0);
            entity.Property(e => e.ObjectType).HasMaxLength(60);

            entity.HasOne(d => d.User).WithMany(p => p.AuditLogs)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__AuditLog__UserId__114A936A");
        });

        modelBuilder.Entity<ContractHistory>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Contract__3214EC07D8D2DDA1");

            entity.ToTable("ContractHistory");

            entity.Property(e => e.ChangedAt).HasPrecision(0);
            entity.Property(e => e.NewStatus).HasMaxLength(30);
            entity.Property(e => e.Note).HasMaxLength(500);
            entity.Property(e => e.OldStatus).HasMaxLength(30);

            entity.HasOne(d => d.ChangedByNavigation).WithMany(p => p.ContractHistories)
                .HasForeignKey(d => d.ChangedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ContractH__Chang__7A672E12");

            entity.HasOne(d => d.Contract).WithMany(p => p.ContractHistories)
                .HasForeignKey(d => d.ContractId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__ContractH__Contr__797309D9");
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Customer__3214EC07A26B2F2F");

            entity.ToTable("Customer");

            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.Email).HasMaxLength(254);
            entity.Property(e => e.FullName).HasMaxLength(150);
            entity.Property(e => e.IdentityNumber).HasMaxLength(30);
            entity.Property(e => e.Phone).HasMaxLength(25);
        });

        modelBuilder.Entity<DriverLicense>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__DriverLi__3214EC0797D036DC");

            entity.ToTable("DriverLicense");

            entity.HasIndex(e => e.CustomerId, "UQ__DriverLi__A4AE64D97B8771E3").IsUnique();

            entity.Property(e => e.LicenseClass).HasMaxLength(10);
            entity.Property(e => e.LicenseNumber).HasMaxLength(30);

            entity.HasOne(d => d.Customer).WithOne(p => p.DriverLicense)
                .HasForeignKey<DriverLicense>(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__DriverLic__Custo__44FF419A");
        });

        modelBuilder.Entity<HandoverImage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Handover__3214EC07ADE0B325");

            entity.ToTable("HandoverImage");

            entity.Property(e => e.ImageUrl).HasMaxLength(2048);

            entity.HasOne(d => d.Handover).WithMany(p => p.HandoverImages)
                .HasForeignKey(d => d.HandoverId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__HandoverI__Hando__6C190EBB");
        });

        modelBuilder.Entity<HandoverRecord>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Handover__3214EC07B22E14FC");

            entity.ToTable("HandoverRecord");

            entity.HasIndex(e => new { e.ContractId, e.RecordType }, "UQ__Handover__28CA16B603E71937").IsUnique();

            entity.Property(e => e.Accessories).HasMaxLength(500);
            entity.Property(e => e.ConditionNote).HasMaxLength(1000);
            entity.Property(e => e.RecordType).HasMaxLength(20);
            entity.Property(e => e.RecordedAt).HasPrecision(0);

            entity.HasOne(d => d.Contract).WithMany(p => p.HandoverRecords)
                .HasForeignKey(d => d.ContractId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__HandoverR__Contr__656C112C");

            entity.HasOne(d => d.RecordedByNavigation).WithMany(p => p.HandoverRecords)
                .HasForeignKey(d => d.RecordedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__HandoverR__Recor__6754599E");
        });

        modelBuilder.Entity<MaintenanceRecord>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Maintena__3214EC0795E4F113");

            entity.ToTable("MaintenanceRecord");

            entity.Property(e => e.Cost).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.EndAt).HasPrecision(0);
            entity.Property(e => e.MaintenanceType).HasMaxLength(50);
            entity.Property(e => e.StartAt).HasPrecision(0);
            entity.Property(e => e.Status).HasMaxLength(30);

            entity.HasOne(d => d.Vehicle).WithMany(p => p.MaintenanceRecords)
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Maintenan__Vehic__74AE54BC");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Notifica__3214EC07FA797EE7");

            entity.ToTable("Notification");

            entity.Property(e => e.Mode).HasMaxLength(20);
            entity.Property(e => e.SentAt).HasPrecision(0);
            entity.Property(e => e.Type).HasMaxLength(40);

            entity.HasOne(d => d.Contract).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.ContractId)
                .HasConstraintName("FK__Notificat__Contr__0D7A0286");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Notificat__UserI__0C85DE4D");
        });

        modelBuilder.Entity<PaymentTransaction>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PaymentT__3214EC074ADBC631");

            entity.ToTable("PaymentTransaction");

            entity.Property(e => e.Amount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.CreatedAt).HasPrecision(0);
            entity.Property(e => e.Method).HasMaxLength(30);
            entity.Property(e => e.PaymentType).HasMaxLength(20);
            entity.Property(e => e.TransactionCode).HasMaxLength(100);

            entity.HasOne(d => d.Contract).WithMany(p => p.PaymentTransactions)
                .HasForeignKey(d => d.ContractId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PaymentTr__Contr__6EF57B66");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.PaymentTransactions)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__PaymentTr__Creat__71D1E811");
        });

        modelBuilder.Entity<PickupLocation>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PickupLo__3214EC0754BD0BE2");

            entity.ToTable("PickupLocation");

            entity.Property(e => e.Address).HasMaxLength(300);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.Name).HasMaxLength(120);
        });

        modelBuilder.Entity<PriceRule>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__PriceRul__3214EC0784F5493D");

            entity.ToTable("PriceRule");

            entity.Property(e => e.Multiplier).HasColumnType("decimal(9, 4)");
            entity.Property(e => e.VehicleType).HasMaxLength(50);
        });

        modelBuilder.Entity<Promotion>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Promotio__3214EC07FF22C4CD");

            entity.ToTable("Promotion");

            entity.HasIndex(e => e.Code, "UQ__Promotio__A25C5AA782E6D683").IsUnique();

            entity.Property(e => e.Code).HasMaxLength(40);
            entity.Property(e => e.DiscountPercent).HasColumnType("decimal(5, 2)");
            entity.Property(e => e.IsActive).HasDefaultValue(true);
        });

        modelBuilder.Entity<RentalContract>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RentalCo__3214EC0754141ED4");

            entity.ToTable("RentalContract");

            entity.HasIndex(e => e.ContractCode, "UQ__RentalCo__CBECF833695387BB").IsUnique();

            entity.HasIndex(e => e.RentalRequestId, "UQ__RentalCo__FD28470458DC296A").IsUnique();

            entity.Property(e => e.ActualReturnAt).HasPrecision(0);
            entity.Property(e => e.CancelReason).HasMaxLength(500);
            entity.Property(e => e.ContractCode).HasMaxLength(40);
            entity.Property(e => e.DepositAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.DiscountAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.EndAt).HasPrecision(0);
            entity.Property(e => e.ExpiresAt).HasPrecision(0);
            entity.Property(e => e.RentalPrice).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.StartAt).HasPrecision(0);
            entity.Property(e => e.Status).HasMaxLength(30);

            entity.HasOne(d => d.Customer).WithMany(p => p.RentalContracts)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RentalCon__Custo__5AEE82B9");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.RentalContracts)
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RentalCon__Vehic__5BE2A6F2");

            entity.HasOne(d => d.RentalRequest).WithMany(p => p.RentalContracts)
                .HasPrincipalKey(p => new { p.Id, p.CustomerId, p.VehicleId })
                .HasForeignKey(d => new { d.RentalRequestId, d.CustomerId, d.VehicleId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RentalContract__619B8048");
        });

        modelBuilder.Entity<RentalRequest>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__RentalRe__3214EC071397B613");

            entity.ToTable("RentalRequest");

            entity.HasIndex(e => new { e.Id, e.CustomerId, e.VehicleId }, "UQ__RentalRe__3B19611F80DC7D40").IsUnique();

            entity.Property(e => e.CreatedAt).HasPrecision(0);
            entity.Property(e => e.EndAt).HasPrecision(0);
            entity.Property(e => e.ExpiresAt).HasPrecision(0);
            entity.Property(e => e.StartAt).HasPrecision(0);
            entity.Property(e => e.Status).HasMaxLength(30);

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.RentalRequests)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RentalReq__Creat__5535A963");

            entity.HasOne(d => d.Customer).WithMany(p => p.RentalRequests)
                .HasForeignKey(d => d.CustomerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RentalReq__Custo__5165187F");

            entity.HasOne(d => d.PickupLocation).WithMany(p => p.RentalRequestPickupLocations)
                .HasForeignKey(d => d.PickupLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RentalReq__Picku__534D60F1");

            entity.HasOne(d => d.ReturnLocation).WithMany(p => p.RentalRequestReturnLocations)
                .HasForeignKey(d => d.ReturnLocationId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RentalReq__Retur__5441852A");

            entity.HasOne(d => d.Vehicle).WithMany(p => p.RentalRequests)
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__RentalReq__Vehic__52593CB8");
        });

        modelBuilder.Entity<SurchargePolicy>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Surcharg__3214EC07CBF29AEF");

            entity.ToTable("SurchargePolicy");

            entity.HasIndex(e => e.VehicleType, "UQ__Surcharg__8E8EABE5EC5DF734").IsUnique();

            entity.Property(e => e.ExtraKmRate).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.VehicleType).HasMaxLength(50);
        });

        modelBuilder.Entity<Vehicle>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Vehicle__3214EC07873F385C");

            entity.ToTable("Vehicle");

            entity.HasIndex(e => e.PlateNumber, "UQ__Vehicle__03692624313C35DF").IsUnique();

            entity.Property(e => e.Brand).HasMaxLength(80);
            entity.Property(e => e.DailyRate).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.DepositAmount).HasColumnType("decimal(18, 2)");
            entity.Property(e => e.FuelType).HasMaxLength(30);
            entity.Property(e => e.Model).HasMaxLength(80);
            entity.Property(e => e.PlateNumber).HasMaxLength(20);
            entity.Property(e => e.Status).HasMaxLength(30);
            entity.Property(e => e.Transmission).HasMaxLength(30);
            entity.Property(e => e.VehicleType).HasMaxLength(50);
            entity.Property(e => e.WeeklyRate).HasColumnType("decimal(18, 2)");
        });

        modelBuilder.Entity<VehicleImage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__VehicleI__3214EC078EA84D6C");

            entity.ToTable("VehicleImage");

            entity.Property(e => e.ImageUrl).HasMaxLength(2048);

            entity.HasOne(d => d.Vehicle).WithMany(p => p.VehicleImages)
                .HasForeignKey(d => d.VehicleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__VehicleIm__Vehic__3E52440B");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

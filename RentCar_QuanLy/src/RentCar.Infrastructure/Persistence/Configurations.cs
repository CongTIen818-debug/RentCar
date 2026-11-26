using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RentCar.Domain.Entities;

namespace RentCar.Infrastructure.Persistence;

// Ánh xạ tên thuộc tính C# sang tên cột tiếng Việt không dấu của CSDL RentCarDb.
// Bảng có trigger phải khai báo HasTrigger: nếu không, EF Core không thể dùng mệnh đề OUTPUT khi INSERT/UPDATE.

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> b)
    {
        b.ToTable("vaiTro", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("idVaiTro");
        b.Property(x => x.Name).HasColumnName("tenVaiTro").HasMaxLength(50);
        b.Property(x => x.Code).HasColumnName("maVaiTro").HasMaxLength(20).IsUnicode(false);
    }
}

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> b)
    {
        b.ToTable("nguoiDung", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("idNguoiDung");
        b.Property(x => x.FullName).HasColumnName("hoTen").HasMaxLength(100);
        b.Property(x => x.Email).HasColumnName("email").HasMaxLength(100).IsUnicode(false);
        b.Property(x => x.PasswordHash).HasColumnName("matKhau").HasMaxLength(512).IsUnicode(false);
        b.Property(x => x.BirthDate).HasColumnName("ngaySinh");
        b.Property(x => x.Phone).HasColumnName("soDienThoai").HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.IdNumber).HasColumnName("cccd").HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.Address).HasColumnName("diaChi").HasMaxLength(255);
        b.Property(x => x.IsActive).HasColumnName("trangThai");
        b.Property(x => x.FailedAttempts).HasColumnName("soLanSai");
        b.Property(x => x.LockedUntil).HasColumnName("khoaDen");
        b.Property(x => x.ResetCodeHash).HasColumnName("maDatLaiMatKhau").HasMaxLength(512).IsUnicode(false);
        b.Property(x => x.ResetCodeExpiresAt).HasColumnName("hanMaDatLai");
        b.Property(x => x.RoleId).HasColumnName("idVaiTro");
        b.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId);
    }
}

internal sealed class DriverLicenseConfiguration : IEntityTypeConfiguration<DriverLicense>
{
    public void Configure(EntityTypeBuilder<DriverLicense> b)
    {
        b.ToTable("giayPhepLaiXe", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("idGPLX");
        b.Property(x => x.UserId).HasColumnName("idNguoiDung");
        b.Property(x => x.LicenseNumber).HasColumnName("soGPLX").HasMaxLength(30).IsUnicode(false);
        b.Property(x => x.LicenseClass).HasColumnName("hang").HasMaxLength(10).IsUnicode(false);
        b.Property(x => x.ExpiryDate).HasColumnName("ngayHetHan");
        b.HasOne(x => x.User).WithOne(u => u.DriverLicense).HasForeignKey<DriverLicense>(x => x.UserId);
    }
}

internal sealed class VehicleTypeConfiguration : IEntityTypeConfiguration<VehicleType>
{
    public void Configure(EntityTypeBuilder<VehicleType> b)
    {
        b.ToTable("loaiXe", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("idLoaiXe");
        b.Property(x => x.Name).HasColumnName("tenLoai").HasMaxLength(50);
        b.Property(x => x.Description).HasColumnName("moTa");
        b.Property(x => x.MinLicenseClass).HasColumnName("hangGPLXToiThieu").HasMaxLength(5).IsUnicode(false);
    }
}

internal sealed class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> b)
    {
        b.ToTable("xe", "dbo", t => t.HasTrigger("trg_xe_ChuyenTrangThai"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("idXe");
        b.Property(x => x.Name).HasColumnName("tenXe").HasMaxLength(100);
        b.Property(x => x.PlateNumber).HasColumnName("bienSo").HasMaxLength(20).IsUnicode(false);
        b.Property(x => x.Brand).HasColumnName("hangXe").HasMaxLength(50);
        b.Property(x => x.Model).HasColumnName("phienBan").HasMaxLength(100);
        b.Property(x => x.ModelYear).HasColumnName("namSanXuat");
        b.Property(x => x.Color).HasColumnName("mauSac").HasMaxLength(30);
        b.Property(x => x.Seats).HasColumnName("soCho").HasConversion(TinyIntConverters.Required);
        b.Property(x => x.Transmission).HasColumnName("hopSo").HasMaxLength(20);
        b.Property(x => x.FuelType).HasColumnName("nhienLieu").HasMaxLength(20);
        b.Property(x => x.DailyRate).HasColumnName("giaThueNgay").HasPrecision(18, 2);
        b.Property(x => x.WeeklyRate).HasColumnName("giaThueTuan").HasPrecision(18, 2);
        b.Property(x => x.DepositAmount).HasColumnName("tienDatCoc").HasPrecision(18, 2);
        b.Property(x => x.Status).HasColumnName("trangThai");
        b.Property(x => x.Description).HasColumnName("moTa");
        b.Property(x => x.PrimaryImagePath).HasColumnName("hinhAnhDaiDien").HasMaxLength(255).IsUnicode(false);
        b.Property(x => x.VehicleTypeId).HasColumnName("idLoaiXe");
        b.HasOne(x => x.VehicleType).WithMany().HasForeignKey(x => x.VehicleTypeId);
        b.HasMany(x => x.Images).WithOne(i => i.Vehicle).HasForeignKey(i => i.VehicleId);
    }
}

internal sealed class VehicleImageConfiguration : IEntityTypeConfiguration<VehicleImage>
{
    public void Configure(EntityTypeBuilder<VehicleImage> b)
    {
        b.ToTable("hinhAnhXe", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("idHinhAnhXe");
        b.Property(x => x.Path).HasColumnName("duongDanAnh").HasMaxLength(255).IsUnicode(false);
        b.Property(x => x.VehicleId).HasColumnName("idXe");
    }
}

internal sealed class PickupLocationConfiguration : IEntityTypeConfiguration<PickupLocation>
{
    public void Configure(EntityTypeBuilder<PickupLocation> b)
    {
        b.ToTable("diemGiaoNhan", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("idDiem");
        b.Property(x => x.Name).HasColumnName("tenDiem").HasMaxLength(150);
        b.Property(x => x.Address).HasColumnName("diaChi").HasMaxLength(255);
        b.Property(x => x.IsActive).HasColumnName("trangThai");
    }
}

internal sealed class PriceRuleConfiguration : IEntityTypeConfiguration<PriceRule>
{
    public void Configure(EntityTypeBuilder<PriceRule> b)
    {
        b.ToTable("bangGia", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("idBangGia");
        b.Property(x => x.VehicleTypeId).HasColumnName("idLoaiXe");
        b.Property(x => x.StartAt).HasColumnName("ngayBatDau");
        b.Property(x => x.EndAt).HasColumnName("ngayKetThuc");
        b.Property(x => x.Factor).HasColumnName("heSo").HasPrecision(5, 2);
        b.Property(x => x.IsActive).HasColumnName("trangThai");
    }
}

internal sealed class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> b)
    {
        b.ToTable("uuDai", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("idUuDai");
        b.Property(x => x.Code).HasColumnName("maUuDai").HasMaxLength(30).IsUnicode(false);
        b.Property(x => x.Name).HasColumnName("tenUuDai").HasMaxLength(150);
        b.Property(x => x.Description).HasColumnName("moTa");
        b.Property(x => x.DiscountPercent).HasColumnName("phanTramGiam").HasPrecision(5, 2);
        b.Property(x => x.StartAt).HasColumnName("ngayBatDau");
        b.Property(x => x.EndAt).HasColumnName("ngayKetThuc");
        b.Property(x => x.IsActive).HasColumnName("trangThai");
        b.HasMany(x => x.Vehicles).WithOne().HasForeignKey(x => x.PromotionId);
    }
}

internal sealed class PromotionVehicleConfiguration : IEntityTypeConfiguration<PromotionVehicle>
{
    public void Configure(EntityTypeBuilder<PromotionVehicle> b)
    {
        b.ToTable("uuDaiXe", "dbo");
        b.HasKey(x => new { x.PromotionId, x.VehicleId });
        b.Property(x => x.PromotionId).HasColumnName("idUuDai");
        b.Property(x => x.VehicleId).HasColumnName("idXe");
    }
}

internal sealed class SurchargePolicyConfiguration : IEntityTypeConfiguration<SurchargePolicy>
{
    public void Configure(EntityTypeBuilder<SurchargePolicy> b)
    {
        b.ToTable("chinhSachPhuPhi", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("idChinhSach");
        b.Property(x => x.VehicleTypeId).HasColumnName("idLoaiXe");
        b.Property(x => x.GraceMinutes).HasColumnName("anHanPhut");
        b.Property(x => x.DailyKmLimit).HasColumnName("hanMucKmNgay");
        b.Property(x => x.PricePerExtraKm).HasColumnName("donGiaKm").HasPrecision(18, 2);
    }
}

internal sealed class RentalRequestConfiguration : IEntityTypeConfiguration<RentalRequest>
{
    public void Configure(EntityTypeBuilder<RentalRequest> b)
    {
        b.ToTable("yeuCauThue", "dbo", t =>
        {
            t.HasTrigger("trg_yeuCauThue_KhongChongLich");
            t.HasTrigger("trg_yeuCauThue_ChuyenTrangThai");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("idYeuCau");
        b.Property(x => x.CustomerId).HasColumnName("idNguoiDung");
        b.Property(x => x.CreatedById).HasColumnName("idNguoiTao");
        b.Property(x => x.StartAt).HasColumnName("ngayNhan");
        b.Property(x => x.EndAt).HasColumnName("ngayTra");
        b.Property(x => x.PickupLocationId).HasColumnName("idDiemNhan");
        b.Property(x => x.ReturnLocationId).HasColumnName("idDiemTra");
        b.Property(x => x.Status).HasColumnName("trangThai");
        b.Property(x => x.CreatedAt).HasColumnName("ngayTao");
        b.Property(x => x.HoldUntil).HasColumnName("hanGiuCho");
        b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId);
        b.HasOne(x => x.PickupLocation).WithMany().HasForeignKey(x => x.PickupLocationId);
        b.HasOne(x => x.ReturnLocation).WithMany().HasForeignKey(x => x.ReturnLocationId);
    }
}

internal sealed class RentalRequestLineConfiguration : IEntityTypeConfiguration<RentalRequestLine>
{
    public void Configure(EntityTypeBuilder<RentalRequestLine> b)
    {
        b.ToTable("chiTietYeuCauThue", "dbo", t => t.HasTrigger("trg_chiTietYeuCauThue_KhongChongLich"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("idChiTietYeuCau");
        b.Property(x => x.RequestId).HasColumnName("idYeuCau");
        b.Property(x => x.VehicleId).HasColumnName("idXe");
        b.Property(x => x.WithDriver).HasColumnName("coTaiXe");
        b.Property(x => x.DriverDailyFee).HasColumnName("phiTaiXeNgay").HasPrecision(18,2);
        b.HasIndex(x => new { x.RequestId, x.VehicleId }).IsUnique();
        b.HasOne(x => x.Request).WithMany(x => x.Lines).HasForeignKey(x => x.RequestId);
        b.HasOne(x => x.Vehicle).WithMany().HasForeignKey(x => x.VehicleId);
    }
}

internal sealed class RentalContractConfiguration : IEntityTypeConfiguration<RentalContract>
{
    public void Configure(EntityTypeBuilder<RentalContract> b)
    {
        b.ToTable("hopDong", "dbo", t =>
        {
            t.HasTrigger("trg_hopDong_KhongChongLichKhiDoiNgay");
            t.HasTrigger("trg_hopDong_ChuyenTrangThai");
        });
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("idHopDong");
        b.Property(x => x.Code).HasColumnName("maHopDong").HasMaxLength(50).IsUnicode(false);
        b.Property(x => x.CreatedAt).HasColumnName("ngayTao");
        b.Property(x => x.StartAt).HasColumnName("ngayBatDauThue");
        b.Property(x => x.EndAt).HasColumnName("ngayKetThucThue");
        b.Property(x => x.GrossAmount).HasColumnName("tongTienGoc").HasPrecision(18, 2);
        b.Property(x => x.PromotionId).HasColumnName("idUuDai");
        b.Property(x => x.PromotionName).HasColumnName("tenUuDaiApDung").HasMaxLength(150);
        b.Property(x => x.PromotionPercent).HasColumnName("phanTramApDung").HasPrecision(5, 2);
        b.Property(x => x.DiscountAmount).HasColumnName("tienGiamGia").HasPrecision(18, 2);
        b.Property(x => x.DepositAmount).HasColumnName("tienDatCoc").HasPrecision(18, 2);
        b.Property(x => x.Status).HasColumnName("trangThai");
        b.Property(x => x.CancelReason).HasColumnName("lyDoHuy").HasMaxLength(255);
        b.Property(x => x.HoldUntil).HasColumnName("hanGiuCho");
        b.Property(x => x.CustomerId).HasColumnName("idNguoiDung");
        b.Property(x => x.CreatedById).HasColumnName("idNguoiTao");
        b.Property(x => x.RentalRequestId).HasColumnName("idYeuCau");
        b.Property(x => x.PickupLocationId).HasColumnName("idDiemNhan");
        b.Property(x => x.ReturnLocationId).HasColumnName("idDiemTra");
        b.Ignore(x => x.TotalPayable);
        b.Ignore(x => x.VatAmount);
        b.Property(x => x.DriverAmount).HasColumnName("phiTaiXe").HasPrecision(18,2);
        b.Property(x => x.VatRate).HasColumnName("thueSuatVat").HasPrecision(5,4);
        b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId);
        b.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById);
        b.HasOne(x => x.PickupLocation).WithMany().HasForeignKey(x => x.PickupLocationId);
        b.HasOne(x => x.ReturnLocation).WithMany().HasForeignKey(x => x.ReturnLocationId);
    }
}

internal sealed class ContractLineConfiguration : IEntityTypeConfiguration<ContractLine>
{
    public void Configure(EntityTypeBuilder<ContractLine> b)
    {
        b.ToTable("chiTietHopDong", "dbo", t => t.HasTrigger("trg_chiTietHopDong_KhongChongLich"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("idChiTietHopDong");
        b.Property(x => x.ContractId).HasColumnName("idHopDong");
        b.Property(x => x.VehicleId).HasColumnName("idXe");
        b.Property(x => x.DailyRateAtBooking).HasColumnName("giaThueNgayTaiThoiDiem").HasPrecision(18, 2);
        b.Property(x => x.WeeklyRateAtBooking).HasColumnName("giaThueTuanTaiThoiDiem").HasPrecision(18, 2);
        b.Property(x => x.DayCount).HasColumnName("soNgayThue");
        b.Property(x => x.WeekCount).HasColumnName("soTuanThue");
        b.Ignore(x => x.GrossLine);
        b.HasOne(x => x.Contract).WithMany(c => c.Lines).HasForeignKey(x => x.ContractId);
        b.HasIndex(x => new { x.ContractId, x.VehicleId }).IsUnique();
        b.Property(x => x.WithDriver).HasColumnName("coTaiXe");
        b.Property(x => x.DriverDailyFee).HasColumnName("phiTaiXeNgay").HasPrecision(18,2);
        b.HasOne(x => x.Vehicle).WithMany().HasForeignKey(x => x.VehicleId);
    }
}

internal sealed class ContractHistoryConfiguration : IEntityTypeConfiguration<ContractHistory>
{
    public void Configure(EntityTypeBuilder<ContractHistory> b)
    {
        b.ToTable("lichSuHopDong", "dbo", t => t.HasTrigger("trg_lichSuHopDong_BatBien"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("idLichSu");
        b.Property(x => x.ContractId).HasColumnName("idHopDong");
        b.Property(x => x.FromStatus).HasColumnName("trangThaiCu");
        b.Property(x => x.ToStatus).HasColumnName("trangThaiMoi");
        b.Property(x => x.ChangedAt).HasColumnName("thoiDiem");
        b.Property(x => x.ChangedById).HasColumnName("idNguoiThucHien");
        b.Property(x => x.Note).HasColumnName("ghiChu").HasMaxLength(255);
        b.HasOne(x => x.Contract).WithMany(c => c.History).HasForeignKey(x => x.ContractId);
    }
}

internal sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        // Script RentCarDb_BanChuan.sql có trigger AFTER DELETE trên bảng thanhToan.
        // Khai báo để EF Core 8 không phát sinh OUTPUT clause không tương thích trigger.
        b.ToTable("thanhToan", "dbo", t => t.HasTrigger("trg_thanhToan_ChanXoa"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("idThanhToan");
        b.Property(x => x.Code).HasColumnName("maGiaoDich").HasMaxLength(30).IsUnicode(false);
        b.Property(x => x.ContractId).HasColumnName("idHopDong");
        b.Property(x => x.Amount).HasColumnName("soTien").HasPrecision(18, 2);
        b.Property(x => x.Type).HasColumnName("loaiThanhToan");
        b.Property(x => x.Method).HasColumnName("phuongThuc").HasMaxLength(50);
        b.Property(x => x.PaidAt).HasColumnName("ngayThanhToan");
        b.Property(x => x.StatusCode).HasColumnName("trangThai");
        b.Property(x => x.ActorId).HasColumnName("nguoiThucHien");
        b.HasOne(x => x.Contract).WithMany(c => c.Payments).HasForeignKey(x => x.ContractId);
    }
}

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> b)
    {
        b.ToTable("nhatKyKiemToan", "dbo", t => t.HasTrigger("trg_nhatKyKiemToan_BatBien"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("idNhatKy");
        b.Property(x => x.ActorId).HasColumnName("idNguoiThucHien");
        b.Property(x => x.ObjectType).HasColumnName("doiTuong").HasMaxLength(30).IsUnicode(false);
        b.Property(x => x.ObjectId).HasColumnName("idDoiTuong").HasMaxLength(50).IsUnicode(false);
        b.Property(x => x.Action).HasColumnName("hanhDong").HasMaxLength(50).IsUnicode(false);
        b.Property(x => x.OldValue).HasColumnName("giaTriCu");
        b.Property(x => x.NewValue).HasColumnName("giaTriMoi");
        b.Property(x => x.At).HasColumnName("thoiDiem");
    }
}

internal sealed class HandoverRecordConfiguration : IEntityTypeConfiguration<HandoverRecord>
{
    public void Configure(EntityTypeBuilder<HandoverRecord> b)
    {
        // Bảng bienBanBanGiao không có trigger; mỗi hợp đồng đúng một dòng (UNIQUE idHopDong).
        b.ToTable("bienBanBanGiao", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("idBienBanBanGiao");
        b.Property(x => x.ContractId).HasColumnName("idHopDong");
        b.Property(x => x.DeliveredAt).HasColumnName("ngayBanGiao");
        b.Property(x => x.DeliveryKm).HasColumnName("soKmGiao");
        b.Property(x => x.DeliveryChecksJson).HasColumnName("chiTietGiaoXe");
        b.Property(x => x.ReturnChecksJson).HasColumnName("chiTietNhanXe");
        b.Property(x => x.DeliveryFuelPercent).HasColumnName("mucNhienLieuGiao").HasConversion(TinyIntConverters.Optional);
        b.Property(x => x.DeliveryCondition).HasColumnName("tinhTrangLucGiao");
        b.Property(x => x.DeliveryAccessories).HasColumnName("phuKienGiao").HasMaxLength(255);
        b.Property(x => x.ReturnedAt).HasColumnName("ngayNhanLai");
        b.Property(x => x.ReturnKm).HasColumnName("soKmNhan");
        b.Property(x => x.ReturnFuelPercent).HasColumnName("mucNhienLieuNhan").HasConversion(TinyIntConverters.Optional);
        b.Property(x => x.ReturnCondition).HasColumnName("tinhTrangLucNhan");
        b.Property(x => x.ReturnAccessories).HasColumnName("phuKienNhan").HasMaxLength(255);
        b.Property(x => x.ExtraCharges).HasColumnName("chiPhiPhatSinh").HasPrecision(18, 2);
        b.HasOne(x => x.Contract).WithOne(c => c.Handover).HasForeignKey<HandoverRecord>(x => x.ContractId);
        b.HasMany(x => x.Images).WithOne(i => i.Handover).HasForeignKey(i => i.HandoverId);
    }
}

internal sealed class HandoverImageConfiguration : IEntityTypeConfiguration<HandoverImage>
{
    public void Configure(EntityTypeBuilder<HandoverImage> b)
    {
        b.ToTable("anhBienBan", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("idAnh");
        b.Property(x => x.HandoverId).HasColumnName("idBienBanBanGiao");
        b.Property(x => x.Path).HasColumnName("duongDanAnh").HasMaxLength(255).IsUnicode(false);
        b.Property(x => x.Kind).HasColumnName("loaiAnh");
    }
}

internal sealed class StaffInvitationConfiguration : IEntityTypeConfiguration<StaffInvitation>
{
    public void Configure(EntityTypeBuilder<StaffInvitation> b)
    {
        b.ToTable("maDangKyNhanVien", "dbo");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("idMa");
        b.Property(x => x.CodeHash).HasColumnName("maBam").HasMaxLength(64).IsUnicode(false);
        b.Property(x => x.Email).HasColumnName("email").HasMaxLength(100).IsUnicode(false);
        b.Property(x => x.FullName).HasColumnName("hoTen").HasMaxLength(100);
        b.Property(x => x.RoleId).HasColumnName("idVaiTro");
        b.Property(x => x.ExpiresAt).HasColumnName("hetHan");
        b.Property(x => x.UsedAt).HasColumnName("ngayDung");
        b.Property(x => x.IsRevoked).HasColumnName("thuHoi");
        b.Property(x => x.IssuedAt).HasColumnName("ngayCap");
        b.Property(x => x.IssuedById).HasColumnName("idNguoiCap");
        b.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId);
        b.HasOne(x => x.IssuedBy).WithMany().HasForeignKey(x => x.IssuedById);
    }
}

internal sealed class MaintenanceScheduleConfiguration : IEntityTypeConfiguration<MaintenanceSchedule>
{
    public void Configure(EntityTypeBuilder<MaintenanceSchedule> b)
    {
        b.ToTable("lichBaoTri", "dbo", t => t.HasTrigger("trg_lichBaoTri_KhongChongHopDong"));
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasColumnName("idLichBaoTri");
        b.Property(x => x.VehicleId).HasColumnName("idXe");
        b.Property(x => x.Type).HasColumnName("loai").HasMaxLength(50);
        b.Property(x => x.Description).HasColumnName("moTa").HasMaxLength(255);
        b.Property(x => x.StartAt).HasColumnName("tgBatDau").HasColumnType("datetime2(0)");
        b.Property(x => x.EndAt).HasColumnName("tgKetThuc").HasColumnType("datetime2(0)");
        b.Property(x => x.Cost).HasColumnName("chiPhi").HasPrecision(18, 2);
        b.HasOne(x => x.Vehicle).WithMany().HasForeignKey(x => x.VehicleId);
    }
}

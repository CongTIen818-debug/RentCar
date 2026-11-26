using Microsoft.EntityFrameworkCore;
using RentCar.Models.Database;

namespace RentCar.Data;

public class RentCarDbContext(DbContextOptions<RentCarDbContext> options) : DbContext(options)
{
    public DbSet<CustomerAccount> CustomerAccounts => Set<CustomerAccount>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CustomerAccount>(entity =>
        {
            entity.ToTable("nguoiDung");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id).HasColumnName("idNguoiDung");
            entity.Property(user => user.FullName).HasColumnName("hoTen").HasMaxLength(100);
            entity.Property(user => user.Email).HasColumnName("email").HasMaxLength(100).IsUnicode(false);
            entity.Property(user => user.PasswordHash).HasColumnName("matKhau").HasMaxLength(512).IsUnicode(false);
            entity.Property(user => user.Phone).HasColumnName("soDienThoai").HasMaxLength(20).IsUnicode(false);
            entity.Property(user => user.IsActive).HasColumnName("trangThai");
            entity.Property(user => user.RoleId).HasColumnName("idVaiTro");
            entity.HasOne(user => user.Role).WithMany().HasForeignKey(user => user.RoleId);
        });
        modelBuilder.Entity<AccountRole>(entity =>
        {
            entity.ToTable("vaiTro");
            entity.HasKey(role => role.Id);
            entity.Property(role => role.Id).HasColumnName("idVaiTro");
            entity.Property(role => role.Code).HasColumnName("maVaiTro").HasMaxLength(20).IsUnicode(false);
        });
    }
}

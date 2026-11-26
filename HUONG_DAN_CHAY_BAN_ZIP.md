# Chạy RentCar từ bản ZIP một cổng

Bản này chỉ chạy một ứng dụng tại **https://localhost:7246**. Giữ nguyên hai thư mục source để phân công công việc.
Không cần chạy RentCar.Web riêng và không cần mở thêm cổng 7248.

## 1. Chuẩn bị

- Windows, .NET 8 SDK và SQL Server/SQL Server Express.
- Visual Studio 2022 có workload ASP.NET and web development nếu chạy bằng Visual Studio.
- Internet để restore NuGet lần đầu; SSMS để chạy SQL.
- Giải nén toàn bộ thư mục RentCar, giữ RentCar_TrangChu và RentCar_QuanLy cạnh nhau.

## 2. Database

- Máy mới chưa có V2: chạy database/CreateRentCarDbV2.sql rồi database/SeedRentCarDbV2.sql.
- Đã có V2 của nhóm: chỉ chạy database/UpgradeQL2MultiVehicle.sql nếu chưa nâng cấp, không tạo lại/seed dữ liệu.
- Việc chuyển từ hai cổng sang một cổng không yêu cầu thêm thay đổi database.

ZIP cung cấp script và dữ liệu demo; không tự mang theo database SQL Server đang chạy trên máy gửi.

## 3. Kết nối SQL Server

Chỉ sửa **RentCar_TrangChu/appsettings.Development.json**, khóa ConnectionStrings:RentCar.
Phần quản lý và trang chủ cùng dùng kết nối này. Gói mặc định dùng SQL Server Express cục bộ:

```json
{
  "ConnectionStrings": {
    "RentCar": "Server=.\\SQLEXPRESS;Database=RentCarDb_V2;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Nếu SQL Server có tên khác, đổi Server theo máy nhận. Không còn appsettings riêng cần cấu hình ở RentCar.Web.
SMTP/user-secrets cấu hình tại project RentCar_TrangChu; gói không kèm mật khẩu SMTP hay cấu hình riêng máy gửi.

## 4. Chạy bằng Visual Studio

1. Mở RentCar.sln ở thư mục gốc.
2. Chọn **RentCar** (trong RentCar_TrangChu) làm Startup Project, profile **https**.
3. Nhấn F5. Không chọn RentCar.Web và không đặt chạy nhiều startup project.

RentCar.Web là thư viện MVC/Razor được ứng dụng chính tải cùng lúc, vẫn giữ Controller, Service và View của quản lý.
Có profile `RentCar - Mot cong` trong RentCar.slnLaunch nếu Visual Studio hỗ trợ launch profile.

## 5. Chạy bằng PowerShell

Mở PowerShell trong thư mục có RentCar.sln:

```powershell
dotnet dev-certs https --trust
powershell -ExecutionPolicy Bypass -File .\chay_du_an.ps1
```

Hoặc chạy trực tiếp (giữ terminal mở):

```powershell
dotnet run --project .\RentCar_TrangChu\RentCar.csproj --launch-profile https
```

Script chỉ mở một ứng dụng; chạy lại sẽ giữ bản đang chạy, không tạo tiến trình trùng cổng.
Chỉ dùng một cách khởi động tại một thời điểm, tránh F5 thêm khi script đã mở cùng ứng dụng.

## 6. Các đường dẫn

- Trang bán hàng: https://localhost:7246/
- Đăng nhập/đăng ký: https://localhost:7246/Account/Login và /Account/Register
- Quản lý: https://localhost:7246/QuanLy
- Dashboard: https://localhost:7246/QuanLy/Dashboard
- Chuyến của khách: https://localhost:7246/QuanLy/Rentals/Mine

Đăng nhập khách về trang chủ hoặc tiếp tục đặt thuê; admin/nhân viên vào quản lý theo vai trò.
Admin mở được trang chủ và quản lý mà không đăng xuất. Một trình duyệt vẫn dùng chung một tài khoản;
để thử khách/admin đồng thời, dùng cửa sổ thường và ẩn danh. Giữ cơ chế tạm khóa sau nhập sai của thành viên.

Tài khoản của bộ seed trong gói:

- Admin: giahuy.tran@gmail.com / 123456
- Khách: minhquan.vu@gmail.com / 123456

Nếu dùng database sẵn có, tài khoản phụ thuộc dữ liệu của database đó.

## 7. Kiểm thử và phạm vi

```powershell
dotnet test .\RentCar_QuanLy\tests\RentCar.Tests\RentCar.Tests.csproj
```

Hợp đồng nhiều xe, biên bản chung, tài xế từng xe, VAT và UTC+7 giữ như bản kết hợp trước.
Checkout trang bán hàng vẫn còn TODO lưu đơn/chi tiết/thanh toán. Chưa có thanh toán trực tuyến hoàn chỉnh,
React, Web API hoặc Swagger. Tài liệu thay đổi ở thư mục gốc ghi rõ các nội dung đã ghép.

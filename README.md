# RentCar

Ứng dụng quản lý và tra cứu xe thuê, dùng ASP.NET Core MVC, .NET 8 và SQL Server.

## Chạy trên máy phát triển

1. Cài .NET 8 SDK và có một SQL Server để phát triển.
2. Mở SSMS và chạy Data/CreateSampleDatabase.sql trên SQL Server để tạo RentCarDB1 với dữ liệu giả lập. Chạy script trên cơ sở dữ liệu mới.
3. Trong RentCarDB1, chạy Data/AddBrandCatalog.sql để tạo danh mục hãng.
4. Tại thư mục chứa RentCar.csproj, đặt chuỗi kết nối trong PowerShell. Thay .\SQLEXPRESS bằng tên máy chủ SQL Server của bạn.

   ```powershell
   $env:ConnectionStrings__RentCar = 'Server=.\SQLEXPRESS;Database=RentCarDB1;Trusted_Connection=True;TrustServerCertificate=True;'
   dotnet run --launch-profile http
   ```

5. Mở địa chỉ localhost mà ứng dụng in ra. Trang mặc định là khu quản lý. Nút Mở trang RentCar mở giao diện khách trong tab mới.

Khu quản lý hiện giới hạn truy cập từ máy chạy ứng dụng. Dữ liệu mẫu không chứa mật khẩu đăng nhập.

# RentCar — trang bán hàng và quản lý trong một ứng dụng

Đọc [HUONG_DAN_CHAY_BAN_ZIP.md](HUONG_DAN_CHAY_BAN_ZIP.md) để cấu hình SQL Server và chạy trên máy nhận.

- Một ứng dụng khởi động: RentCar_TrangChu/RentCar.csproj, profile https.
- Một cổng: https://localhost:7246. Đăng nhập ở /Account/Login, quản lý ở /QuanLy.
- Giữ RentCar_TrangChu riêng cho giao diện khách và RentCar_QuanLy riêng cho bốn project quản lý.
- RentCar.Web là thư viện MVC/Razor; không chạy riêng, không mở thêm ứng dụng ở 7248.
- Chỉ cấu hình ConnectionStrings:RentCar tại RentCar_TrangChu/appsettings.Development.json.
- Chạy chay_du_an.ps1 hoặc chọn RentCar làm Startup Project trong Visual Studio.
- Database tiếp tục là RentCarDb_V2; việc ghép một cổng không tạo lại hoặc thay cấu trúc database.
- Giữ nghiệp vụ nhiều xe và đăng nhập của thành viên; 29 unit test hiện có.

Chi tiết thay đổi: [Kết hợp trang bán hàng và một số thay đổi RentCar (trang quản lý)](<Kết hợp trang bán hàng và 1 số thay đổi RentCar(trang quản lý).md>).

## Lưu ý khi phát triển phần quản lý

Controller nằm trong RentCar_QuanLy/src/RentCar.Web/Controllers.
View của quản lý nằm trong RentCar_QuanLy/src/RentCar.Web/Areas/QuanLy/Views để tách khỏi View trang chủ.
ManagementModule đăng ký các service và Controller vào host chính; ManagementViewLocationExpander chọn đúng bộ View/Layout.
CSS/JS của quản lý được phục vụ ở /_content/RentCar.Web/, tránh trùng CSS/JS trang chủ.
Các Controller quản lý có đường dẫn /QuanLy/{controller}/{action}; Account dùng đường dẫn /Account/{action}.
Nếu thêm Controller quản lý mới, cập nhật danh sách Controller trong route management của Program.cs ứng dụng chính.

## Phần còn thiếu

Checkout của trang bán hàng vẫn còn TODO lưu đơn/chi tiết/thanh toán. Chưa bổ sung cổng thanh toán trực tuyến hoàn chỉnh,
React, Web API hay Swagger. Không coi việc ghép một cổng là hoàn thành các phần nghiệp vụ còn thiếu.

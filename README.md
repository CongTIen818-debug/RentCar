# RentCar

Trang khách thuê xe sang dùng ASP.NET Core MVC, .NET 8 và SQL Server.

## Chạy ứng dụng

1. Cài .NET 8 SDK.
2. Dùng cấu trúc RentCarDb mới đã thống nhất với nhóm. Các bảng dùng cho trang khách gồm xe, loaiXe, hinhAnhXe, khuyenMai, khuyenMaiXe, hopDong, chiTietHopDong, nguoiDung và vaiTro.
3. Tạo appsettings.Development.json ở cùng thư mục với RentCar.csproj, dùng cấu hình sau và thay tên SQL Server của bạn.

   {
     "ConnectionStrings": {
       "RentCar": "Server=.\SQLEXPRESS;Database=RentCarDb;Trusted_Connection=True;TrustServerCertificate=True;"
     }
   }

4. Mở RentCar.sln trong Visual Studio, chọn http rồi chạy. Có thể chạy bằng dotnet run --launch-profile http.

File cấu hình kết nối cục bộ được .gitignore bỏ qua.

Mỗi thẻ đại diện một chiếc xe. xe.trangThai = 1 là sẵn sàng. Giá khuyến mãi áp dụng theo thời điểm đặt, lưu thời gian theo UTC. Ảnh đại diện đọc từ xe.hinhAnhDaiDien, sau đó lấy ảnh phụ có đường dẫn hợp lệ nếu cần.

## Lịch thuê

Hợp đồng dùng mã 0 = Đã đặt cọc, 1 = Đã nhận xe, 2 = Hoàn thành, 3 = Đã hủy. Ngày nhận phải sau ngày hiện tại theo giờ Việt Nam, ngày giờ trả phải sau ngày giờ nhận. Database lưu thời gian UTC. Xe đã nhận quá hạn trả tiếp tục bị loại khỏi kết quả đến khi hoàn tất nhận lại.
## Đăng nhập

Trang đăng nhập chỉ nhận vai trò KHACHHANG trong vaiTro.maVaiTro. Tài khoản cần có trangThai = 1 và mật khẩu được băm bằng PasswordHasher của ASP.NET Core Identity. Các hồ sơ có matKhau = UNSET cần được cấp mật khẩu trước khi sử dụng.

Schema mới chưa có trường lưu số lần đăng nhập sai và thời hạn khóa tạm. Phần khóa sau nhiều lần sai cần được nhóm thống nhất cùng cấu trúc tài khoản.

## Tích hợp Checkout của thành viên

Bản này đã ghép Checkout từ thư mục kiểm tra test-Thanh-toan, đồng bộ với model hiện tại:
- RentalReviewViewModel.Lines: mỗi dòng chứa Car, CanHaveDriver, WithDriver.
- RentalReviewViewModel.Price: tiền xe, giảm giá, tài xế, VAT và số phải thanh toán.
- Nút Đặt thuê tại chi tiết xe và danh sách thuê đi tới Rental/Checkout.
- Checkout GET nhận nhiều xe bằng tham số carIds lặp lại; Checkout POST dùng chuỗi ID phân cách dấu phẩy theo code của thành viên.

Đã kiểm tra biên dịch và luồng mở Checkout. Logic thanh toán của thành viên được giữ nguyên.
Checkout POST còn TODO lưu hợp đồng, chi tiết hợp đồng và thanh toán; hiện vẫn báo thành công
và bỏ xe khỏi session mà chưa lưu các bản ghi đó. Chưa dùng kết quả này làm xác nhận thanh toán.
Cách tính tiền trong Checkout cũng cần thành viên đối chiếu với RentalPricing trước khi hoàn thiện.

Mỗi máy tự cấu hình ConnectionStrings:RentCar trong appsettings.Development.json
(file này được bỏ qua bởi Git). Không cần đổi schema database chỉ để đồng bộ model Lines/Price.

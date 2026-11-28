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
React cho phần thống kê quản lý. API và Swagger trang khách đã được bổ sung trong mục tuần 6 bên dưới. Không coi việc ghép một cổng là hoàn thành các phần nghiệp vụ còn thiếu.


## Phần trang khách đến tuần 6

- Giữ bố cục trắng/cam #FF5E00, lịch nhỏ nhập tay với giờ 24 tiếng, sidebar lọc thu gọn, một xe riêng biệt trên mỗi thẻ, một điểm nhận/trả, hợp đồng nhiều xe và tài xế từng xe.
- Tìm theo tên/hãng, loại, số chỗ, hộp số, nhiên liệu, khoảng giá; sắp xếp và phân trang từ SQL. Các trang dịch vụ cũng phân trang và giữ thứ tự đã thống nhất.
- Giá dự kiến: giá ngày/tuần theo bảng giá tại thời điểm nhận, một ưu đãi có lợi nhất hoặc giảm nhiều xe 6%/10%, không giảm phí tài xế, giá đã gồm thuế. Hợp đồng cũ vẫn dùng số đã chốt.
- Hồ sơ/GPLX: `/tai-khoan/ho-so`; đổi mật khẩu: `/tai-khoan/doi-mat-khau`; khôi phục qua liên kết: `/tai-khoan/quen-mat-khau`; quyền riêng tư: `/tai-khoan/quyen-rieng-tu`; thông báo: `/thong-bao`.
- API khách và Swagger: `/swagger` chỉ trong Development; lấy mã ở `/api/csrf` rồi gửi header `X-CSRF-TOKEN` cho POST. Phiên API dùng cùng cookie với MVC và trả 401/403 thay vì chuyển trang.
- Cấu hình SMTP tiếp tục dùng `Smtp` của thành viên. `CustomerAccount:PublicBaseUrl` phải là URL HTTPS triển khai. Không đưa mật khẩu SMTP vào Git; dùng user-secrets hoặc biến môi trường. Chưa cấu hình SMTP thì không gửi email thật. `CustomerAccount:ShowResetPreview` mặc định false, chỉ bật trong Development trên database thử để lấy liên kết mô phỏng.
- Đăng nhập của thành viên được giữ nguyên; luồng khôi phục mới có lối vào trên header trang khách. Không thay các file `RentCar_QuanLy`. Thao tác ẩn danh phải do khách xác nhận bằng mật khẩu, bị chặn nếu còn yêu cầu/hợp đồng đang xử lý, giữ nguyên giao dịch tài chính.
- `/health` chỉ trả trạng thái kết nối. Phiên hết hạn sau 30 phút không hoạt động; có CSRF, giới hạn thao tác, kiểm tra chủ sở hữu, che giấy tờ và vô hiệu phiên khách cũ sau đổi mật khẩu.

Kiểm tra: `dotnet test RentCar.sln`. Bộ HTTP `tests/week6_checks.py` chỉ dành cho host kiểm thử ở localhost:51917 và database `RentCar_Week6_Verify_20261128`; cần tạo database thử từ CreateRentCarDbV2.sql, SeedRentCarDbV2.sql và AddHanoiPickupLocations.sql bằng cách thay tên database trong bản sao. Không chạy bộ HTTP trên host đang dùng database thật. File `tests/Week6PerformanceFixture.sql` chỉ tạo tải trong database thử; chỉ chạy một lần trên fixture mới. Kết quả đo nằm trong `tests/week6-performance-results.json`.

### Phần còn cần tích hợp hoặc nghiệm thu

- Thanh toán và tạo hợp đồng sau cọc thuộc phần thành viên; chưa nối cổng thanh toán. Đường Checkout cũ có TODO được chặn để tránh thông báo thành công giả.
- Không thêm tác vụ tự đổi trạng thái yêu cầu hoặc tự gửi email hợp đồng. Yêu cầu chờ quá hạn được ghi rõ trên giao diện; việc tự chuyển trạng thái 1 sang 3 đang chờ phê duyệt riêng. Yêu cầu đã duyệt vẫn được giữ nguyên. Thông báo đọc từ lịch sử, lịch trả và bảng thông báo hiện có.
- Chưa chỉnh luồng quản lý về thời điểm lập/xác nhận hợp đồng theo thỏa thuận; code quản lý được giữ nguyên.
- Sao lưu hằng ngày/giữ 7 bản, RTO 4 giờ/RPO 24 giờ, độ sẵn sàng 99% và dọn dữ liệu cá nhân sau tối đa 5 năm là phần vận hành máy chủ; chưa tự cài lịch sao lưu hoặc xóa dữ liệu.
- Chưa có thử nghiệm với 5 người dùng thật để nghiệm thu NFR-12. Phép đo hiệu năng hiện tại dùng dữ liệu 1.000 xe, 20.000 hợp đồng và 50 yêu cầu đồng thời trên máy phát triển, không khẳng định tương đương phần cứng chuẩn SRS.


## Cập nhật từ quanly(moi nhat) và đổi cách lưu giờ — 28/11/2026

- Chỉ ghép sửa ô ngày giờ không bị cắt phút, CSS các ô chọn, 19 tệp ảnh upload và hai bộ kiểm thử mới. Bộ BoundaryTests được đổi phần dựng dữ liệu từ Line sang Lines để tương thích hợp đồng nhiều xe. Không chép lại Program, đăng nhập, model hoặc service một xe của thành viên.
- Giữ một ứng dụng/cổng 7246, cookie và điều hướng đăng nhập hiện tại, yêu cầu/hợp đồng nhiều xe, tài xế từng xe, giao nhận đủ xe, trạng thái và cách tính tiền hiện tại. Không chạy SQL 23 bảng của thành viên và không thay danh sách xe bằng dữ liệu của họ.
- **Quy ước mới nhất: các cột datetime/datetime2 trong database lưu trực tiếp giờ Việt Nam (UTC+7).** EF không còn trừ/cộng 7 giờ khi lưu/đọc. Thời điểm nhận 09:00 được lưu 09:00. Các trường DATE như ngày sinh/hạn GPLX giữ nguyên.
- `database/UseVietnamLocalTime.sql` chuyển database V2 cũ đang lưu UTC sang giờ Việt Nam trong một giao dịch, đồng thời sửa mặc định, view, trigger và thủ tục so sánh thời gian. Phải sao lưu và dừng ứng dụng trước; chạy bằng `sqlcmd -d <database> -b -I -f 65001 -i database/UseVietnamLocalTime.sql`. Script không tạo lại bảng, không đổi trạng thái/tiền, có dấu RentCar.TimeStorage để chống cộng giờ lần hai. Không chạy script này trên database đã lưu giờ Việt Nam nhưng không có dấu xác nhận nếu chưa đối chiếu nguồn dữ liệu.
- Máy này đã được sao lưu COPY_ONLY có CHECKSUM và chuyển RentCarDb_V2. Thành viên dùng database V2 UTC cũ cần chạy script trên bản của họ; không chỉ kéo code rồi chạy với dữ liệu cũ. Cài mới dùng CreateRentCarDbV2.sql và SeedRentCarDbV2.sql đã cập nhật giờ Việt Nam. Các script BanChuan/v2_1/MigrateRentCarDbToV2 cũ là tài liệu lịch sử, không chạy lại để cập nhật bản hiện tại.
- Một số DTO của tab yêu cầu/thông báo vẫn có hậu tố Utc: đây là giá trị đã chuyển tại biên trả dữ liệu để trình duyệt đếm ngược chính xác ở mọi múi giờ và giữ dấu đã đọc cũ, không phải cách lưu database. Không cộng giờ thêm trong trang hợp đồng.
- Kiểm tra sau ghép: 121 ca unit test; 10 nhóm kiểm tra HTTP; luồng tích hợp hợp đồng 10 xe, chống trùng xe cuối, tài xế, giao/nhận đủ xe, quyết toán và hết hạn cọc trên database thử. Đối chiếu 24 bảng sau chuyển giờ: chỉ datetime tăng 7 giờ, dữ liệu khác giữ nguyên.

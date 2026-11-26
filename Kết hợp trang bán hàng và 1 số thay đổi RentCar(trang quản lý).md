# Kết hợp trang bán hàng và một số thay đổi RentCar (trang quản lý)

Cập nhật ngày 26/11/2026.

Tài liệu này ghi những thay đổi của bản RentCar kết hợp so với bản **RentCar (trang quản lý)** do thành viên gửi, để cả nhóm tiếp tục làm việc trên cùng mã nguồn. Tên “RentCar (trang quản lý)” được dùng xuyên suốt tài liệu để gọi bản gốc của thành viên. Tên thư mục và script kỹ thuật đang được code tham chiếu vẫn được giữ nguyên.

Bản kết hợp giữ các tính năng mới của thành viên, đồng thời điều chỉnh để dùng chung với trang bán hàng, hỗ trợ hợp đồng nhiều xe và thống nhất thời gian Việt Nam. Luồng Checkout của trang bán hàng vẫn còn phần lưu đơn/thanh toán chưa hoàn thiện; không coi việc ghép code là đã hoàn thành thanh toán đầu-cuối.

## 1. Cấu trúc thư mục chung

```text
RentCar/
├── RentCar.sln
├── RentCar.slnLaunch
├── chay_du_an.ps1
├── RentCar_TrangChu/
│   ├── Controllers/
│   ├── Models/
│   ├── Views/
│   ├── Services/
│   ├── Data/
│   ├── Properties/
│   ├── wwwroot/
│   ├── Program.cs
│   └── RentCar.csproj
├── RentCar_QuanLy/
│   ├── src/
│   │   ├── RentCar.Web/
│   │   ├── RentCar.Application/
│   │   ├── RentCar.Domain/
│   │   └── RentCar.Infrastructure/
│   └── tests/
│       ├── RentCar.Tests/
│       └── RentCar.IntegrationChecks/
└── database/
```

- Trang bán hàng giữ cách tổ chức MVC của phần trang chủ.
- Trang quản lý giữ kiến trúc bốn project của thành viên. Mỗi project vẫn đảm nhận nhiệm vụ cũ.
- Mở `RentCar.sln` ở thư mục gốc. Thành viên làm quản lý sửa code trong `RentCar_QuanLy`.
- Hai ứng dụng dùng chung `RentCarDb_V2`; SQL được đặt trong `database`.

## 2. Những tính năng của bản quản lý được giữ lại

| Tính năng | Nội dung giữ lại |
|---|---|
| Tổng quan | Chỉ số hợp đồng, doanh thu và doanh thu thuần; so sánh kỳ 30 ngày và biểu đồ 14 ngày. |
| Lịch bảo trì | Xem, thêm, sửa, xóa lịch; kiểm tra chồng lịch hợp đồng. |
| Bản in | In hợp đồng, biên nhận cọc và biên bản giao/nhận xe. |
| Hết hạn giữ chỗ | Tác vụ nền gọi thủ tục hủy hợp đồng chờ xác nhận hết hạn, mặc định mỗi 5 phút. |
| Ghi nhận cọc | Kiểm tra hạn giữ chỗ, dùng transaction và khóa dòng hợp đồng. |
| Tài khoản | Quản lý nhân viên, cấp mã đăng ký, đổi/quên mật khẩu và tạm khóa đăng nhập. |
| Giao diện | Giao diện kính mờ, icon, bố cục responsive và các tiện ích JavaScript của thành viên. |

`AuthService`, các service quản lý tài khoản, `DashboardService` và `HoldExpirationWorker` giữ nguyên code so với bản thành viên. Các service hợp đồng/giao nhận được sửa để xử lý nhiều xe và khớp database V2.

Dashboard hiện dùng Razor và SVG; bản kết hợp chưa bổ sung React, Web API hoặc Swagger.

## 3. Yêu cầu thuê và hợp đồng nhiều xe

### So với bản quản lý gốc

| Nội dung | Bản quản lý gốc | Bản kết hợp |
|---|---|---|
| Xe trong yêu cầu | `RentalRequest.VehicleId` và `Vehicle` | `RentalRequest.Lines`, mỗi phần tử là `RentalRequestLine`. |
| Xe trong hợp đồng | Một `RentalContract.Line` | Danh sách `RentalContract.Lines`. |
| Quan hệ hợp đồng–chi tiết | Một–một | Một–nhiều. |
| Kiểm tra điều kiện | Theo một xe | Kiểm tra tất cả xe trong hợp đồng. |
| Tài xế | Chưa xử lý theo từng xe | `WithDriver` và `DriverDailyFee` riêng cho từng dòng. |

`idXe` của yêu cầu nằm trong `chiTietYeuCauThue`, không nằm trực tiếp trong `yeuCauThue`. Đây là nguyên nhân cần đổi model và ánh xạ để tránh lỗi `Invalid column name 'idXe'`.

Giữ `UNIQUE(idHopDong, idXe)`: một hợp đồng chứa nhiều xe nhưng không được lặp cùng một chiếc. Các truy vấn lịch xe, bảo trì, xác nhận hợp đồng và danh sách hiển thị đã chuyển sang kiểm tra các dòng xe.

Phần nghiệp vụ đã có phương thức tạo yêu cầu nhiều xe và chuyển đầy đủ các xe sang hợp đồng. Điều này không thay thế phần nối Checkout còn thiếu của trang bán hàng.

## 4. Tính tiền và lưu giá đã chốt

Thứ tự tính tiền của hợp đồng mới:

1. Cộng tiền thuê của các xe theo giá đã chốt.
2. Giảm 6% tiền xe khi thuê 6–9 xe; giảm 10% từ 10 xe.
3. Cộng phí tài xế riêng: 1.000.000 đồng/xe/ngày đối với xe được chọn kèm tài xế. Chỉ xe trên 2 chỗ hỗ trợ lựa chọn này.
4. Tính VAT 8% trên tiền sau giảm giá và cộng phí tài xế.
5. Tính tổng tiền phải thu; khoản cọc được lưu và ghi nhận riêng theo nghiệp vụ quản lý.

Nếu nhập mã ưu đãi, cách xử lý hiện tại là lấy phần trăm cao hơn giữa mã và ưu đãi số lượng xe, không cộng dồn hai mức giảm.

Giá từng xe và phí tài xế/ngày được lưu ở chi tiết hợp đồng. Tổng phí tài xế và thuế suất VAT được lưu trong hợp đồng để việc đổi giá sau này không làm thay đổi giá đã chốt.

Hợp đồng cũ được giữ phí tài xế và VAT bằng 0 trong lần nâng cấp để không thay đổi tổng tiền đã có.

## 5. Một biên bản chung, kiểm tra đủ từng xe

Mỗi hợp đồng vẫn chỉ có một biên bản giao/nhận. Biểu mẫu bổ sung danh sách kiểm tra từng xe gồm:

- Kilomet và phần trăm nhiên liệu.
- Tình trạng khi giao và khi nhận lại.
- Chi phí hư hỏng và lựa chọn cần bảo trì khi nhận trả.

Máy chủ kiểm tra phải có đủ các xe trong hợp đồng, mỗi xe đúng một lần. Không thể giao/nhận 9 xe rồi xác nhận biên bản của hợp đồng 10 xe.

Chi tiết từng xe lưu dạng JSON trong `bienBanBanGiao.chiTietGiaoXe` và `chiTietNhanXe`. Các trường cũ của biên bản vẫn giữ để tương thích dữ liệu một xe trước đây.

Khi trả xe, phí phát sinh được tính theo từng xe rồi cộng lại; trạng thái từng xe được cập nhật riêng. Biên bản trả được lưu trước bước đổi trạng thái xe, trong cùng transaction, để trigger nhận biết xe đã trả. Nếu có lỗi thì giao dịch được rollback.

Các trang hợp đồng, biên nhận và bản in đã đổi sang hiển thị toàn bộ danh sách xe.

## 6. Thời gian Việt Nam UTC+7

| Lớp xử lý | Quy ước |
|---|---|
| Giao diện, biểu mẫu và nghiệp vụ | Giờ Việt Nam UTC+7. |
| Database V2 | Lưu UTC để bảo toàn dữ liệu hiện có. |
| Entity Framework | Tự chuyển UTC ↔ UTC+7 ở biên đọc/ghi dữ liệu. |
| SQL trực tiếp | Chuyển tham số giờ Việt Nam về UTC; so sánh với `SYSUTCDATETIME()`. |

`VietnamTime` có `Now`, `ToUtc` và `FromUtc`. Không cộng thêm 7 giờ tại View sau khi EF đã chuyển đổi. Không lấy giờ nghiệp vụ bằng `DateTime.Now` phụ thuộc múi giờ máy chủ.

Ví dụ: khách chọn 09:00 giờ Việt Nam thì database lưu 02:00 UTC; trang quản lý hiển thị lại 09:00.

Đồng hồ nghiệp vụ được lấy đến giây, phù hợp `datetime2(0)`, để tránh SQL làm tròn thời điểm vừa trả xe thành một thời điểm tương lai. Không dịch hàng loạt các mốc thời gian cũ trong database.

## 7. Đăng nhập chung trong một ứng dụng

Chỉ chạy RentCar_TrangChu/RentCar.csproj ở cổng 7246. RentCar.Web đã chuyển thành thư viện MVC/Razor được host chính tải,
không còn Program khởi động riêng hoặc cổng 7248. Giữ AuthService và cơ chế tạm khóa của thành viên.

- Trang chủ tại /; Account của thành viên tại /Account/Login, /Account/Register và các action liên quan.
- Khách đăng nhập về trang chủ hoặc tiếp tục đặt thuê. Admin/nhân viên vào /QuanLy theo vai trò.
- Admin đi qua lại trang chủ/quản lý không cần đăng xuất. Một trình duyệt dùng chung một phiên tài khoản.
- Controller và View quản lý vẫn ở project RentCar.Web. View được chuyển vào Areas/QuanLy/Views; bộ chọn View riêng
  giúp dùng đúng layout quản lý. CSS/JS riêng qua /_content/RentCar.Web/ để không trùng file trang chủ.

## 8. Các vị trí code cần chú ý khi làm tiếp

Các đường dẫn dưới đây tính từ `RentCar_QuanLy/src`:

| Vị trí | Nội dung thay đổi |
|---|---|
| `RentCar.Domain/Entities/Entities.cs` | Model nhiều dòng xe, snapshot phí/VAT, chi tiết biên bản. |
| `RentCar.Domain/Rules/ConfirmationRules.cs` | Kiểm tra điều kiện cho tất cả xe, có xét lựa chọn tài xế. |
| `RentCar.Domain/Common/Shared.cs` | Chuyển đổi thời gian UTC/UTC+7. |
| `RentCar.Infrastructure/Persistence/Configurations.cs` | Ánh xạ quan hệ một–nhiều và các cột mới. |
| `RentCar.Infrastructure/Persistence/AppDbContext.cs` | Chuyển đổi thời gian và tham số truy vấn lịch xe. |
| `RentCar.Application/Rentals/RentalService.cs` | Yêu cầu/hợp đồng nhiều xe, tính tiền, kiểm tra lịch và ghi nhận cọc. |
| `RentCar.Application/Handover/HandoverService.cs` | Kiểm tra đủ xe, phụ phí và trạng thái từng xe. |
| `RentCar.Web/Models/Forms.cs` và `Controllers/HandoverController.cs` | Nhận danh sách kiểm tra từng xe từ biểu mẫu. |
| `RentCar.Web/Controllers/AccountController.cs`, `ManagementModule.cs` | Nối đăng nhập và đăng ký mô-đun quản lý vào host chính. |
| `RentCar.Web/Authentication/SharedAuthentication.cs` | Cấu hình phiên đăng nhập duy nhất. |
| Các View Staff, Rentals, Handover và Shared | Danh sách xe, kiểm tra từng xe và bản in. |

Không chép đè nguyên bộ model, cấu hình EF hoặc service một xe từ bản gốc lên bản kết hợp.

## 9. Cập nhật database khi lấy code mới

### Đã có RentCarDb_V2 của nhóm

Chạy `database/UpgradeQL2MultiVehicle.sql` trước khi chạy code mới. Script đã được áp dụng trên máy người tích hợp; các thành viên khác cần áp dụng trên máy của mình.

Script bổ sung hai cột `phiTaiXe`, `thueSuatVat` ở hợp đồng; hai cột JSON kiểm tra từng xe ở biên bản; cập nhật công thức tổng tiền, ràng buộc ưu đãi và default thời gian cấp mã nhân viên. Có thể chạy lại script này. Không xóa/tạo lại database và không đổi tiền của hợp đồng cũ.

Tên script kỹ thuật được giữ nguyên để khớp repository, dù tài liệu gọi bản thành viên là RentCar (trang quản lý).

### Chưa có V2, mới chỉ có database của bản quản lý gốc

Không chạy riêng script Upgrade và cho rằng đã chuyển được cấu trúc một xe sang nhiều xe. Cần chuẩn bị database V2 tương ứng theo README.

Để tạo bộ demo mới, dùng `database/CreateRentCarDbV2.sql` rồi `database/SeedRentCarDbV2.sql`, chỉ trên database V2 mới/trống. Không dùng script xóa/tạo lại database cũ để cập nhật bản đang có dữ liệu.

Git chỉ đồng bộ file code/SQL, không tự đồng bộ database SQL Server trên máy mỗi thành viên.

## 10. Cấu hình và chạy một cổng

Chỉ cấu hình ConnectionStrings:RentCar tại RentCar_TrangChu/appsettings.Development.json.
Mở solution và chọn RentCar làm Startup Project, profile https; hoặc chạy chay_du_an.ps1 tại thư mục gốc.
Chỉ cần một tiến trình host tại https://localhost:7246; đăng nhập /Account/Login, quản lý /QuanLy.
Không chạy RentCar.Web riêng vì project này là thư viện MVC/Razor.

Xem HUONG_DAN_CHAY_BAN_ZIP.md để tạo/cập nhật database trên máy thành viên.

## 11. Kết quả kiểm tra

- Solution build thành công.
- 29 unit test đạt, gồm 24 bài cũ và 5 bài bổ sung.
- Đã thử trên database riêng: hợp đồng 10 xe, tài xế từng xe, giảm giá, VAT, cọc, xác nhận, chống trùng lịch, chặn giao thiếu xe, nhận trả, quyết toán, hết hạn cọc và bảo trì.
- Đã thử qua HTTP: đăng nhập khách/admin, dashboard, trang bảo trì, các bản in và biểu mẫu giao/nhận hai xe.
- Năm hợp đồng cũ trên database thật giữ nguyên số tiền; dữ liệu kiểm thử không được tạo trong database thật.

Integration runner nằm trong `RentCar_QuanLy/tests/RentCar.IntegrationChecks`, yêu cầu database thử riêng có tên bắt đầu `RentCar_QL2_Verify_`. Không chạy runner trên database thật.

## 12. Phần còn phải hoàn thiện

- Checkout của trang bán hàng vẫn có TODO tạo đơn/chi tiết/thanh toán và lưu database. Hiện thông báo thành công ở bước đó chưa chứng minh giao dịch đã được lưu.
- Chưa bổ sung cổng thanh toán trực tuyến hoàn chỉnh.
- Chưa bổ sung React, bộ Web API và Swagger.
- Cần kiểm thử tiếp các trường hợp đồng thời, thanh toán và dữ liệu nghiệp vụ thực tế khi các thành viên nối những phần còn thiếu.

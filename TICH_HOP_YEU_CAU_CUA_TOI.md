# Tab Yêu cầu của tôi

Trang khách: `/yeu-cau-cua-toi`. Mã nằm trong `RentCar_TrangChu`: controller `MyRequestsController`, service `CustomerRequests`, model `CustomerRequests`, views `Views/MyRequests`, giao diện `customer-requests.js` và `rentcar-layout.css`.

## Quy ước đã thống nhất

- Trạng thái yêu cầu: 1 đang chờ duyệt, 2 đã duyệt, 3 đã hủy. Khách chỉ hủy được yêu cầu của mình ở trạng thái 1; thao tác kiểm tra trạng thái ngay khi cập nhật.
- Nhân viên duyệt: đặt trạng thái 2 và `hanGiuCho` bằng thời điểm duyệt + 24 giờ (lưu UTC, hiển thị UTC+7). Không tạo hợp đồng lúc duyệt.
- Hết hạn chưa cọc: tab hiện thông báo hết 24 giờ và chặn đường dẫn thanh toán. Không đổi trạng thái 2, không xóa yêu cầu.
- Cọc thành công mới tạo hợp đồng. Yêu cầu vẫn là 2. Tab đọc giao dịch thanh toán cọc thành công để phân biệt đã cọc với chỉ có hợp đồng.
- Một yêu cầu chứa nhiều xe; mỗi xe có lựa chọn tài xế riêng trong chi tiết yêu cầu.

## Thông báo đã đọc

Chấm đỏ dựa trên yêu cầu đã duyệt chưa có dấu đã xem. JavaScript đánh dấu khi thông báo nằm trong vùng nhìn thấy và trang đang mở; cũng có nút đánh dấu thủ công. Lưu sự kiện `KHACH_XEM_DUYET` trong bảng nhật ký kiểm toán sẵn có, theo khách, yêu cầu và phiên bản hạn cọc. Không thay schema, không sửa bảng thông báo của quản lý. Mở GET không tự ghi đã đọc. Có kiểm tra đăng nhập, chủ sở hữu và chống giả mạo yêu cầu POST.

## Điểm cần thành viên kết nối (chưa sửa vì thuộc phần quản lý/thanh toán)

1. Luồng duyệt hiện tại của quản lý vẫn có hành vi tạo hợp đồng. Cần đổi theo quy ước trên, đặt lại hạn cọc 24 giờ từ thời điểm duyệt. Tab khách không thay luồng này.
2. Lịch giữ xe hiện tại cần được thành viên kiểm tra/điều chỉnh để giữ xe cho yêu cầu trạng thái 2 còn hạn và chưa chuyển thành hợp đồng; hết hạn phải trả lịch. Tab khách chỉ hiển thị hạn và chặn thanh toán, không thay view lịch hay nghiệp vụ duyệt.
3. Nút thanh toán qua endpoint kiểm tra quyền và hạn `/yeu-cau-cua-toi/{id}/thanh-toan`. Có thể cấu hình `CustomerRequests:PaymentPath` là đường dẫn nội bộ của trang thanh toán; endpoint sẽ truyền `requestId`. Khi chưa cấu hình, hiển thị trang thông tin và thông báo thanh toán trực tuyến chưa khả dụng, không tạo hợp đồng và không giả lập thanh toán thành công.
4. Phần thanh toán phải tự kiểm tra lại chủ sở hữu, duyệt, hạn cọc và chống ghi nhận trùng tại lúc thanh toán; không chỉ dựa vào việc nút đang hiển thị. Hợp đồng và giao dịch chỉ ghi theo kết quả thanh toán thực tế.

Phần triển khai tab ban đầu không sửa trang chi tiết xe hay các tệp trong `RentCar_QuanLy`; phần ghép nút gửi yêu cầu sau đó được ghi riêng bên dưới.

## Kiểm tra

`dotnet test tests/RentCar.CustomerRequests.Tests/RentCar.CustomerRequests.Tests.csproj`

11 kiểm tra quy tắc: ba trạng thái, biên hết hạn, đã cọc, không có xe và UTC+7. Kiểm tra HTTP trên cơ sở dữ liệu thử riêng: quyền sở hữu, chống POST thiếu mã xác thực, hủy khi chờ duyệt, từ chối hủy khi đã duyệt, ghi đã đọc lặp không trùng, quá hạn và đường dẫn thanh toán.


## Bổ sung tab Hợp đồng của tôi ngày 26/11/2026

- Đường dẫn `/hop-dong-cua-toi`, controller `MyContractsController`, service `CustomerContracts`, views `RentCar_TrangChu/Views/MyContracts`. Chỉ đọc và chỉ cho khách xem hợp đồng của mình, kể cả bản in và biên nhận.
- Tận dụng `RentalService.GetContractAsync`, `GetDepositReceiptsAsync`, các entity và quy tắc quyết toán của thành viên; không sửa `RentCar_QuanLy`. Danh sách phân trang, tìm theo mã, lọc đủ năm trạng thái. Không có thao tác hủy hay thanh toán trong tab.
- Quyết định mới nhất: một hợp đồng nhiều xe; cọc thành công mới lập hợp đồng ở Chờ xác nhận; khách đến ký thì Đã xác nhận; giao xe xong thì Đang thuê; trả xe và quyết toán đầy đủ mới Hoàn thành. Quy định giữ chỗ 24 giờ, chính sách hủy/hoàn cọc và cách tính thuế/giá theo SRS. Đây là yêu cầu tích hợp, không phải thay đổi đã thực hiện vào nghiệp vụ quản lý.
- Tab hiển thị trạng thái thực tế trong database, không suy diễn đã ký chỉ từ một giao dịch cọc và không tự chuyển trạng thái.
- Tổng giá dùng số liệu hợp đồng đã chốt, không tra giá xe hiện tại. Với hợp đồng theo SRS mà giá đã gồm thuế, phần tạo hợp đồng cần lưu giá phù hợp và không cộng thêm VAT (cấu trúc hiện có dùng thueSuatVat=0 khi không cộng thêm). Hợp đồng cũ có VAT đã lưu vẫn giữ nguyên tổng, không trừ thuế hoặc cộng lại lần nữa trên tab.
- Phân biệt tiền cọc theo hợp đồng với cọc đã thu: chỉ giao dịch loại 1 thành công được cộng vào cọc đã thu; loại 4 là cọc đã hoàn, loại 5 là cọc đã giữ/phí hủy. Giao dịch đảo không vào tổng. Tiền cọc không tự khấu trừ tiền thuê. Không tính khoản hoàn từ tiền cọc chưa từng thu.
- Trạng thái Hoàn thành/Đã hủy, bàn giao và hoàn cọc hiển thị theo dữ liệu quản lý đã ghi nhận. Phần thu cọc/tạo hợp đồng, xác nhận khi ký, giữ chỗ, tính giá SRS và thanh toán do thành viên kết nối sau. Luồng quản lý hiện còn khác quy ước mới về thời điểm lập và xác nhận hợp đồng; chưa sửa do yêu cầu giữ nguyên code quản lý.
- Phần yêu cầu 24 giờ ở các mục trước mô tả triển khai cũ. Cần đồng bộ lại với BR-15 của SRS khi thành viên cập nhật luồng yêu cầu/duyệt; lượt bổ sung tab hợp đồng này không sửa cơ chế hết hạn hiện tại.


## Ghép riêng chức năng gửi yêu cầu từ RentCar(yêu cầu)

- Giữ nguyên giao diện, màu cam, lịch gọn, tab yêu cầu và hợp đồng của bản chính. Không chép Program, cấu hình, model hoặc CSS toàn bộ từ bản thành viên. Không sửa mã quản lý và không thay schema.
- Nút một xe ở `Views/Cars/Details.cshtml` gọi POST `Rental/CreateRequest`. Nút đội xe gọi POST `Rental/CreateListRequest`. Thành công chuyển sang `MyRequests/Index` và đánh dấu vị trí yêu cầu mới bằng đường dẫn neo.
- Dùng lại `RentalService.CreateRequestAsync` qua adapter mới `CustomerRequestSubmission`; không dùng phần INSERT raw SQL ghi giờ Việt Nam vào cột UTC của bản nhập. Kiểm tra khách đăng nhập, chống giả mạo POST, ngày nhận sau hôm nay, từ 4 giờ đến 30 ngày, xe sẵn sàng, điểm chung hợp lệ và từng xe không trùng lịch.
- Một yêu cầu chứa nhiều chi tiết xe; cờ tài xế và phí tài xế 1 triệu/xe/ngày theo từng xe. Gửi nhiều xe trong một giao dịch SERIALIZABLE; một xe lỗi thì không tạo yêu cầu dở dang. Chỉ xóa các xe đã gửi khỏi đội xe sau khi lưu thành công.
- Điểm giao nhận lấy từ danh mục đang hoạt động, kế thừa lựa chọn trang chủ qua session. Cùng id dùng cho nhận và trả. Giờ nhập là UTC+7; EF của dự án đổi sang UTC lúc lưu; hạn giữ chỗ 24 giờ tính từ lúc gửi theo BR-15. Việc gửi không tạo hợp đồng hoặc giao dịch thanh toán.
- Giá dự tính và phần thanh toán hiện tại được giữ nguyên trong lượt ghép này; không sửa phần thành viên phụ trách.
- Kiểm tra trên database thử riêng: gửi một/nhiều xe, giờ nhập mới trên trình duyệt, cờ tài xế từng xe, CSRF, đăng nhập và vai trò, thời gian không hợp lệ, điểm không hợp lệ, lỗi một xe rollback toàn yêu cầu và hai lượt gửi đồng thời chỉ giữ xe một lần.

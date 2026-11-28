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


## Bổ sung trang khách đến tuần 6 ngày 28/11/2026

Nguồn: SRS v1.6 trong Downloads; kế hoạch mẫu 8 tuần và kế hoạch RentCar của nhóm được đối chiếu. Những khác biệt đã được người dùng chốt tiếp tục ưu tiên: nhiều xe trong hợp đồng, lập sau cọc, ký khi nhận, giữ nguyên phần quản lý, không thêm lại gói thuê dài hạn. React thống kê thuộc quản lý nên không đưa vào trang khách.

| Yêu cầu | Kết quả phía khách |
|---|---|
| FR-2.1–2.4, NFR-01 | Lọc lịch, tên, hãng, loại, ghế, hộp số, nhiên liệu, giá; sắp xếp và phân trang SQL. Phân trang cả các trang dịch vụ. |
| FR-3.1–3.2, BR-09/10/12 | Một/nhiều xe, kiểm tra tuổi và GPLX tự lái khi gửi, giờ UTC, giao dịch SERIALIZABLE; báo giá tuần/ngày, chọn một ưu đãi hoặc mức giảm nhiều xe tốt hơn; không cộng VAT lần nữa. |
| FR-3.4, FR-6.2a | Giữ tab yêu cầu/hợp đồng, bản in/biên nhận. Thêm hồ sơ/GPLX riêng cho khách và thêm xe bằng AJAX. |
| FR-6.1b/c, FR-6.5 | Dùng đăng nhập/đăng ký thành viên hiện có; bổ sung giao diện đổi mật khẩu và khôi phục liên kết một lần 30 phút, SMTP/mô phỏng. |
| FR-8.1 phía hiển thị | Đọc thông báo có sẵn, lịch sử và nhắc trả/quá hạn, lưu đã đọc theo đúng chủ sở hữu. Không tự gửi email mới. |
| NFR-02/03/04/08 | Cookie dùng chung, 30 phút, dấu mật khẩu trên phiên khách, rate limit, CSRF, JSON lỗi, validation, che giấy tờ, UTC+7 và giao diện đáp ứng. |
| NFR-05/06/07/09 | Giữ giao dịch kiểm tra lịch, dịch vụ tách lớp, nhật ký qua dịch vụ sẵn có; thêm bộ test và CI. Không thay mã quản lý. |
| NFR-13 | Khách xác nhận ẩn danh bằng mật khẩu khi hết yêu cầu/hợp đồng mở; không xóa giao dịch. Lịch tự dọn dữ liệu 5 năm thuộc vận hành, chưa cài. |
| FR-3.7 | Chưa thêm tác vụ tự hủy vì bị kiểm duyệt tự động từ chối; đang chờ người dùng phê duyệt riêng. UI chỉ đọc và báo hết hạn, không đổi trạng thái. |
| NFR-11/12 | Có health check; backup/uptime/retention và phép thử với 5 người thật chưa nghiệm thu, không coi là đã hoàn thành bằng kiểm tra UI. |

Phần thanh toán và phần quản lý vẫn cần thành viên nối như đã thống nhất. Không khẳng định đã hoàn thành toàn bộ hệ thống hoặc các NFR vận hành chỉ từ phạm vi trang khách.


### Quy ước thời gian cập nhật sau cùng — 28/11/2026

Theo yêu cầu mới nhất, database lưu trực tiếp giờ Việt Nam (UTC+7), thay quy ước UTC trong những ghi chú cũ ở trên. Không thay đổi ba trạng thái yêu cầu, hạn 24 giờ, dấu đã đọc, hợp đồng nhiều xe hoặc quyền của khách. Những DTO *Utc vẫn là dạng chuyển đổi cho đếm ngược/JSON, không phải dữ liệu lưu. Xem README mục cập nhật quanly(moi nhat) và database/UseVietnamLocalTime.sql.


### Giữ giao diện chi tiết của thành viên — 28/11/2026

Theo xác nhận mới nhất, chỉ trang chi tiết xe dùng bố cục/khung đặt xe/CSS của RentCar(yêu cầu). Trang danh sách thuê, trang chủ và các tab của bản chính giữ nguyên. Thư mục gốc của thành viên không bị chỉnh sửa.

- Giữ khung cố định, lịch có nút, ô địa điểm, tài xế, hai dòng tạm tính, nút gửi/thêm và thông báo lỗi của thành viên. Partial lịch riêng _MemberRentalDates không kéo lùi lịch gọn trên trang chủ.
- Sửa xử lý đồng bộ ngày giờ ẩn trước khi gửi; nút lịch được nối với bộ lịch hiện tại. Giữ backend CreateRequest/CustomerRequestSubmission đã kiểm tra, giờ lưu Việt Nam, chống trùng lịch, hạn 24 giờ và không lập hợp đồng khi gửi yêu cầu.
- Cars/Quote chỉ đọc để điền các ô tạm tính có sẵn, dùng đúng giá tuần/ưu đãi/tài xế từ RentalPricing thay cho phép nhân giá ngày đơn giản. Dòng thuế ở đúng vị trí cũ nhưng ghi Giá đã gồm thuế theo cấu hình/SRS hiện tại.
- Đã kiểm thử trình duyệt: mở lịch, tạm tính 8 ngày, chuyển đăng nhập, gửi bằng nút thành viên và kiểm tra DB có giờ 09:00, tài xế 1 triệu/ngày, hạn 24 giờ, không có hợp đồng mới. Kiểm tra invalid dates/duration/ID và trùng lịch. Toàn bộ thao tác ghi thử chỉ dùng database kiểm thử đã dọn.

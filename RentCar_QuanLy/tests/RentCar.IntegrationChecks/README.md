# Kiểm tra tích hợp QL2

Runner này chỉ chấp nhận database tên bắt đầu `RentCar_QL2_Verify_`, không chạy trên RentCarDb_V2.
Tạo database thử có tên đó bằng bản sao CreateRentCarDbV2.sql và SeedRentCarDbV2.sql đã thay tên database.
Dùng database mới có seed cho mỗi lần chạy; runner sẽ tạo yêu cầu, hợp đồng, giao dịch, biên bản và lịch bảo trì.

Đặt biến môi trường RENTCAR_TEST_CONNECTION_STRING trỏ database thử, rồi chạy:

```powershell
dotnet run --project RentCar_QuanLy/tests/RentCar.IntegrationChecks/RentCar.IntegrationChecks.csproj
```

Kiểm tra 10 xe trong một hợp đồng, tài xế từng xe, giảm 10%, VAT 8%, lưu trực tiếp giờ Việt Nam (UTC+7),
chống trùng xe cuối danh sách, ghi cọc/xác nhận, chặn giao thiếu xe, một biên bản chung,
phí hư hỏng đúng xe, trạng thái tất cả xe, quyết toán, bản ghi doanh thu, hết hạn cọc và lịch bảo trì.
Cuối runner có thêm hợp đồng hai xe đã xác nhận để thử biểu mẫu giao nhận qua HTTP trên database này.

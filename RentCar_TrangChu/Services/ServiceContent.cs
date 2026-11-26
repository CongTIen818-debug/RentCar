using RentCar.Models;

namespace RentCar.Services;

// Original RentCar copy informed by RichCars service pages and Vietnam Transport / Saigon VIP Service.
public static class ServiceContent
{
    public static ServicePageViewModel Create(string slug, IReadOnlyList<VehicleSearchResult> cars) => slug switch
    {
        "thue-xe-tu-lai" => new(slug, "Thuê xe tự lái", "Cầm lái chiếc xe bạn thích. Tự chọn nhịp đi cho hành trình.",
            "/images/services/self-drive.png",
            "Một chuyến nghỉ dưỡng, buổi gặp gỡ quan trọng hay đơn giản là trải nghiệm chiếc xe bạn yêu thích: RentCar giúp bạn chọn từng chiếc xe cụ thể, kiểm tra lịch trống và gom nhiều xe vào cùng danh sách thuê.",
            [
                new("Riêng tư trên từng chặng đường", "Bạn chủ động lịch trình, điểm dừng và người đồng hành. Xe hai chỗ phù hợp chuyến đi đôi; sedan và SUV dành cho nhóm cần thêm không gian."),
                new("Chọn đúng chiếc xe", "Mỗi thẻ là một xe riêng biệt. Xem hình ảnh, số chỗ và giá theo ngày trước khi thêm xe vào danh sách; các xe dưới đây được xếp theo số ghế từ ít đến nhiều."),
                new("Chuẩn bị trước ngày nhận xe", "Chuẩn bị giấy tờ tùy thân và giấy phép lái xe phù hợp. Điều kiện thuê, khoản cọc, phạm vi sử dụng và tình trạng xe cần được xác nhận trong hợp đồng và biên bản bàn giao."),
                new("Kiểm tra xe khi giao và trả", "Cùng nhân viên kiểm tra ngoại thất, nội thất và tình trạng vận hành; ghi nhận hình ảnh và các lưu ý trước khi ký biên bản. Các khoản phát sinh được đối chiếu khi trả xe.")
            ],
            [
                new("Một người có thể thuê nhiều xe không?", "Có. Bạn có thể thêm nhiều xe khác nhau vào danh sách thuê cho nhóm bạn hoặc gia đình, dùng chung thời gian nhận và trả."),
                new("Thời gian thuê được tính thế nào?", "Mỗi ngày thuê tương ứng 24 giờ; phần ngày lẻ được làm tròn lên. Ngày nhận phải sau ngày hôm nay và ngày giờ trả phải sau ngày giờ nhận."),
                new("Có thể đổi sang thuê có tài xế không?", "Với xe trên 2 chỗ, bạn có thể tích Kèm tài xế ở trang chi tiết hoặc danh sách thuê. Phí tài xế được cộng riêng 1.000.000 đồng/xe/ngày.")
            ], "Chọn xe tự lái · Số ghế tăng dần", cars),
        "thue-xe-kem-tai-xe" => new(slug, "Thuê xe kèm tài xế", "Dành thời gian cho cuộc gặp. Để hành trình được sắp xếp gọn gàng.",
            "/images/services/chauffeur.jpg",
            "Dịch vụ xe kèm tài xế phù hợp cho công tác, đón tiếp đối tác, đưa đón khách và các chuyến đi cần người lái riêng. Bạn chọn xe, thống nhất lịch trình và xem riêng tiền xe cùng phí tài xế ngay trong danh sách thuê.",
            [
                new("Công tác và đón tiếp đối tác", "Sedan hạng sang tạo không gian riêng cho việc nghỉ ngơi và chuẩn bị trước cuộc gặp. Thống nhất địa điểm đón, giờ hẹn và các điểm dừng để sắp xếp hành trình."),
                new("Đưa đón sân bay và nghỉ dưỡng", "Chuẩn bị thông tin chuyến bay, số người và hành lý. Chọn xe có đủ không gian cho hành khách sau khi tính cả ghế của tài xế."),
                new("Ưu tiên xe cao cấp", "Danh sách chỉ gồm xe trên 2 chỗ, xếp theo giá thuê xe từ cao xuống thấp. Khi mở chi tiết từ trang này, lựa chọn Kèm tài xế đã được tích sẵn và bạn vẫn có thể bỏ chọn."),
                new("Phí tài xế minh bạch", "Mỗi xe chọn kèm tài xế cộng 1.000.000 đồng cho mỗi ngày thuê. Phí này tách khỏi tiền xe và không nằm trong khoản giảm giá theo số lượng.")
            ],
            [
                new("Vì sao không có xe 2 chỗ?", "Một ghế phải dành cho người lái. RentCar giới hạn danh sách dịch vụ này ở xe trên 2 chỗ để có thêm không gian cho hành khách."),
                new("Tôi có thể chọn tài xế cho một vài xe không?", "Có. Mỗi xe trong danh sách thuê có lựa chọn tài xế riêng. Bạn có thể kết hợp xe tự lái và xe có tài xế trong cùng danh sách."),
                new("Phí cầu đường, đỗ xe có nằm trong phí tài xế không?", "Bảng dự tính hiện tách tiền xe, phí tài xế và VAT. Các chi phí cầu đường, đỗ xe, ăn ở hoặc lịch trình ngoài thỏa thuận cần được xác nhận riêng trước khi ký hợp đồng.")
            ], "Xe kèm tài xế · Ưu tiên dòng cao cấp", cars),
        _ => new(slug, "Thuê xe sự kiện", "Một đội xe cho ngày đặc biệt. Một danh sách thuê để cùng quản lý.",
            "/images/services/event-rental.png",
            "RentCar hỗ trợ thuê nhiều xe để phục vụ lễ cưới, hội nghị, ra mắt sản phẩm, chụp hình và đón tiếp khách mời. Chọn nhiều chiếc xe khác nhau, tập hợp vào cùng danh sách và thống nhất thời gian thuê cho cả đội xe.",
            [
                new("Lễ cưới và ngày kỷ niệm", "Kết hợp xe dành cho nhân vật chính với xe đưa đón gia đình, khách mời. Màu sắc, số chỗ và thời gian di chuyển là những thông tin nên chốt trước."),
                new("Hội nghị và sự kiện doanh nghiệp", "Sắp xếp xe đón đối tác, khách VIP và nhóm tổ chức theo từng điểm đón. Chuẩn bị danh sách hành khách, giờ hẹn và đầu mối liên hệ để thuận tiện phối hợp."),
                new("Ra mắt sản phẩm và chụp hình", "Chọn chiếc xe phù hợp phong cách chương trình, bối cảnh và hình ảnh thương hiệu. Mục đích trưng bày hoặc chụp hình cần được xác nhận khi thống nhất hợp đồng."),
                new("Thuê nhiều xe trong một hợp đồng", "Bạn có thể chọn nhiều xe sẵn có thay vì lập danh sách riêng cho từng chiếc. Kết hợp sedan, SUV và xe thể thao theo nhu cầu của chương trình; lịch trống được kiểm tra khi bạn nhập ngày nhận và trả.")
            ],
            [
                new("Thuê theo đoàn được giảm thế nào?", "Hợp đồng từ 6 đến 9 xe giảm 6% tiền xe; từ 10 xe trở lên giảm 10%. Hai mức không cộng dồn. Phí tài xế tính riêng, sau đó mới tính VAT 8%."),
                new("Cần chuẩn bị thông tin gì cho sự kiện?", "Chuẩn bị loại sự kiện, thời gian, địa điểm, số xe, số khách, các điểm đón và nhu cầu tài xế. Với danh sách trên website, các xe dùng chung thời gian nhận và trả."),
                new("Có thể thay đổi xe trước ngày tổ chức không?", "Việc đổi xe phụ thuộc lịch trống và thỏa thuận hợp đồng. Hãy xác nhận thay đổi trước khi nhận xe để cập nhật danh sách và chi phí.")
            ], "Danh sách xe phục vụ sự kiện", cars)
    };
}

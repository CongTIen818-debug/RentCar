using Microsoft.EntityFrameworkCore;
using RentCar.Application.Abstractions;
using RentCar.Domain.Common;
using RentCar.Domain.Entities;
using RentCar.Domain.Enums;
using RentCar.Domain.Pricing;
using RentCar.Domain.Rules;

namespace RentCar.Application.Rentals;

/// <summary>
/// UC-02 (yêu cầu thuê, lập và xác nhận hợp đồng), ghi nhận giao dịch (FR-5.1) và truy vấn hợp đồng.
/// Mọi thay đổi trạng thái đều ghi lịch sử hợp đồng và nhật ký kiểm toán trong cùng một giao dịch (NFR-05, NFR-09).
/// </summary>
public sealed class RentalService(IAppDbContext db)
{
    public Task<RentalRequest> CreateRequestAsync(int customerId, int? createdById, int vehicleId,
        DateTime startAt, DateTime endAt, int pickupLocationId, int returnLocationId, CancellationToken ct = default) =>
        CreateRequestAsync(customerId, createdById, [new RequestedVehicle(vehicleId, false)], startAt, endAt, pickupLocationId, returnLocationId, ct);

    public async Task<RentalRequest> CreateRequestAsync(int customerId, int? createdById,
        IReadOnlyList<RequestedVehicle> requested, DateTime startAt, DateTime endAt,
        int pickupLocationId, int returnLocationId, CancellationToken ct = default)
    {
        var now = VietnamTime.Now;
        BookingRules.ValidateWindow(startAt, endAt);
        if (startAt <= now) throw new DomainException("Thời điểm nhận xe phải sau hiện tại.");
        if (requested.Count == 0 || requested.Select(x => x.VehicleId).Distinct().Count() != requested.Count)
            throw new DomainException("Danh sách xe phải có ít nhất một xe và không được lặp xe.");
        var ids = requested.Select(x => x.VehicleId).ToArray();
        var cars = await db.Vehicles.AsNoTracking().Where(v => ids.Contains(v.Id)).ToListAsync(ct);
        if (cars.Count != ids.Length) throw new DomainException("Có xe không tồn tại.", 404);
        if (cars.Any(v => v.Status is VehicleStatus.Maintenance or VehicleStatus.Retired))
            throw new DomainException("Có xe đang bảo trì hoặc ngừng kinh doanh.", 409);
        if (requested.Any(x => x.WithDriver && cars.Single(v => v.Id == x.VehicleId).Seats <= 2))
            throw new DomainException("Chỉ xe trên 2 chỗ mới hỗ trợ kèm tài xế.");
        var expected = pickupLocationId == returnLocationId ? 1 : 2;
        if (await db.PickupLocations.CountAsync(p => p.IsActive && (p.Id == pickupLocationId || p.Id == returnLocationId), ct) != expected)
            throw new DomainException("Điểm nhận hoặc trả không hợp lệ.");
        var available = await db.AvailableVehicleIdsAsync(startAt, endAt, ct);
        if (ids.Any(id => !available.Contains(id))) throw new DomainException("Có xe không trống lịch đã chọn.", 409);
        var request = new RentalRequest
        {
            CustomerId = customerId, CreatedById = createdById, StartAt = startAt, EndAt = endAt,
            PickupLocationId = pickupLocationId, ReturnLocationId = returnLocationId,
            Status = RentalRequestStatus.Pending, CreatedAt = now, HoldUntil = now.AddHours(BookingRules.HoldHours),
            Lines = requested.Select(x => new RentalRequestLine
            {
                VehicleId = x.VehicleId, WithDriver = x.WithDriver, DriverDailyFee = x.WithDriver ? 1_000_000m : 0m
            }).ToList()
        };
        db.RentalRequests.Add(request);
        await db.SaveChangesAsync(ct);
        return request;
    }

    /// <summary>
    /// UC-02 A7: nhân viên đặt xe hộ khách đã có tài khoản và hồ sơ. Khách được tìm theo email hoặc số điện thoại.
    /// Yêu cầu ghi nhân viên là người tạo (idNguoiTao) và có nhật ký kiểm toán; việc tạo và ghi nhật ký cùng một giao dịch.
    /// </summary>
    public async Task<RentalRequest> CreateRequestForCustomerAsync(int staffId, string? customerKey, int vehicleId,
        DateTime startAt, DateTime endAt, int pickupLocationId, int returnLocationId, CancellationToken ct = default)
    {
        var customer = await FindCustomerForBookingAsync(customerKey, ct);

        await using var tx = await db.BeginTransactionAsync(ct);
        try
        {
            var request = await CreateRequestAsync(customer.Id, staffId, vehicleId, startAt, endAt,
                pickupLocationId, returnLocationId, ct);

            db.AuditLogs.Add(new AuditLog
            {
                ActorId = staffId,
                ObjectType = "YeuCauThue",
                ObjectId = request.Id.ToString(),
                Action = "DAT_XE_HO_KHACH",
                NewValue = $"khach={customer.Id}; xe={vehicleId}; nhan={startAt:yyyy-MM-dd HH:mm}; tra={endAt:yyyy-MM-dd HH:mm}",
                At = VietnamTime.Now
            });
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            request.Customer = customer;
            return request;
        }
        catch (DomainException ex) when (IsScheduleConflict(ex))
        {
            // Hai người cùng giữ một xe một lúc: trigger CSDL từ chối người đến sau (BR-01).
            // AppDbContext đổi lỗi RAISERROR (mã 50000) thành DomainException 409 nên bắt ở đây.
            throw new DomainException("Xe vừa được giữ chỗ trong khoảng thời gian này, vui lòng chọn lại (BR-01).", 409);
        }
    }

    /// <summary>
    /// FR-3.5: hủy yêu cầu thuê chưa lập hợp đồng. Khách chỉ hủy được yêu cầu của chính mình (BR-06);
    /// nhân viên hủy được mọi yêu cầu nhưng phải ghi lý do (NFR-09). Hợp đồng đã lập thì hủy ở hợp đồng (FR-3.6).
    /// </summary>
    public async Task<RentalRequest> CancelRequestAsync(int requestId, int actorId, bool isStaff, string? reason,
        CancellationToken ct = default)
    {
        var request = await db.RentalRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct);
        // Khách xem yêu cầu của người khác cũng chỉ thấy "không tìm thấy", không lộ yêu cầu đó có tồn tại.
        if (request is null || (!isStaff && request.CustomerId != actorId))
            throw new DomainException("Không tìm thấy yêu cầu thuê.", 404);

        if (request.Status != RentalRequestStatus.Pending)
            throw new DomainException(request.Status == RentalRequestStatus.ConvertedToContract
                ? "Yêu cầu đã được lập hợp đồng. Hãy hủy ở hợp đồng tương ứng (FR-3.6)."
                : "Yêu cầu này đã được hủy trước đó.", 409);

        var text = (reason ?? "").Trim();
        if (isStaff && text.Length == 0)
            throw new DomainException("Vui lòng nhập lý do hủy yêu cầu.");
        if (text.Length > 200) text = text[..200];

        request.Status = RentalRequestStatus.Cancelled;
        db.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            ObjectType = "YeuCauThue",
            ObjectId = request.Id.ToString(),
            Action = "HUY_YEU_CAU",
            OldValue = "trangThai=1",
            NewValue = $"trangThai=3; boi={(isStaff ? "nhanVien" : "khach")}; lyDo={(text.Length == 0 ? "khong" : text)}",
            At = VietnamTime.Now
        });

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DomainException ex) when (ex.StatusCode == 409 && ex.Message.Contains("BR-08", StringComparison.Ordinal))
        {
            // Yêu cầu vừa được chuyển sang hợp đồng ở một phiên khác: trigger BR-08 từ chối 2 -> 3.
            throw new DomainException("Yêu cầu vừa được xử lý ở nơi khác. Vui lòng tải lại trang.", 409);
        }
        return request;
    }

    /// <summary>UC-02 bước 5 đến 6: tính giá, lập hợp đồng Chờ xác nhận và chuyển yêu cầu sang Đã chuyển hợp đồng.</summary>
    public async Task<RentalContract> CreateContractFromRequestAsync(int requestId, int staffId, string? promotionCode = null, CancellationToken ct = default)
    {
        var now = VietnamTime.Now;
        var request = await db.RentalRequests
            .Include(r => r.Lines).ThenInclude(l => l.Vehicle).ThenInclude(v => v.VehicleType)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw new DomainException("Không tìm thấy yêu cầu thuê.", 404);

        if (request.Status != RentalRequestStatus.Pending)
            throw new DomainException("Yêu cầu không còn ở trạng thái Chờ xử lý.", 409);

        if (request.HoldUntil <= now)
        {
            // BR-15: quá hạn giữ chỗ thì tự hủy yêu cầu để giải phóng lịch xe; vẫn ghi nhật ký (NFR-09).
            request.Status = RentalRequestStatus.Cancelled;
            db.AuditLogs.Add(new AuditLog
            {
                ActorId = staffId,
                ObjectType = "YeuCauThue",
                ObjectId = request.Id.ToString(),
                Action = "TU_HUY_QUA_HAN",
                OldValue = "trangThai=1",
                NewValue = "trangThai=3; lyDo=het han giu cho (BR-15)",
                At = now
            });
            await db.SaveChangesAsync(ct);
            throw new DomainException("Yêu cầu quá 24 giờ chưa xử lý nên đã bị hủy và giải phóng lịch xe (BR-15).", 409);
        }

        if (request.Lines.Count == 0) throw new DomainException("Yêu cầu không có xe.", 409);
        var lines = new List<ContractLine>();
        foreach (var requested in request.Lines)
        {
            var vehicle = requested.Vehicle;
            if (requested.WithDriver && vehicle.Seats <= 2) throw new DomainException("Xe 2 chỗ không hỗ trợ tài xế.");
            var factor = await GetPriceFactorAsync(vehicle.VehicleTypeId, request.StartAt, ct);
            var quote = PricingCalculator.Quote(request.StartAt, request.EndAt, vehicle.DailyRate, vehicle.WeeklyRate, factor);
            lines.Add(new ContractLine
            {
                VehicleId = vehicle.Id, DailyRateAtBooking = quote.DailyRate, WeeklyRateAtBooking = quote.WeeklyRate,
                DayCount = quote.DayCount, WeekCount = quote.WeekCount,
                WithDriver = requested.WithDriver, DriverDailyFee = requested.WithDriver ? requested.DriverDailyFee : 0m
            });
        }
        var gross = lines.Sum(l => l.GrossLine);
        var driver = lines.Sum(l => l.DayCount * l.DriverDailyFee);

        Promotion? promotion = null;
        if (!string.IsNullOrWhiteSpace(promotionCode))
        {
            var code = promotionCode.Trim().ToUpperInvariant();
            promotion = await db.Promotions.AsNoTracking().FirstOrDefaultAsync(p =>
                p.Code == code && p.IsActive && p.StartAt <= now && now < p.EndAt, ct);
            if (promotion is null)
                throw new DomainException("Mã ưu đãi không hợp lệ, đã hết hạn hoặc chưa được kích hoạt.");
        }
        var bulkPercent = lines.Count >= 10 ? 10m : lines.Count >= 6 ? 6m : 0m;
        var percent = Math.Max(promotion?.DiscountPercent ?? 0m, bulkPercent);
        var usePromotion = promotion is not null && promotion.DiscountPercent >= bulkPercent;
        var discount = decimal.Round(gross * percent / 100m, 0, MidpointRounding.AwayFromZero);

        await using var tx = await db.BeginTransactionAsync(ct);

        request.Status = RentalRequestStatus.ConvertedToContract;
        var contract = new RentalContract
        {
            Code = NewCode("HD", now),
            CreatedAt = now,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            GrossAmount = gross,
            DriverAmount = driver,
            VatRate = .08m,
            PromotionId = usePromotion ? promotion!.Id : null,
            PromotionName = usePromotion ? promotion!.Name : percent > 0 ? "Ưu đãi thuê nhiều xe" : null,
            PromotionPercent = percent > 0 ? percent : null,
            DiscountAmount = discount,
            DepositAmount = request.Lines.Sum(l => l.Vehicle.DepositAmount),
            Status = ContractStatus.PendingConfirmation,
            HoldUntil = now.AddHours(BookingRules.HoldHours),
            CustomerId = request.CustomerId,
            CreatedById = staffId,
            RentalRequestId = request.Id,
            PickupLocationId = request.PickupLocationId,
            ReturnLocationId = request.ReturnLocationId,
            Lines = lines
        };
        contract.History.Add(new ContractHistory
        {
            FromStatus = null,
            ToStatus = ContractStatus.PendingConfirmation,
            ChangedAt = now,
            ChangedById = staffId,
            Note = "Lập hợp đồng từ yêu cầu thuê"
        });
        db.Contracts.Add(contract);
        db.AuditLogs.Add(new AuditLog
        {
            ActorId = staffId,
            ObjectType = "HopDong",
            ObjectId = contract.Code,
            Action = "LAP_HOP_DONG",
            NewValue = $"trangThai=0; tongTienGoc={gross}; uuDai={promotion?.Code ?? "khong"}; giam={discount}; tongThanhToan={contract.TotalPayable}",
            At = now
        });

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return contract;
    }

    /// <summary>Tải đầy đủ thông tin để kiểm tra điều kiện xác nhận (BR-03).</summary>
    public async Task<IReadOnlyList<ConfirmationCheck>> EvaluateChecksAsync(RentalContract contract, CancellationToken ct = default)
    {
        var now = VietnamTime.Now;
        var paid = await db.Payments.AsNoTracking()
            .Where(p => p.ContractId == contract.Id && p.Type == PaymentType.Deposit && p.StatusCode == 1)
            .SumAsync(p => p.Amount, ct);
        var clash = await HasScheduleClashAsync(contract, now, ct);
        return ConfirmationRules.Evaluate(contract, paid, clash);
    }

    /// <summary>UC-02 bước 7: nhân viên xác nhận hợp đồng sau khi đủ cọc. Chỉ chuyển nếu mọi điều kiện đạt.</summary>
    public async Task<RentalContract> ConfirmContractAsync(int contractId, int staffId, CancellationToken ct = default)
    {
        var now = VietnamTime.Now;
        var contract = await db.Contracts
            .Include(c => c.Customer).ThenInclude(u => u.DriverLicense)
            .Include(c => c.Lines).ThenInclude(l => l.Vehicle).ThenInclude(v => v.VehicleType)
            .FirstOrDefaultAsync(c => c.Id == contractId, ct)
            ?? throw new DomainException("Không tìm thấy hợp đồng.", 404);

        if (contract.Status != ContractStatus.PendingConfirmation)
            throw new DomainException("Hợp đồng không ở trạng thái Chờ xác nhận.", 409);
        if (contract.HoldUntil is { } hold && hold <= now)
            throw new DomainException("Hợp đồng quá 24 giờ chưa đặt cọc nên hết hạn giữ chỗ (BR-15).", 409);

        var checks = await EvaluateChecksAsync(contract, ct);
        if (!ConfirmationRules.CanConfirm(checks))
        {
            var failed = checks.Where(c => !c.Passed).Select(c => c.Label);
            throw new DomainException("Chưa đủ điều kiện xác nhận: " + string.Join("; ", failed), 409);
        }

        await using var tx = await db.BeginTransactionAsync(ct);
        contract.Status = ContractStatus.Confirmed;
        contract.History.Add(new ContractHistory
        {
            FromStatus = ContractStatus.PendingConfirmation,
            ToStatus = ContractStatus.Confirmed,
            ChangedAt = now,
            ChangedById = staffId,
            Note = "Xác nhận hợp đồng sau khi đủ cọc"
        });
        db.AuditLogs.Add(new AuditLog
        {
            ActorId = staffId,
            ObjectType = "HopDong",
            ObjectId = contract.Code,
            Action = "XAC_NHAN_HOP_DONG",
            OldValue = "trangThai=0",
            NewValue = "trangThai=1",
            At = now
        });

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return contract;
    }

    /// <summary>FR-5.1 và BR-04: ghi nhận đặt cọc hoặc thanh toán, có người thực hiện và thời điểm.</summary>
    public async Task<Payment> RecordPaymentAsync(int contractId, PaymentType type, decimal amount, string method,
        int actorId, CancellationToken ct = default)
    {
        if (type != PaymentType.Deposit)
            throw new DomainException("Thanh toán sau thuê được ghi nhận ở mục Bàn giao và nhận xe.");
        if (!PaymentMethods.IsAllowed(method))
            throw new DomainException("Hình thức thanh toán không hợp lệ.");
        if (amount <= 0)
            throw new FieldValidationException(new Dictionary<string, string> { ["Amount"] = "Số tiền phải lớn hơn 0 (BR-02)." });

        var contract = await db.Contracts.FirstOrDefaultAsync(c => c.Id == contractId, ct)
            ?? throw new DomainException("Không tìm thấy hợp đồng.", 404);

        if (contract.Status is ContractStatus.Cancelled or ContractStatus.Completed)
            throw new DomainException("Hợp đồng đã đóng, không ghi nhận thêm giao dịch (BR-04).", 409);
        if (type == PaymentType.Deposit && contract.Status != ContractStatus.PendingConfirmation)
            throw new DomainException("Chỉ ghi nhận đặt cọc khi hợp đồng đang Chờ xác nhận (FR-3.3b).", 409);
        if (type == PaymentType.Deposit && contract.HoldUntil is { } holdUntil && holdUntil <= VietnamTime.Now)
            throw new DomainException(HoldExpiredMessage, 409);

        var now = VietnamTime.Now;
        await using var tx = await db.BeginTransactionAsync(ct);
        if (type == PaymentType.Deposit)
        {
            // #12 (BR-15): khóa dòng hợp đồng bằng UPDATE có điều kiện trong cùng giao dịch. Thủ tục tự hủy quá hạn
            // cập nhật đúng dòng này, nên hai bên không thể chạy chồng nhau: hoặc cọc vào trước (thủ tục bỏ qua hợp đồng),
            // hoặc thủ tục hủy trước (cọc bị từ chối). Giờ so sánh theo đồng hồ CSDL, cùng nguồn với spHuyHopDongQuaHanGiuCho.
            var locked = await db.ExecuteSqlAsync(
                $"UPDATE dbo.hopDong SET trangThai = trangThai WHERE idHopDong = {contractId} AND trangThai = 0 AND hanGiuCho > SYSUTCDATETIME()",
                ct);
            if (locked == 0) throw new DomainException(HoldExpiredMessage, 409);
        }

        var payment = new Payment
        {
            Code = NewCode("GD", now),
            ContractId = contractId,
            Amount = amount,
            Type = type,
            Method = method,
            PaidAt = now,
            StatusCode = 1,
            ActorId = actorId
        };
        db.Payments.Add(payment);
        db.AuditLogs.Add(new AuditLog
        {
            ActorId = actorId,
            ObjectType = "ThanhToan",
            ObjectId = payment.Code,
            Action = "GHI_NHAN_GIAO_DICH",
            NewValue = $"loai={(int)type}; soTien={amount}; hopDong={contract.Code}",
            At = now
        });

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return payment;
    }

    /// <summary>
    /// FR-3.6 và UC-03 A4: hủy hợp đồng đã xác nhận nhưng chưa bàn giao. Tiền cọc xử lý theo BR-14:
    /// phần giữ lại ghi thành giao dịch Giữ cọc, phần hoàn để kế toán chi sau.
    /// Khách không đến (NoShow) chỉ được ghi nhận sau 4 giờ kể từ giờ nhận xe.
    /// </summary>
    public async Task<RentalContract> CancelContractAsync(int contractId, int staffId, CancellationKind kind,
        string reason, CancellationToken ct = default)
    {
        var now = VietnamTime.Now;
        var contract = await db.Contracts
            .Include(c => c.Handover)
            .Include(c => c.History)
            .Include(c => c.Payments)
            .FirstOrDefaultAsync(c => c.Id == contractId, ct)
            ?? throw new DomainException("Không tìm thấy hợp đồng.", 404);

        if (contract.Status != ContractStatus.Confirmed || contract.Handover is not null)
            throw new DomainException("Chỉ hủy hợp đồng đã xác nhận và chưa bàn giao xe (FR-3.6).", 409);
        if (kind == CancellationKind.NoShow && now < contract.StartAt.AddHours(BookingRules.NoShowHours))
            throw new DomainException($"Chỉ ghi nhận khách không đến sau {BookingRules.NoShowHours} giờ kể từ giờ nhận xe (UC-03 A4).", 409);
        if (string.IsNullOrWhiteSpace(reason))
            throw new DomainException("Vui lòng nhập lý do hủy.");

        var settlement = PricingCalculator.SettleDeposit(kind, now, contract.StartAt, contract.DepositAmount);
        var trimmed = reason.Trim();
        var shortReason = trimmed.Length > 255 ? trimmed[..255] : trimmed;

        await using var tx = await db.BeginTransactionAsync(ct);

        contract.Status = ContractStatus.Cancelled;
        contract.CancelReason = shortReason;
        contract.History.Add(new ContractHistory
        {
            FromStatus = ContractStatus.Confirmed,
            ToStatus = ContractStatus.Cancelled,
            ChangedAt = now,
            ChangedById = staffId,
            Note = shortReason.Length > 200 ? shortReason[..200] : shortReason
        });
        if (settlement.Retained > 0)
        {
            // Method để trống vì CSDL không có hình thức "Giữ cọc" (CK_thanhToan_phuongThuc).
            contract.Payments.Add(new Payment
            {
                Code = NewCode("GD", now),
                Amount = settlement.Retained,
                Type = PaymentType.Forfeit,
                Method = null,
                PaidAt = now,
                StatusCode = 1,
                ActorId = staffId
            });
        }
        db.AuditLogs.Add(new AuditLog
        {
            ActorId = staffId,
            ObjectType = "HopDong",
            ObjectId = contract.Code,
            Action = "HUY_HOP_DONG",
            OldValue = "trangThai=1",
            NewValue = $"trangThai=4; loai={kind}; hoanCoc={settlement.Refund}; giuCoc={settlement.Retained}",
            At = now
        });

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return contract;
    }

    public Task<List<RentalRequest>> GetRequestsForStaffAsync(CancellationToken ct = default) =>
        db.RentalRequests.AsNoTracking()
            .Include(r => r.Customer).Include(r => r.Lines).ThenInclude(l => l.Vehicle)
            .OrderByDescending(r => r.CreatedAt).Take(100)
            .ToListAsync(ct);

    public Task<List<RentalContract>> GetContractsForStaffAsync(CancellationToken ct = default) =>
        db.Contracts.AsNoTracking()
            .Include(c => c.Customer)
            .Include(c => c.Lines).ThenInclude(l => l.Vehicle)
            .OrderByDescending(c => c.CreatedAt).Take(100)
            .ToListAsync(ct);

    public Task<List<RentalRequest>> GetRequestsForCustomerAsync(int customerId, CancellationToken ct = default) =>
        db.RentalRequests.AsNoTracking()
            .Include(r => r.Lines).ThenInclude(l => l.Vehicle)
            .Where(r => r.CustomerId == customerId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

    public Task<List<RentalContract>> GetContractsForCustomerAsync(int customerId, CancellationToken ct = default) =>
        db.Contracts.AsNoTracking()
            .Include(c => c.Lines).ThenInclude(l => l.Vehicle)
            .Where(c => c.CustomerId == customerId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(ct);

    /// <summary>
    /// Chi tiết hợp đồng. Khi customerId có giá trị, chỉ trả về hợp đồng của chính khách đó (BR-06, NFR-03).
    /// </summary>
    public async Task<RentalContract?> GetContractAsync(int id, int? customerId, CancellationToken ct = default)
    {
        IQueryable<RentalContract> query = db.Contracts.AsNoTracking()
            .Include(c => c.Customer).ThenInclude(u => u.DriverLicense)
            .Include(c => c.Lines).ThenInclude(l => l.Vehicle).ThenInclude(v => v.VehicleType)
            .Include(c => c.PickupLocation)
            .Include(c => c.ReturnLocation)
            .Include(c => c.History)
            .Include(c => c.Payments)
            .Include(c => c.Handover).ThenInclude(h => h.Images)
            .Where(c => c.Id == id);

        if (customerId is { } cid) query = query.Where(c => c.CustomerId == cid);
        return await query.FirstOrDefaultAsync(ct);
    }

    private async Task<bool> HasScheduleClashAsync(RentalContract contract, DateTime now, CancellationToken ct)
    {
        var vehicleIds = contract.Lines.Select(l => l.VehicleId).ToArray();
        var start = contract.StartAt;
        var end = contract.EndAt;

        var contractClash = await db.ContractLines.AsNoTracking().AnyAsync(l =>
            vehicleIds.Contains(l.VehicleId) && l.ContractId != contract.Id
            && (l.Contract.Status == ContractStatus.Confirmed
                || l.Contract.Status == ContractStatus.Active
                || (l.Contract.Status == ContractStatus.PendingConfirmation && l.Contract.HoldUntil > now))
            && l.Contract.StartAt < end
            && ((l.Contract.Status == ContractStatus.Active && (l.Contract.Handover == null || l.Contract.Handover.ReturnedAt == null))
                || start < (l.Contract.Handover != null && l.Contract.Handover.ReturnedAt != null ? l.Contract.Handover.ReturnedAt.Value : l.Contract.EndAt)), ct);
        if (contractClash) return true;

        return await db.RentalRequests.AsNoTracking().AnyAsync(r =>
            r.Lines.Any(l => vehicleIds.Contains(l.VehicleId)) && r.Id != contract.RentalRequestId && r.Status == RentalRequestStatus.Pending && r.HoldUntil > now
            && r.StartAt < end && start < r.EndAt, ct);
    }

    private async Task<decimal> GetPriceFactorAsync(int vehicleTypeId, DateTime at, CancellationToken ct)
    {
        var factor = await db.PriceRules.AsNoTracking()
            .Where(r => r.VehicleTypeId == vehicleTypeId && r.IsActive && r.StartAt <= at && at < r.EndAt)
            .Select(r => (decimal?)r.Factor)
            .FirstOrDefaultAsync(ct);
        return factor ?? 1m;
    }

    /// <summary>
    /// Tìm khách để đặt hộ theo email (duy nhất) hoặc số điện thoại. Bắt buộc là tài khoản khách đang hoạt động,
    /// có hồ sơ và GPLX. Nếu số điện thoại trùng nhiều tài khoản thì không đoán, yêu cầu dùng email.
    /// </summary>
    private async Task<User> FindCustomerForBookingAsync(string? customerKey, CancellationToken ct)
    {
        var key = (customerKey ?? "").Trim();
        if (key.Length == 0)
            throw new DomainException("Vui lòng nhập email hoặc số điện thoại của khách.");

        var query = db.Users
            .Include(u => u.Role)
            .Include(u => u.DriverLicense)
            .Where(u => u.Role.Code == RoleCodes.Customer);

        if (key.Contains('@'))
        {
            var email = key.ToLower();
            query = query.Where(u => u.Email.ToLower() == email);
        }
        else
        {
            // Số điện thoại lưu dạng chỉ có chữ số; bỏ khoảng trắng, dấu chấm, gạch nối khi nhập.
            var phone = new string(key.Where(char.IsDigit).ToArray());
            if (phone.Length < 10)
                throw new DomainException("Vui lòng nhập email hoặc số điện thoại hợp lệ của khách.");
            query = query.Where(u => u.Phone == phone);
        }

        var matches = await query.Take(2).ToListAsync(ct);

        if (matches.Count == 0)
            throw new DomainException("Không tìm thấy khách hàng này. Khách cần đăng ký tài khoản trước khi nhân viên đặt hộ (UC-02 A7).", 404);
        if (matches.Count > 1)
            throw new DomainException("Có nhiều tài khoản trùng số điện thoại này. Vui lòng nhập email của khách.");

        var customer = matches[0];
        if (!customer.IsActive)
            throw new DomainException("Tài khoản của khách đang bị khóa, không thể đặt xe hộ.", 403);

        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(customer.FullName)) missing.Add("họ tên");
        if (string.IsNullOrWhiteSpace(customer.Phone)) missing.Add("số điện thoại");
        if (string.IsNullOrWhiteSpace(customer.IdNumber)) missing.Add("giấy tờ tùy thân");
        if (customer.BirthDate is null) missing.Add("ngày sinh");
        if (customer.DriverLicense is null) missing.Add("GPLX");
        if (missing.Count > 0)
            throw new DomainException("Hồ sơ của khách còn thiếu: " + string.Join(", ", missing)
                                      + ". Khách cần bổ sung ở trang Hồ sơ và GPLX trước khi đặt hộ.");
        return customer;
    }

    /// <summary>Trigger CSDL báo chồng lịch (BR-01) đã được AppDbContext đổi thành DomainException 409.</summary>
    private static bool IsScheduleConflict(DomainException ex) =>
        ex.StatusCode == 409 && ex.Message.Contains("BR-01", StringComparison.Ordinal);

    /// <summary>Mã theo giây cộng 6 ký tự ngẫu nhiên từ GUID, tránh trùng mã UNIQUE khi hai giao dịch cùng giây.</summary>
    private static string NewCode(string prefix, DateTime now) =>
        $"{prefix}{now:yyyyMMddHHmmss}{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    private const string HoldExpiredMessage =
        "Hợp đồng đã hết hạn giữ chỗ hoặc không còn ở trạng thái Chờ xác nhận, nên không nhận đặt cọc (BR-15).";

    /// <summary>
    /// v8 (#13, FR-3.4): các khoản đặt cọc đã ghi nhận của hợp đồng, kèm tên người thu, dùng cho biên nhận.
    /// </summary>
    public async Task<IReadOnlyList<DepositReceiptLine>> GetDepositReceiptsAsync(int contractId, CancellationToken ct = default)
    {
        var deposits = await db.Payments.AsNoTracking()
            .Where(p => p.ContractId == contractId && p.Type == PaymentType.Deposit && p.StatusCode == 1)
            .OrderBy(p => p.PaidAt)
            .ToListAsync(ct);

        var actorIds = deposits.Select(p => p.ActorId).Distinct().ToList();
        var names = await db.Users.AsNoTracking()
            .Where(u => actorIds.Contains(u.Id))
            .Select(u => new { u.Id, u.FullName })
            .ToDictionaryAsync(u => u.Id, u => u.FullName, ct);

        return deposits
            .Select(p => new DepositReceiptLine(p.Code, p.Amount, p.Method, p.PaidAt,
                names.TryGetValue(p.ActorId, out var name) ? name : "—"))
            .ToList();
    }
}

/// <summary>Một khoản đặt cọc trên biên nhận (FR-3.4).</summary>
public sealed record DepositReceiptLine(string Code, decimal Amount, string? Method, DateTime PaidAt, string ActorName);

public sealed record RequestedVehicle(int VehicleId, bool WithDriver);

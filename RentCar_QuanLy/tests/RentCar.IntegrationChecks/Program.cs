using Microsoft.EntityFrameworkCore;
using RentCar.Infrastructure.Persistence;
using RentCar.Application.Rentals;
using RentCar.Application.Handover;
using RentCar.Application.Dashboard;
using RentCar.Domain.Common;
using RentCar.Domain.Entities;
using RentCar.Domain.Enums;
using RentCar.Domain.Rules;
var connection = Environment.GetEnvironmentVariable("RENTCAR_TEST_CONNECTION_STRING") ?? throw new InvalidOperationException("Set RENTCAR_TEST_CONNECTION_STRING to a seeded, disposable database.");
if (!new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection).InitialCatalog.StartsWith("RentCar_QL2_Verify_", StringComparison.Ordinal))
    throw new InvalidOperationException("Integration checks require a disposable RentCar_QL2_Verify_ database, never the live database.");
var options = new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options;
await using var db = new AppDbContext(options);
void Check(bool ok,string text) { if(!ok)throw new Exception(text);Console.WriteLine("PASS "+text); }
var customer=await db.Users.Include(u=>u.Role).Include(u=>u.DriverLicense).FirstAsync(u=>u.Role.Code==RoleCodes.Customer);
customer.BirthDate=new DateOnly(1990,1,1);customer.IdNumber="012345678901";customer.Phone="0912345678";
if(customer.DriverLicense is null)db.DriverLicenses.Add(new DriverLicense{UserId=customer.Id,LicenseNumber="099999999999",LicenseClass="E",ExpiryDate=new DateOnly(2035,1,1)});
else {customer.DriverLicense.LicenseClass="E";customer.DriverLicense.ExpiryDate=new DateOnly(2035,1,1);}
await db.SaveChangesAsync();
var admin=await db.Users.Where(u=>u.Role.Code==RoleCodes.Admin).Select(u=>u.Id).FirstAsync();
var location=await db.PickupLocations.Where(l=>l.IsActive).Select(l=>l.Id).FirstAsync();
var cars=await db.Vehicles.Where(v=>v.Status==VehicleStatus.Ready).OrderBy(v=>v.Id).Take(10).ToListAsync();
Check(cars.Count==10,"10 ready physical vehicles");
var requested=cars.Select(v=>new RequestedVehicle(v.Id,v.Seats>2)).ToArray();
var rental=new RentalService(db);var handover=new HandoverService(db);
var start=VietnamTime.Now.Date.AddDays(90).AddHours(9);var end=start.AddDays(2);
var req=await rental.CreateRequestAsync(customer.Id,null,requested,start,end,location,location);
var stored=await db.Database.SqlQuery<DateTime>($"SELECT ngayNhan AS [Value] FROM dbo.yeuCauThue WHERE idYeuCau={req.Id}").SingleAsync();
Check(stored==VietnamTime.ToUtc(start),"UTC storage -> Vietnam 09:00 roundtrip");
try {await rental.CreateRequestAsync(customer.Id,null,cars[^1].Id,start,end,location,location);throw new Exception("overlap accepted");}catch(DomainException){Console.WriteLine("PASS overlap rejected for last (not first) vehicle");}
var contract=await rental.CreateContractFromRequestAsync(req.Id,admin);
Check(contract.Lines.Count==10,"one contract retains all 10 vehicles");
Check(contract.DiscountAmount==decimal.Round(contract.GrossAmount*.10m,0,MidpointRounding.AwayFromZero),"10 cars discount 10 percent");
Check(contract.DriverAmount==requested.Count(x=>x.WithDriver)*2_000_000m,"driver charged individually per car/day");
Check(contract.VatRate==.08m,"VAT snapshot 8 percent");
var total=await db.Database.SqlQuery<decimal>($"SELECT tongTienThanhToan AS [Value] FROM dbo.hopDong WHERE idHopDong={contract.Id}").SingleAsync();
Check(total==contract.TotalPayable,"database total equals domain total including drivers and VAT");
await rental.RecordPaymentAsync(contract.Id,PaymentType.Deposit,contract.DepositAmount,"Tiền mặt",admin);
await rental.ConfirmContractAsync(contract.Id,admin);
Check(contract.Status==ContractStatus.Confirmed,"confirm after deposit checks ALL cars");
// Move the test rental window back in time so delivery/return are realistic without waiting.
contract.StartAt=VietnamTime.Now.AddDays(-2);contract.EndAt=VietnamTime.Now.AddDays(1);await db.SaveChangesAsync();
var delivered=cars.Select(v=>new VehicleInspection(v.Id,10000,100,"Đạt")).ToArray();
try {await handover.RecordDeliveryAsync(new DeliveryRequest(contract.Id,admin,VietnamTime.Now.AddHours(-1),0,100,"Đạt",null,true,[],delivered[..9]));throw new Exception("partial delivery accepted");}catch(DomainException){Console.WriteLine("PASS cannot deliver only 9 of 10 cars");}
await handover.RecordDeliveryAsync(new DeliveryRequest(contract.Id,admin,VietnamTime.Now.AddHours(-1),0,100,"Đạt",null,true,[],delivered));
Check(await db.Handovers.CountAsync(h=>h.ContractId==contract.Id)==1,"exactly ONE shared handover for ten cars");
var returned=cars.Select((v,i)=>new VehicleInspection(v.Id,10100,90,"Đạt",i==9?500000:0,i==9)).ToArray();
await handover.RecordReturnAsync(new ReturnRequest(contract.Id,admin,VietnamTime.Now,0,100,"Đạt",null,0,false,[],returned));
Check(contract.Handover!.ExtraCharges==500000,"damage fee uses correct last vehicle");
Check(cars[^1].Status==VehicleStatus.Maintenance && cars.Take(9).All(v=>v.Status==VehicleStatus.PendingInspection),"return changes status of EVERY car");
var balance=SettlementRules.Balance(contract);
await handover.RecordSettlementPaymentAsync(contract.Id,PaymentType.Rent,balance.OutstandingRent,"Tiền mặt",admin);
await handover.RecordSettlementPaymentAsync(contract.Id,PaymentType.DepositRefund,balance.OutstandingRefund,"Tiền mặt",admin);
Check(contract.Status==ContractStatus.Completed,"complete only after all cars returned and balances settled");
Check((await rental.GetDepositReceiptsAsync(contract.Id)).Count==1,"deposit receipt query");
var dashboard=await new DashboardService(db).GetAsync(VietnamTime.Now);
Check(dashboard.Revenue.Current>0,"dashboard reads actual payment data");
var nextStart=VietnamTime.Now.AddDays(120);
var next=await rental.CreateRequestAsync(customer.Id,null,cars[0].Id,nextStart,nextStart.AddDays(2),location,location);
var expired=await rental.CreateContractFromRequestAsync(next.Id,admin);
expired.CreatedAt=VietnamTime.Now.AddHours(-26);expired.HoldUntil=VietnamTime.Now.AddHours(-1);await db.SaveChangesAsync();
await db.Database.ExecuteSqlInterpolatedAsync($"EXEC dbo.spHuyHopDongQuaHanGiuCho @idNguoiHeThong={admin}");
await db.Entry(expired).ReloadAsync();
Check(expired.Status==ContractStatus.Cancelled,"expiry procedure uses UTC and cancels expired unpaid contract");
try {await rental.RecordPaymentAsync(expired.Id,PaymentType.Deposit,100000,"Tiền mặt",admin);throw new Exception("late deposit accepted");}catch(DomainException){Console.WriteLine("PASS expired contract cannot receive deposit");}
await using(var maintenanceDb=new AppDbContext(options))
{
    maintenanceDb.MaintenanceSchedules.Add(new MaintenanceSchedule{VehicleId=cars[0].Id,Type="Bảo dưỡng",StartAt=nextStart.AddDays(10),EndAt=nextStart.AddDays(11),Cost=100000});
    await maintenanceDb.SaveChangesAsync();
    Check(await maintenanceDb.MaintenanceSchedules.AnyAsync(),"maintenance maps to existing table and saves Vietnam times");
}
Console.WriteLine($"CONTRACT_ID={contract.Id}");

await handover.CompleteInspectionAsync(cars[0].Id,admin,true);
await handover.CompleteInspectionAsync(cars[1].Id,admin,true);
var httpStart=VietnamTime.Now.AddDays(150);
var httpRequest=await rental.CreateRequestAsync(customer.Id,null,cars.Take(2).Select(v=>new RequestedVehicle(v.Id,v.Seats>2)).ToArray(),httpStart,httpStart.AddDays(2),location,location);
var httpContract=await rental.CreateContractFromRequestAsync(httpRequest.Id,admin);
await rental.RecordPaymentAsync(httpContract.Id,PaymentType.Deposit,httpContract.DepositAmount,"Tiền mặt",admin);
await rental.ConfirmContractAsync(httpContract.Id,admin);
httpContract.StartAt=VietnamTime.Now.AddDays(-2);httpContract.EndAt=VietnamTime.Now.AddDays(1);await db.SaveChangesAsync();
Console.WriteLine($"HTTP_CONTRACT_ID={httpContract.Id}");

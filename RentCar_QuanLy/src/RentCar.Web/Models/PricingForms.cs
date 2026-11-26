using System.ComponentModel.DataAnnotations;

namespace RentCar.Web.Models;

public sealed class PriceRuleForm
{
    public int Id { get; set; }
    [Range(1, int.MaxValue)] public int VehicleTypeId { get; set; }
    [Required] public DateTime StartAt { get; set; }
    [Required] public DateTime EndAt { get; set; }
    [Range(typeof(decimal), "0.01", "99.99")] public decimal Factor { get; set; } = 1m;
}

public sealed class PromotionForm
{
    [Required, StringLength(30)] public string Code { get; set; } = "";
    [Required, StringLength(150)] public string Name { get; set; } = "";
    public string? Description { get; set; }
    [Range(typeof(decimal), "0.01", "100")] public decimal DiscountPercent { get; set; }
    [Required] public DateTime StartAt { get; set; }
    [Required] public DateTime EndAt { get; set; }
}

public sealed class SurchargePolicyForm
{
    [Range(1, int.MaxValue)] public int VehicleTypeId { get; set; }
    [Range(0, 10080)] public int GraceMinutes { get; set; } = 120;
    [Range(0, 100000)] public int DailyKmLimit { get; set; } = 200;
    [Range(typeof(decimal), "0", "1000000000")] public decimal PricePerExtraKm { get; set; } = 5000m;
}

public sealed class PricingIndexVm
{
    public List<RentCar.Domain.Entities.PriceRule> PriceRules { get; set; } = new();
    public List<RentCar.Domain.Entities.Promotion> Promotions { get; set; } = new();
    public List<RentCar.Domain.Entities.SurchargePolicy> SurchargePolicies { get; set; } = new();
    public List<RentCar.Domain.Entities.VehicleType> VehicleTypes { get; set; } = new();
    public PriceRuleForm PriceForm { get; set; } = new();
    public PromotionForm PromotionForm { get; set; } = new();
    public SurchargePolicyForm SurchargeForm { get; set; } = new();
}

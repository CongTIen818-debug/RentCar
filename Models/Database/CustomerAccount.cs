namespace RentCar.Models.Database;

public class CustomerAccount
{
    public int Id { get; set; }
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public string PasswordHash { get; set; } = "";
    public bool IsActive { get; set; }
    public int RoleId { get; set; }
    public AccountRole Role { get; set; } = null!;
}

public class AccountRole
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
}

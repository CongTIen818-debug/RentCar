using System.ComponentModel.DataAnnotations;

namespace RentCar.Models;
public class BrandsForm
{
    [Display(Name = "Tên hãng")]
    [Required, StringLength(80)]
    public string Name { get; set; } = "";
}

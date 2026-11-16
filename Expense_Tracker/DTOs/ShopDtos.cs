using System.ComponentModel.DataAnnotations;

namespace Expense_Tracker.DTOs;

public class ShopDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Location { get; set; }
    public string? Contact { get; set; }
    public string? Notes { get; set; }
    public bool IsDemo { get; set; }
    public decimal TotalSpending { get; set; }
    public int ProductsCount { get; set; }
    public int LowestPriceMatchesCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateShopDto
{
    [Required(ErrorMessage = "Shop name is required")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Shop name must be between 2 and 150 characters")]
    public string Name { get; set; } = string.Empty;

    public string? Location { get; set; }
    public string? Contact { get; set; }
    public string? Notes { get; set; }
}

public class UpdateShopDto
{
    [Required(ErrorMessage = "Shop name is required")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Shop name must be between 2 and 150 characters")]
    public string Name { get; set; } = string.Empty;

    public string? Location { get; set; }
    public string? Contact { get; set; }
    public string? Notes { get; set; }
}

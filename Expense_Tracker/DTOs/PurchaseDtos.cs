using System.ComponentModel.DataAnnotations;

namespace Expense_Tracker.DTOs;

public class PurchaseDto
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "kg";
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }
    public int? ShopId { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public string? ShopLocation { get; set; }
    public string PaymentMethod { get; set; } = "UPI";
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public bool FromList { get; set; }
    public bool IsDemo { get; set; }
    public int? TransactionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreatePurchaseDto
{
    [Required(ErrorMessage = "Product name is required")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Product name must be between 2 and 150 characters")]
    public string ProductName { get; set; } = string.Empty;

    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }

    [Required(ErrorMessage = "Quantity is required")]
    [Range(0.001, 1000000, ErrorMessage = "Quantity must be greater than zero")]
    public decimal Quantity { get; set; }

    [Required(ErrorMessage = "Unit is required")]
    public string Unit { get; set; } = "kg";

    [Required(ErrorMessage = "Unit price is required")]
    [Range(0.01, 1000000, ErrorMessage = "Unit price must be greater than zero")]
    public decimal UnitPrice { get; set; }

    [Required(ErrorMessage = "Shop name is required")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Shop name must be between 2 and 150 characters")]
    public string ShopName { get; set; } = string.Empty;

    public string? ShopLocation { get; set; }

    [Required(ErrorMessage = "Payment method is required")]
    public string PaymentMethod { get; set; } = "UPI";

    [Required(ErrorMessage = "Purchase date is required")]
    public DateTime Date { get; set; }

    public string? Notes { get; set; }
    public bool FromList { get; set; }
    public bool IsDemo { get; set; }
}

public class UpdatePurchaseDto
{
    [Required(ErrorMessage = "Product name is required")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Product name must be between 2 and 150 characters")]
    public string ProductName { get; set; } = string.Empty;

    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }

    [Required(ErrorMessage = "Quantity is required")]
    [Range(0.001, 1000000, ErrorMessage = "Quantity must be greater than zero")]
    public decimal Quantity { get; set; }

    [Required(ErrorMessage = "Unit is required")]
    public string Unit { get; set; } = "kg";

    [Required(ErrorMessage = "Unit price is required")]
    [Range(0.01, 1000000, ErrorMessage = "Unit price must be greater than zero")]
    public decimal UnitPrice { get; set; }

    [Required(ErrorMessage = "Shop name is required")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Shop name must be between 2 and 150 characters")]
    public string ShopName { get; set; } = string.Empty;

    public string? ShopLocation { get; set; }

    [Required(ErrorMessage = "Payment method is required")]
    public string PaymentMethod { get; set; } = "UPI";

    [Required(ErrorMessage = "Purchase date is required")]
    public DateTime Date { get; set; }

    public string? Notes { get; set; }
}

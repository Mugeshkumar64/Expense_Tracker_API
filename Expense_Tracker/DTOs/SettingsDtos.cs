using System.ComponentModel.DataAnnotations;

namespace Expense_Tracker.DTOs;

public class UserSettingsDto
{
    public int Id { get; set; }
    public string Currency { get; set; } = "₹";
    public decimal MonthBudget { get; set; }
    public decimal ShopBudget { get; set; }
    public decimal ListBudget { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UpdateSettingsDto
{
    [Required]
    [StringLength(10)]
    public string Currency { get; set; } = "₹";

    [Range(0, 1000000000)]
    public decimal MonthBudget { get; set; }

    [Range(0, 1000000000)]
    public decimal ShopBudget { get; set; }

    [Range(0, 1000000000)]
    public decimal ListBudget { get; set; }
}

public class BackupDataDto
{
    public UserSettingsDto Settings { get; set; } = new();
    public List<CategoryDto> Categories { get; set; } = new();
    public List<ShopDto> Shops { get; set; } = new();
    public List<TransactionDto> Transactions { get; set; } = new();
    public List<PurchaseDto> Purchases { get; set; } = new();
    public List<ShoppingListItemDto> ShoppingList { get; set; } = new();
}

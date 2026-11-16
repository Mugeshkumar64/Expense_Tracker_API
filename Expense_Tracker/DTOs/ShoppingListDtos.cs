using System.ComponentModel.DataAnnotations;

namespace Expense_Tracker.DTOs;

public class ShoppingListItemDto
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "kg";
    public bool IsPurchased { get; set; }
    public DateTime CreatedAt { get; set; }

    // Calculated fields based on recorded prices
    public bool HasRecordedPrice { get; set; }
    public string? CheapestShop { get; set; }
    public decimal? CheapestPricePerBaseUnit { get; set; }
    public string? BaseUnit { get; set; }
    public decimal? EstimatedCost { get; set; }
    public decimal? EstimatedSavingsVsUsual { get; set; }
}

public class CreateShoppingListItemDto
{
    [Required(ErrorMessage = "Product name is required")]
    [StringLength(150, MinimumLength = 2, ErrorMessage = "Product name must be between 2 and 150 characters")]
    public string ProductName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Quantity is required")]
    [Range(0.001, 1000000, ErrorMessage = "Quantity must be greater than zero")]
    public decimal Quantity { get; set; } = 1;

    [Required(ErrorMessage = "Unit is required")]
    public string Unit { get; set; } = "kg";
}

public class ShoppingListSummaryDto
{
    public List<ShoppingListItemDto> Items { get; set; } = new();
    public decimal TotalEstimatedCost { get; set; }
    public decimal EstimatedSavingsVsUsual { get; set; }
    public int TotalItemsCount { get; set; }
    public int PricedItemsCount { get; set; }
    public int UnpricedItemsCount { get; set; }
    public decimal TargetBudget { get; set; }
    public decimal BudgetDifference { get; set; }
    public bool IsWithinBudget { get; set; }
}

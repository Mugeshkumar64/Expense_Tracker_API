namespace Expense_Tracker.DTOs;

public class SavingsSummaryDto
{
    public decimal ActualSavings { get; set; }
    public decimal PotentialSavings { get; set; }
    public decimal ShoppingListPlanningSavings { get; set; }
    public decimal ActualBoughtViaListSavings { get; set; }

    public List<ActualSavingsLogDto> ActualSavingsLog { get; set; } = new();
    public List<MonthlySavingsTrendDto> MonthlyActualSavings { get; set; } = new();
    public List<PotentialSavingsItemDto> HighestPotentialSavings { get; set; } = new();
}

public class ActualSavingsLogDto
{
    public string ProductName { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public decimal SavedAmount { get; set; }
    public decimal PaidPricePerUnit { get; set; }
    public decimal UsualPricePerUnit { get; set; }
    public string UsualShopName { get; set; } = string.Empty;
    public DateTime UsualPurchaseDate { get; set; }
    public DateTime PurchaseDate { get; set; }
    public string BaseUnit { get; set; } = "kg";
}

public class MonthlySavingsTrendDto
{
    public string MonthKey { get; set; } = string.Empty; // "yyyy-MM"
    public string MonthName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
}

public class PotentialSavingsItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public decimal LastPaidPrice { get; set; }
    public string LastShop { get; set; } = string.Empty;
    public decimal LowestPrice { get; set; }
    public string LowestShop { get; set; } = string.Empty;
    public decimal EstimatedSaving { get; set; }
    public decimal LastQuantity { get; set; }
    public string BaseUnit { get; set; } = "kg";
}

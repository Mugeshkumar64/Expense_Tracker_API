namespace Expense_Tracker.DTOs;

public class PriceComparisonResultDto
{
    public string ProductName { get; set; } = string.Empty;
    public string BaseUnit { get; set; } = "kg";
    public decimal PlannedQuantity { get; set; }

    public decimal LowestRecordedPrice { get; set; }
    public DateTime LowestRecordedDate { get; set; }
    public string CheapestShop { get; set; } = string.Empty;

    public decimal LastPurchasePrice { get; set; }
    public string LastPurchaseShop { get; set; } = string.Empty;
    public DateTime LastPurchaseDate { get; set; }

    public decimal SavingVsLastPurchasePerUnit { get; set; }
    public decimal TotalSavingVsLastPurchase { get; set; }

    public string SuggestionText { get; set; } = string.Empty;
    public bool IsOutdated { get; set; }
    public DateTime LatestRecordedDate { get; set; }

    public List<ShopComparisonRowDto> ShopRows { get; set; } = new();
    public List<ProductPriceHistoryRecordDto> PriceHistory { get; set; } = new();
}

public class ShopComparisonRowDto
{
    public string ShopName { get; set; } = string.Empty;
    public decimal PricePerBaseUnit { get; set; }
    public decimal EstimatedTotal { get; set; }
    public DateTime LastRecordedDate { get; set; }
    public bool IsLowestPrice { get; set; }
    public decimal PriceDifferencePerUnit { get; set; }
    public bool IsPotentiallyOutdated { get; set; }
}

public class ProductPriceHistoryRecordDto
{
    public DateTime Date { get; set; }
    public string ShopName { get; set; } = string.Empty;
    public decimal NormalizedPrice { get; set; }
    public string BaseUnit { get; set; } = "kg";
    public decimal PercentageOfMax { get; set; }
}

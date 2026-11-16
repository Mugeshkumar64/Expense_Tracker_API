namespace Expense_Tracker.DTOs;

public class MonthlyReportDto
{
    public string Month { get; set; } = string.Empty; // "yyyy-MM"
    public string MonthName { get; set; } = string.Empty; // "October 2026"
    public decimal TotalInflow { get; set; }
    public decimal TotalOutflow { get; set; }
    public decimal NetPosition { get; set; }
    public int SavingsRate { get; set; }

    public List<CategoryExpenseAllocationDto> CategoryExpenses { get; set; } = new();

    public string? TopOutflowCategory { get; set; }
    public decimal TopOutflowCategoryAmount { get; set; }

    public string? PeakSingleOutflowTitle { get; set; }
    public decimal PeakSingleOutflowAmount { get; set; }

    public decimal AverageOutflowTicket { get; set; }
}

public class ProductPriceReportDto
{
    public string ProductName { get; set; } = string.Empty;
    public string BaseUnit { get; set; } = "kg";
    public int ShopsCount { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal LowestPrice { get; set; }
    public string LowestShop { get; set; } = string.Empty;
    public decimal LastPaidPrice { get; set; }
    public string LastShop { get; set; } = string.Empty;
    public decimal? GapEstimate { get; set; }
}

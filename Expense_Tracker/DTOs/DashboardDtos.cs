namespace Expense_Tracker.DTOs;

public class DashboardSummaryDto
{
    public decimal TotalNetBalance { get; set; }
    public decimal IncomeThisMonth { get; set; }
    public decimal ExpenseThisMonth { get; set; }
    public decimal NetSavingsThisMonth { get; set; }
    public int SavingsPercentage { get; set; }

    public decimal ShoppingBudgetLeft { get; set; }
    public decimal ShoppingSpentThisMonth { get; set; }
    public decimal ShoppingBudget { get; set; }
    public bool HasShoppingBudget { get; set; }

    public decimal MonthlyBudget { get; set; }
    public int MonthlyBudgetUsedPercentage { get; set; }
    public bool HasMonthlyBudget { get; set; }

    public decimal PotentialSavings { get; set; }
    public decimal ActualSavings { get; set; }

    public List<MonthlyCashFlowDto> MonthlyCashFlow { get; set; } = new();
    public List<CategoryExpenseAllocationDto> ExpenseAllocation { get; set; } = new();
    public List<TransactionDto> RecentTransactions { get; set; } = new();
    public List<string> SmartInsights { get; set; } = new();
    public List<PriceMoverDto> PriceMovers { get; set; } = new();
    public List<CheapestShopDto> CheapestShops { get; set; } = new();
}

public class MonthlyCashFlowDto
{
    public string MonthKey { get; set; } = string.Empty; // "yyyy-MM"
    public string MonthLabel { get; set; } = string.Empty; // "Jan", "Feb", etc.
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
}

public class CategoryExpenseAllocationDto
{
    public string CategoryName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal Percentage { get; set; }
}

public class PriceMoverDto
{
    public string ProductName { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public string BaseUnit { get; set; } = "kg";
    public decimal PreviousPrice { get; set; }
    public decimal LatestPrice { get; set; }
    public decimal PercentageChange { get; set; }
    public bool IsIncrease { get; set; }
}

public class CheapestShopDto
{
    public string ProductName { get; set; } = string.Empty;
    public string ShopName { get; set; } = string.Empty;
    public string BaseUnit { get; set; } = "kg";
    public decimal LowestPrice { get; set; }
}

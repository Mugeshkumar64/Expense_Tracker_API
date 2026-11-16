using System.Globalization;
using Expense_Tracker.Common;
using Expense_Tracker.Data;
using Expense_Tracker.DTOs;
using Expense_Tracker.DTOs.Common;
using Expense_Tracker.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Expense_Tracker.Services;

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;
    private readonly ISavingsService _savingsService;
    private readonly IShoppingListService _shoppingListService;

    public DashboardService(ApplicationDbContext context, ISavingsService savingsService, IShoppingListService shoppingListService)
    {
        _context = context;
        _savingsService = savingsService;
        _shoppingListService = shoppingListService;
    }

    public async Task<ApiResponse<DashboardSummaryDto>> GetDashboardSummaryAsync()
    {
        var now = DateTime.UtcNow;
        var currentMonthKey = now.ToString("yyyy-MM");
        var prevMonthKey = now.AddMonths(-1).ToString("yyyy-MM");

        var allTransactions = await _context.Transactions
            .Include(t => t.Category)
            .AsNoTracking()
            .ToListAsync();

        var allPurchases = await _context.Purchases
            .AsNoTracking()
            .ToListAsync();

        var settings = await _context.UserSettings.FirstOrDefaultAsync() ?? new Models.UserSettings();

        var savingsRes = await _savingsService.GetSavingsSummaryAsync();
        var savingsData = savingsRes.Data ?? new SavingsSummaryDto();

        var listSummaryRes = await _shoppingListService.GetShoppingListSummaryAsync();
        var listEstimatedSavings = listSummaryRes.Data?.EstimatedSavingsVsUsual ?? 0;

        // Current month transactions
        var currentMonthTx = allTransactions
            .Where(t => t.Date.ToString("yyyy-MM") == currentMonthKey)
            .ToList();

        var incomeThisMonth = currentMonthTx.Where(t => t.Type == "income").Sum(t => t.Amount);
        var expenseThisMonth = currentMonthTx.Where(t => t.Type == "expense").Sum(t => t.Amount);
        var netSavingsThisMonth = incomeThisMonth - expenseThisMonth;
        var savingsPct = incomeThisMonth > 0 ? (int)Math.Round((netSavingsThisMonth / incomeThisMonth) * 100) : 0;

        // Total Net Balance across all time
        var totalIncome = allTransactions.Where(t => t.Type == "income").Sum(t => t.Amount);
        var totalExpense = allTransactions.Where(t => t.Type == "expense").Sum(t => t.Amount);
        var totalBalance = totalIncome - totalExpense;

        // Shopping budget
        var shoppingSpentThisMonth = allPurchases
            .Where(p => p.Date.ToString("yyyy-MM") == currentMonthKey)
            .Sum(p => p.TotalPrice);

        var shoppingBudgetLeft = settings.ShopBudget > 0 ? settings.ShopBudget - shoppingSpentThisMonth : 0;
        var monthlyBudgetUsedPct = settings.MonthBudget > 0 ? (int)Math.Round((expenseThisMonth / settings.MonthBudget) * 100) : 0;

        // Monthly cash flow analytics for last 6 months
        var cashFlowList = new List<MonthlyCashFlowDto>();
        for (int i = 5; i >= 0; i--)
        {
            var d = now.AddMonths(-i);
            var mKey = d.ToString("yyyy-MM");
            var mLabel = d.ToString("MMM", CultureInfo.InvariantCulture);

            var monthTxs = allTransactions.Where(t => t.Date.ToString("yyyy-MM") == mKey).ToList();
            var mInc = monthTxs.Where(t => t.Type == "income").Sum(t => t.Amount);
            var mExp = monthTxs.Where(t => t.Type == "expense").Sum(t => t.Amount);

            cashFlowList.Add(new MonthlyCashFlowDto
            {
                MonthKey = mKey,
                MonthLabel = mLabel,
                Income = mInc,
                Expense = mExp
            });
        }

        // Category expense allocation for current month
        var catExpenses = currentMonthTx
            .Where(t => t.Type == "expense")
            .GroupBy(t => t.Category != null ? t.Category.Name : "Other")
            .Select(g => new
            {
                Category = g.Key,
                Amount = g.Sum(t => t.Amount)
            })
            .OrderByDescending(x => x.Amount)
            .ToList();

        var expenseAllocation = catExpenses.Select(c => new CategoryExpenseAllocationDto
        {
            CategoryName = c.Category,
            Amount = c.Amount,
            Percentage = expenseThisMonth > 0 ? Math.Round((c.Amount / expenseThisMonth) * 100, 1) : 0
        }).ToList();

        // Recent 6 transactions
        var recentTxs = allTransactions
            .OrderByDescending(t => t.Date)
            .ThenByDescending(t => t.CreatedAt)
            .Take(6)
            .Select(t => new TransactionDto
            {
                Id = t.Id,
                Title = t.Title,
                Amount = t.Amount,
                Type = t.Type,
                CategoryId = t.CategoryId,
                CategoryName = t.Category != null ? t.Category.Name : "Other",
                Date = t.Date,
                PaymentMethod = t.PaymentMethod,
                Notes = t.Notes,
                Shop = t.Shop,
                PurchaseId = t.PurchaseId,
                IsDemo = t.IsDemo,
                CreatedAt = t.CreatedAt
            })
            .ToList();

        // Product Groups for Smart Insights, Price Movers, and Cheapest Shops
        var productGroups = allPurchases
            .GroupBy(p =>
            {
                var (baseUnit, _) = UnitConverter.GetUnitInfo(p.Unit);
                return (Name: p.ProductName.Trim().ToLowerInvariant(), BaseUnit: baseUnit);
            })
            .Select(g =>
            {
                var recs = g.Select(p => new
                {
                    Purchase = p,
                    NormalizedPrice = UnitConverter.NormalizePrice(p.UnitPrice, p.Unit),
                    NormalizedQty = UnitConverter.NormalizeQuantity(p.Quantity, p.Unit)
                })
                .OrderBy(r => r.Purchase.Date)
                .ThenBy(r => r.Purchase.CreatedAt)
                .ToList();

                var shopGroups = recs
                    .GroupBy(r => r.Purchase.ShopName)
                    .Select(sg => new
                    {
                        Shop = sg.Key,
                        Count = sg.Count(),
                        Price = sg.Last().NormalizedPrice,
                        Date = sg.Last().Purchase.Date,
                        Records = sg.ToList()
                    })
                    .OrderBy(x => x.Price)
                    .ToList();

                var low = shopGroups.FirstOrDefault();
                var high = shopGroups.LastOrDefault();
                var last = recs.LastOrDefault();

                return new
                {
                    ProductName = g.First().ProductName,
                    BaseUnit = g.Key.BaseUnit,
                    Recs = recs,
                    ShopGroups = shopGroups,
                    Low = low,
                    High = high,
                    LastRecord = last
                };
            })
            .OrderByDescending(g => g.Recs.Count)
            .ToList();

        // Smart Insights
        var insights = new List<string>();
        var currentMonthPurchases = allPurchases.Where(p => p.Date.ToString("yyyy-MM") == currentMonthKey).ToList();
        if (currentMonthPurchases.Any())
        {
            insights.Add($"You spent {settings.Currency}{shoppingSpentThisMonth:0.00} on {currentMonthPurchases.Count} shopping purchase(s) this month.");
        }

        foreach (var g in productGroups.Where(g => g.ShopGroups.Count > 1 && g.High != null && g.Low != null).Take(2))
        {
            var diff = g.High!.Price - g.Low!.Price;
            insights.Add($"The recorded price of {g.ProductName} is {settings.Currency}{diff:0.00}/{g.BaseUnit} lower at {g.Low.Shop} than {g.High.Shop}.");
        }

        if (listEstimatedSavings > 0)
        {
            insights.Add($"You could potentially save {settings.Currency}{listEstimatedSavings:0.00} on your current shopping list (estimate).");
        }

        // Compare category spending between current and previous month
        var prevMonthCatExpenses = allTransactions
            .Where(t => t.Type == "expense" && t.Date.ToString("yyyy-MM") == prevMonthKey)
            .GroupBy(t => t.Category != null ? t.Category.Name : "Other")
            .ToDictionary(g => g.Key, g => g.Sum(t => t.Amount));

        foreach (var c in catExpenses)
        {
            if (prevMonthCatExpenses.TryGetValue(c.Category, out var prevAmt) && prevAmt > 0 && c.Amount > prevAmt)
            {
                var incPct = Math.Round(((c.Amount / prevAmt) - 1) * 100);
                insights.Add($"Your {c.Category} expenses increased by {incPct}% compared with last month.");
                break;
            }
        }

        // Price Movers: repeat purchases at same shop with price changes
        var movers = new List<PriceMoverDto>();
        foreach (var g in productGroups)
        {
            if (g.LastRecord == null) continue;
            var sameShopEarlier = g.Recs
                .Take(g.Recs.Count - 1)
                .Where(r => r.Purchase.ShopName == g.LastRecord.Purchase.ShopName)
                .LastOrDefault();

            if (sameShopEarlier != null && sameShopEarlier.NormalizedPrice != g.LastRecord.NormalizedPrice)
            {
                var diff = g.LastRecord.NormalizedPrice - sameShopEarlier.NormalizedPrice;
                var pct = Math.Round(((g.LastRecord.NormalizedPrice / sameShopEarlier.NormalizedPrice) - 1) * 100, 1);
                movers.Add(new PriceMoverDto
                {
                    ProductName = g.ProductName,
                    ShopName = g.LastRecord.Purchase.ShopName,
                    BaseUnit = g.BaseUnit,
                    PreviousPrice = sameShopEarlier.NormalizedPrice,
                    LatestPrice = g.LastRecord.NormalizedPrice,
                    PercentageChange = Math.Abs(pct),
                    IsIncrease = pct > 0
                });
            }
        }

        // Cheapest shops for frequent products
        var cheapestShops = productGroups
            .Where(g => g.ShopGroups.Count > 1 && g.Low != null)
            .Take(4)
            .Select(g => new CheapestShopDto
            {
                ProductName = g.ProductName,
                ShopName = g.Low!.Shop,
                BaseUnit = g.BaseUnit,
                LowestPrice = g.Low.Price
            })
            .ToList();

        var summary = new DashboardSummaryDto
        {
            TotalNetBalance = totalBalance,
            IncomeThisMonth = incomeThisMonth,
            ExpenseThisMonth = expenseThisMonth,
            NetSavingsThisMonth = netSavingsThisMonth,
            SavingsPercentage = savingsPct,
            ShoppingBudgetLeft = shoppingBudgetLeft,
            ShoppingSpentThisMonth = shoppingSpentThisMonth,
            ShoppingBudget = settings.ShopBudget,
            HasShoppingBudget = settings.ShopBudget > 0,
            MonthlyBudget = settings.MonthBudget,
            MonthlyBudgetUsedPercentage = monthlyBudgetUsedPct,
            HasMonthlyBudget = settings.MonthBudget > 0,
            PotentialSavings = savingsData.PotentialSavings,
            ActualSavings = savingsData.ActualSavings,
            MonthlyCashFlow = cashFlowList,
            ExpenseAllocation = expenseAllocation,
            RecentTransactions = recentTxs,
            SmartInsights = insights,
            PriceMovers = movers.OrderByDescending(m => m.PercentageChange).Take(4).ToList(),
            CheapestShops = cheapestShops
        };

        return ApiResponse<DashboardSummaryDto>.Ok(summary);
    }
}

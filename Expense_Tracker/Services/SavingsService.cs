using System.Globalization;
using Expense_Tracker.Common;
using Expense_Tracker.Data;
using Expense_Tracker.DTOs;
using Expense_Tracker.DTOs.Common;
using Expense_Tracker.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Expense_Tracker.Services;

public class SavingsService : ISavingsService
{
    private readonly ApplicationDbContext _context;
    private readonly IShoppingListService _shoppingListService;

    public SavingsService(ApplicationDbContext context, IShoppingListService shoppingListService)
    {
        _context = context;
        _shoppingListService = shoppingListService;
    }

    public async Task<ApiResponse<SavingsSummaryDto>> GetSavingsSummaryAsync()
    {
        var purchases = await _context.Purchases
            .AsNoTracking()
            .ToListAsync();

        var listSummaryRes = await _shoppingListService.GetShoppingListSummaryAsync();
        var shoppingListSavings = listSummaryRes.Data?.EstimatedSavingsVsUsual ?? 0;

        var productGroups = purchases
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
                        Date = sg.Last().Purchase.Date
                    })
                    .OrderBy(x => x.Price)
                    .ToList();

                var low = shopGroups.FirstOrDefault();
                var usual = shopGroups.OrderByDescending(x => x.Count).ThenByDescending(x => x.Date).FirstOrDefault();
                var last = recs.LastOrDefault();

                return new
                {
                    ProductName = g.First().ProductName,
                    BaseUnit = g.Key.BaseUnit,
                    Recs = recs,
                    ShopGroups = shopGroups,
                    Low = low,
                    Usual = usual,
                    LastRecord = last
                };
            })
            .ToList();

        var actualSavingsLogs = new List<ActualSavingsLogDto>();
        var monthlySavings = new Dictionary<string, decimal>();
        decimal actualBoughtViaList = 0;

        foreach (var g in productGroups)
        {
            if (g.ShopGroups.Count < 2 || g.Usual == null) continue;

            for (int i = 0; i < g.Recs.Count; i++)
            {
                var current = g.Recs[i];
                if (current.Purchase.ShopName.Equals(g.Usual.Shop, StringComparison.OrdinalIgnoreCase)) continue;

                // Find latest earlier purchase at usual shop
                var prevUsual = g.Recs
                    .Take(i)
                    .Where(r => r.Purchase.ShopName.Equals(g.Usual.Shop, StringComparison.OrdinalIgnoreCase))
                    .LastOrDefault();

                if (prevUsual == null) continue;

                var savingPerUnit = prevUsual.NormalizedPrice - current.NormalizedPrice;
                var totalSaving = savingPerUnit * current.NormalizedQty;

                if (totalSaving > 0)
                {
                    var roundedSaving = Math.Round(totalSaving, 2);
                    actualSavingsLogs.Add(new ActualSavingsLogDto
                    {
                        ProductName = g.ProductName,
                        ShopName = current.Purchase.ShopName,
                        SavedAmount = roundedSaving,
                        PaidPricePerUnit = current.NormalizedPrice,
                        UsualPricePerUnit = prevUsual.NormalizedPrice,
                        UsualShopName = prevUsual.Purchase.ShopName,
                        UsualPurchaseDate = prevUsual.Purchase.Date,
                        PurchaseDate = current.Purchase.Date,
                        BaseUnit = g.BaseUnit
                    });

                    var monthKey = current.Purchase.Date.ToString("yyyy-MM");
                    monthlySavings[monthKey] = (monthlySavings.TryGetValue(monthKey, out var val) ? val : 0) + roundedSaving;

                    if (current.Purchase.FromList)
                    {
                        actualBoughtViaList += roundedSaving;
                    }
                }
            }
        }

        var totalActualSavings = actualSavingsLogs.Sum(x => x.SavedAmount);

        // Potential savings: last purchase vs lowest recorded
        var potentialSavingsItems = productGroups
            .Where(g => g.ShopGroups.Count > 1 && g.LastRecord != null && g.Low != null && g.LastRecord.NormalizedPrice > g.Low.Price)
            .Select(g =>
            {
                var diff = g.LastRecord!.NormalizedPrice - g.Low!.Price;
                var totalPotential = Math.Round(diff * g.LastRecord.NormalizedQty, 2);

                return new PotentialSavingsItemDto
                {
                    ProductName = g.ProductName,
                    LastPaidPrice = g.LastRecord.NormalizedPrice,
                    LastShop = g.LastRecord.Purchase.ShopName,
                    LowestPrice = g.Low.Price,
                    LowestShop = g.Low.Shop,
                    EstimatedSaving = totalPotential,
                    LastQuantity = g.LastRecord.NormalizedQty,
                    BaseUnit = g.BaseUnit
                };
            })
            .OrderByDescending(x => x.EstimatedSaving)
            .ToList();

        var totalPotentialSavings = potentialSavingsItems.Sum(x => x.EstimatedSaving);

        // Monthly trend list
        var monthlyTrend = monthlySavings
            .OrderBy(kv => kv.Key)
            .Select(kv =>
            {
                var dt = DateTime.ParseExact(kv.Key + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);
                var pct = totalActualSavings > 0 ? Math.Round((kv.Value / totalActualSavings) * 100, 1) : 0;
                return new MonthlySavingsTrendDto
                {
                    MonthKey = kv.Key,
                    MonthName = dt.ToString("MMM yyyy"),
                    Amount = kv.Value,
                    Percentage = pct
                };
            })
            .ToList();

        var result = new SavingsSummaryDto
        {
            ActualSavings = totalActualSavings,
            PotentialSavings = totalPotentialSavings,
            ShoppingListPlanningSavings = shoppingListSavings,
            ActualBoughtViaListSavings = actualBoughtViaList,
            ActualSavingsLog = actualSavingsLogs.OrderByDescending(x => x.PurchaseDate).Take(20).ToList(),
            MonthlyActualSavings = monthlyTrend,
            HighestPotentialSavings = potentialSavingsItems.Take(10).ToList()
        };

        return ApiResponse<SavingsSummaryDto>.Ok(result);
    }
}

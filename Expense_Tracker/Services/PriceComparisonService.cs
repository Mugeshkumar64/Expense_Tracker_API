using Expense_Tracker.Common;
using Expense_Tracker.Data;
using Expense_Tracker.DTOs;
using Expense_Tracker.DTOs.Common;
using Expense_Tracker.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Expense_Tracker.Services;

public class PriceComparisonService : IPriceComparisonService
{
    private const int OutdatedThresholdDays = 60;
    private readonly ApplicationDbContext _context;

    public PriceComparisonService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<PriceComparisonResultDto>> CompareProductPricesAsync(string productName, decimal plannedQuantity = 1)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return ApiResponse<PriceComparisonResultDto>.Fail("Product name is required.");
        }

        var search = productName.Trim().ToLowerInvariant();
        var purchases = await _context.Purchases
            .AsNoTracking()
            .ToListAsync();

        if (!purchases.Any())
        {
            return ApiResponse<PriceComparisonResultDto>.Fail("No purchases recorded yet. Comparisons require recorded purchase prices.");
        }

        // Find matching purchases
        var matching = purchases
            .Where(p => p.ProductName.Trim().ToLowerInvariant() == search)
            .ToList();

        if (!matching.Any())
        {
            matching = purchases
                .Where(p => p.ProductName.Trim().ToLowerInvariant().Contains(search))
                .ToList();
        }

        if (!matching.Any())
        {
            return ApiResponse<PriceComparisonResultDto>.Fail($"No recorded purchases found matching '{productName}'.");
        }

        // Determine dominant base unit
        var primaryMatch = matching.First();
        var (baseUnit, _) = UnitConverter.GetUnitInfo(primaryMatch.Unit);

        var records = matching
            .Select(p =>
            {
                var normPrice = UnitConverter.NormalizePrice(p.UnitPrice, p.Unit);
                var normQty = UnitConverter.NormalizeQuantity(p.Quantity, p.Unit);
                return new
                {
                    Purchase = p,
                    NormalizedPrice = normPrice,
                    NormalizedQty = normQty
                };
            })
            .OrderBy(r => r.Purchase.Date)
            .ThenBy(r => r.Purchase.CreatedAt)
            .ToList();

        // Group by shop
        var shopGroups = records
            .GroupBy(r => r.Purchase.ShopName)
            .Select(sg =>
            {
                var lastRec = sg.Last();
                return new
                {
                    ShopName = sg.Key,
                    Count = sg.Count(),
                    LatestRecord = lastRec,
                    Price = lastRec.NormalizedPrice,
                    Date = lastRec.Purchase.Date
                };
            })
            .OrderBy(sg => sg.Price)
            .ToList();

        var lowShop = shopGroups.First();
        var highShop = shopGroups.Last();
        var lastPurchase = records.Last();

        var qty = plannedQuantity > 0 ? plannedQuantity : 1m;
        var diffHighLow = highShop.Price - lowShop.Price;
        var diffLastLow = Math.Max(0, lastPurchase.NormalizedPrice - lowShop.Price);

        var latestDate = shopGroups.Max(sg => sg.Date);
        var isOutdated = (DateTime.UtcNow.Date - latestDate.Date).TotalDays > OutdatedThresholdDays;

        var suggestion = $"Buy {primaryMatch.ProductName} at {lowShop.ShopName}. Based on your recorded prices, you can save {diffHighLow:0.00} per {baseUnit} compared with {highShop.ShopName}" +
                         (qty > 0 ? $" — {(diffHighLow * qty):0.00} on {qty:0.###} {baseUnit}." : ".");

        if (diffLastLow > 0)
        {
            suggestion += $" Compared with your last purchase, you could save {diffLastLow:0.00} per {baseUnit}" +
                          (qty > 0 ? $" ({(diffLastLow * qty):0.00} on {qty:0.###} {baseUnit})." : ".");
        }
        else
        {
            suggestion += " Your last purchase was already at the lowest recorded price.";
        }

        var shopRows = shopGroups.Select(sg =>
        {
            var isOld = (DateTime.UtcNow.Date - sg.Date.Date).TotalDays > OutdatedThresholdDays;
            var isLowest = sg.ShopName == lowShop.ShopName;
            var diff = sg.Price - lowShop.Price;

            return new ShopComparisonRowDto
            {
                ShopName = sg.ShopName,
                PricePerBaseUnit = sg.Price,
                EstimatedTotal = Math.Round(sg.Price * qty, 2),
                LastRecordedDate = sg.Date,
                IsLowestPrice = isLowest,
                PriceDifferencePerUnit = diff,
                IsPotentiallyOutdated = isOld
            };
        }).ToList();

        var latest8 = records.TakeLast(8).ToList();
        var maxPrice = latest8.Any() ? latest8.Max(r => r.NormalizedPrice) : 1m;
        if (maxPrice <= 0) maxPrice = 1m;

        var historyList = latest8.Select(r => new ProductPriceHistoryRecordDto
        {
            Date = r.Purchase.Date,
            ShopName = r.Purchase.ShopName,
            NormalizedPrice = r.NormalizedPrice,
            BaseUnit = baseUnit,
            PercentageOfMax = Math.Round((r.NormalizedPrice / maxPrice) * 100, 1)
        }).ToList();

        var result = new PriceComparisonResultDto
        {
            ProductName = primaryMatch.ProductName,
            BaseUnit = baseUnit,
            PlannedQuantity = qty,
            LowestRecordedPrice = lowShop.Price,
            LowestRecordedDate = lowShop.Date,
            CheapestShop = lowShop.ShopName,
            LastPurchasePrice = lastPurchase.NormalizedPrice,
            LastPurchaseShop = lastPurchase.Purchase.ShopName,
            LastPurchaseDate = lastPurchase.Purchase.Date,
            SavingVsLastPurchasePerUnit = diffLastLow,
            TotalSavingVsLastPurchase = Math.Round(diffLastLow * qty, 2),
            SuggestionText = suggestion,
            IsOutdated = isOutdated,
            LatestRecordedDate = latestDate,
            ShopRows = shopRows,
            PriceHistory = historyList
        };

        return ApiResponse<PriceComparisonResultDto>.Ok(result);
    }
}

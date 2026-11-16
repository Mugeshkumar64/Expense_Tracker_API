using Expense_Tracker.Common;
using Expense_Tracker.Data;
using Expense_Tracker.DTOs;
using Expense_Tracker.DTOs.Common;
using Expense_Tracker.Interfaces;
using Expense_Tracker.Models;
using Microsoft.EntityFrameworkCore;

namespace Expense_Tracker.Services;

public class ShoppingListService : IShoppingListService
{
    private readonly ApplicationDbContext _context;
    private readonly IPurchaseService _purchaseService;

    public ShoppingListService(ApplicationDbContext context, IPurchaseService purchaseService)
    {
        _context = context;
        _purchaseService = purchaseService;
    }

    public async Task<ApiResponse<ShoppingListSummaryDto>> GetShoppingListSummaryAsync()
    {
        var items = await _context.ShoppingListItems
            .OrderBy(i => i.CreatedAt)
            .ToListAsync();

        var purchases = await _context.Purchases
            .AsNoTracking()
            .ToListAsync();

        var settings = await _context.UserSettings.FirstOrDefaultAsync() ?? new UserSettings();

        // Group purchases by product and base unit
        var productGroups = purchases
            .GroupBy(p =>
            {
                var (baseUnit, _) = UnitConverter.GetUnitInfo(p.Unit);
                return (Name: p.ProductName.Trim().ToLowerInvariant(), BaseUnit: baseUnit);
            })
            .ToDictionary(
                g => g.Key,
                g =>
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

                    // Group by shop to find cheapest and usual shop
                    var shopGroups = recs
                        .GroupBy(r => r.Purchase.ShopName)
                        .Select(sg => new
                        {
                            Shop = sg.Key,
                            Count = sg.Count(),
                            LatestRec = sg.Last(),
                            Price = sg.Last().NormalizedPrice
                        })
                        .OrderBy(x => x.Price)
                        .ToList();

                    var low = shopGroups.FirstOrDefault();
                    var usual = shopGroups.OrderByDescending(x => x.Count).ThenByDescending(x => x.LatestRec.Purchase.Date).FirstOrDefault();

                    return new { Low = low, Usual = usual, BaseUnit = g.Key.BaseUnit };
                });

        decimal totalEstimatedCost = 0;
        decimal totalEstimatedSavings = 0;
        int pricedCount = 0;

        var itemDtos = new List<ShoppingListItemDto>();

        foreach (var item in items)
        {
            var (baseUnit, factor) = UnitConverter.GetUnitInfo(item.Unit);
            var key = (Name: item.ProductName.Trim().ToLowerInvariant(), BaseUnit: baseUnit);

            var dto = new ShoppingListItemDto
            {
                Id = item.Id,
                ProductName = item.ProductName,
                Quantity = item.Quantity,
                Unit = item.Unit,
                IsPurchased = item.IsPurchased,
                CreatedAt = item.CreatedAt,
                BaseUnit = baseUnit
            };

            if (productGroups.TryGetValue(key, out var grp) && grp.Low != null)
            {
                var normalizedQty = factor > 0 ? item.Quantity / factor : item.Quantity;
                var cost = Math.Round(grp.Low.Price * normalizedQty, 2);
                var savings = grp.Usual != null ? Math.Max(0, Math.Round((grp.Usual.Price - grp.Low.Price) * normalizedQty, 2)) : 0;

                dto.HasRecordedPrice = true;
                dto.CheapestShop = grp.Low.Shop;
                dto.CheapestPricePerBaseUnit = grp.Low.Price;
                dto.EstimatedCost = cost;
                dto.EstimatedSavingsVsUsual = savings;

                totalEstimatedCost += cost;
                totalEstimatedSavings += savings;
                pricedCount++;
            }
            else
            {
                dto.HasRecordedPrice = false;
            }

            itemDtos.Add(dto);
        }

        var targetBudget = settings.ListBudget;
        var diff = targetBudget - totalEstimatedCost;
        var isWithinBudget = targetBudget <= 0 || diff >= 0;

        var summary = new ShoppingListSummaryDto
        {
            Items = itemDtos,
            TotalEstimatedCost = totalEstimatedCost,
            EstimatedSavingsVsUsual = totalEstimatedSavings,
            TotalItemsCount = items.Count,
            PricedItemsCount = pricedCount,
            UnpricedItemsCount = items.Count - pricedCount,
            TargetBudget = targetBudget,
            BudgetDifference = diff,
            IsWithinBudget = isWithinBudget
        };

        return ApiResponse<ShoppingListSummaryDto>.Ok(summary);
    }

    public async Task<ApiResponse<ShoppingListItemDto>> AddItemAsync(CreateShoppingListItemDto dto)
    {
        var item = new ShoppingListItem
        {
            ProductName = dto.ProductName.Trim(),
            Quantity = dto.Quantity,
            Unit = dto.Unit.Trim().ToLowerInvariant(),
            CreatedAt = DateTime.UtcNow
        };

        _context.ShoppingListItems.Add(item);
        await _context.SaveChangesAsync();

        var result = new ShoppingListItemDto
        {
            Id = item.Id,
            ProductName = item.ProductName,
            Quantity = item.Quantity,
            Unit = item.Unit,
            IsPurchased = item.IsPurchased,
            CreatedAt = item.CreatedAt
        };

        return ApiResponse<ShoppingListItemDto>.Ok(result, "Shopping list item added.");
    }

    public async Task<ApiResponse> RemoveItemAsync(int id)
    {
        var item = await _context.ShoppingListItems.FindAsync(id);
        if (item == null)
        {
            return ApiResponse.Fail("Shopping list item not found.");
        }

        _context.ShoppingListItems.Remove(item);
        await _context.SaveChangesAsync();

        return ApiResponse.Ok("Item removed from shopping list.");
    }

    public async Task<ApiResponse<PurchaseDto>> BuyItemAsync(int id, CreatePurchaseDto purchaseDto)
    {
        var item = await _context.ShoppingListItems.FindAsync(id);
        if (item == null)
        {
            return ApiResponse<PurchaseDto>.Fail("Shopping list item not found.");
        }

        purchaseDto.FromList = true;
        var purchaseRes = await _purchaseService.CreatePurchaseAsync(purchaseDto, id);
        return purchaseRes;
    }
}

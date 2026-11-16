using Expense_Tracker.Common;
using Expense_Tracker.Data;
using Expense_Tracker.DTOs;
using Expense_Tracker.DTOs.Common;
using Expense_Tracker.Interfaces;
using Expense_Tracker.Models;
using Microsoft.EntityFrameworkCore;

namespace Expense_Tracker.Services;

public class ShopService : IShopService
{
    private readonly ApplicationDbContext _context;

    public ShopService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<ShopDto>>> GetAllShopsAsync()
    {
        var shops = await _context.Shops
            .Include(s => s.Purchases)
            .AsNoTracking()
            .ToListAsync();

        var allPurchases = await _context.Purchases
            .AsNoTracking()
            .ToListAsync();

        // Calculate lowest price matches across products
        var productGroups = allPurchases
            .GroupBy(p =>
            {
                var (baseUnit, _) = UnitConverter.GetUnitInfo(p.Unit);
                return (ProductName: p.ProductName.Trim().ToLowerInvariant(), BaseUnit: baseUnit);
            })
            .Select(g =>
            {
                // Find shop with lowest price
                var low = g.Select(p => new
                {
                    p.ShopName,
                    NormalizedPrice = UnitConverter.NormalizePrice(p.UnitPrice, p.Unit)
                })
                .OrderBy(x => x.NormalizedPrice)
                .FirstOrDefault();

                var shopCount = g.Select(x => x.ShopName.ToLower()).Distinct().Count();

                return new
                {
                    GroupKey = g.Key,
                    HasMultipleShops = shopCount > 1,
                    LowestShop = low?.ShopName
                };
            })
            .Where(g => g.HasMultipleShops && !string.IsNullOrEmpty(g.LowestShop))
            .ToList();

        var result = shops.Select(sh =>
        {
            var shopPurchases = sh.Purchases.ToList();
            var totalSpending = shopPurchases.Sum(p => p.TotalPrice);
            var distinctProducts = shopPurchases.Select(p => p.ProductName.Trim().ToLowerInvariant()).Distinct().Count();
            var lowestMatches = productGroups.Count(g => string.Equals(g.LowestShop, sh.Name, StringComparison.OrdinalIgnoreCase));

            return new ShopDto
            {
                Id = sh.Id,
                Name = sh.Name,
                Location = sh.Location,
                Contact = sh.Contact,
                Notes = sh.Notes,
                IsDemo = sh.IsDemo,
                TotalSpending = totalSpending,
                ProductsCount = distinctProducts,
                LowestPriceMatchesCount = lowestMatches,
                CreatedAt = sh.CreatedAt,
                UpdatedAt = sh.UpdatedAt
            };
        })
        .OrderBy(s => s.Name)
        .ToList();

        return ApiResponse<List<ShopDto>>.Ok(result);
    }

    public async Task<ApiResponse<ShopDto>> GetShopByIdAsync(int id)
    {
        var sh = await _context.Shops
            .Include(s => s.Purchases)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id);

        if (sh == null)
        {
            return ApiResponse<ShopDto>.Fail("Shop not found.");
        }

        var totalSpending = sh.Purchases.Sum(p => p.TotalPrice);
        var distinctProducts = sh.Purchases.Select(p => p.ProductName.Trim().ToLowerInvariant()).Distinct().Count();

        var dto = new ShopDto
        {
            Id = sh.Id,
            Name = sh.Name,
            Location = sh.Location,
            Contact = sh.Contact,
            Notes = sh.Notes,
            IsDemo = sh.IsDemo,
            TotalSpending = totalSpending,
            ProductsCount = distinctProducts,
            LowestPriceMatchesCount = 0,
            CreatedAt = sh.CreatedAt,
            UpdatedAt = sh.UpdatedAt
        };

        return ApiResponse<ShopDto>.Ok(dto);
    }

    public async Task<ApiResponse<ShopDto>> CreateShopAsync(CreateShopDto dto)
    {
        var name = dto.Name.Trim();
        var exists = await _context.Shops.AnyAsync(s => s.Name.ToLower() == name.ToLower());
        if (exists)
        {
            return ApiResponse<ShopDto>.Fail("A shop with that name already exists.");
        }

        var shop = new Shop
        {
            Name = name,
            Location = dto.Location?.Trim(),
            Contact = dto.Contact?.Trim(),
            Notes = dto.Notes?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Shops.Add(shop);
        await _context.SaveChangesAsync();

        var result = new ShopDto
        {
            Id = shop.Id,
            Name = shop.Name,
            Location = shop.Location,
            Contact = shop.Contact,
            Notes = shop.Notes,
            CreatedAt = shop.CreatedAt
        };

        return ApiResponse<ShopDto>.Ok(result, "Shop created successfully.");
    }

    public async Task<ApiResponse<ShopDto>> UpdateShopAsync(int id, UpdateShopDto dto)
    {
        var shop = await _context.Shops.FirstOrDefaultAsync(s => s.Id == id);
        if (shop == null)
        {
            return ApiResponse<ShopDto>.Fail("Shop not found.");
        }

        var name = dto.Name.Trim();
        var duplicate = await _context.Shops.AnyAsync(s => s.Id != id && s.Name.ToLower() == name.ToLower());
        if (duplicate)
        {
            return ApiResponse<ShopDto>.Fail("Another shop with that name already exists.");
        }

        var oldName = shop.Name;
        shop.Name = name;
        shop.Location = dto.Location?.Trim();
        shop.Contact = dto.Contact?.Trim();
        shop.Notes = dto.Notes?.Trim();
        shop.UpdatedAt = DateTime.UtcNow;

        // Sync name across purchases and linked transactions if renamed
        if (!string.Equals(oldName, name, StringComparison.OrdinalIgnoreCase))
        {
            var linkedPurchases = await _context.Purchases.Where(p => p.ShopId == id || p.ShopName.ToLower() == oldName.ToLower()).ToListAsync();
            foreach (var p in linkedPurchases)
            {
                p.ShopName = name;
                p.ShopId = id;
            }

            var linkedTransactions = await _context.Transactions.Where(t => t.Shop != null && t.Shop.ToLower() == oldName.ToLower()).ToListAsync();
            foreach (var t in linkedTransactions)
            {
                t.Shop = name;
                if (t.PurchaseId.HasValue && t.Title.Contains(" @ "))
                {
                    var prefix = t.Title.Split(" @ ")[0];
                    t.Title = $"{prefix} @ {name}";
                }
            }
        }

        await _context.SaveChangesAsync();

        var result = new ShopDto
        {
            Id = shop.Id,
            Name = shop.Name,
            Location = shop.Location,
            Contact = shop.Contact,
            Notes = shop.Notes,
            CreatedAt = shop.CreatedAt,
            UpdatedAt = shop.UpdatedAt
        };

        return ApiResponse<ShopDto>.Ok(result, "Shop updated successfully.");
    }

    public async Task<ApiResponse> DeleteShopAsync(int id)
    {
        var shop = await _context.Shops
            .Include(s => s.Purchases)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (shop == null)
        {
            return ApiResponse.Fail("Shop not found.");
        }

        var purchaseCount = shop.Purchases.Count;
        if (purchaseCount > 0)
        {
            return ApiResponse.Fail($"{purchaseCount} purchase(s) use this shop — rename it instead, or delete those purchases first.");
        }

        _context.Shops.Remove(shop);
        await _context.SaveChangesAsync();

        return ApiResponse.Ok("Shop removed successfully.");
    }
}

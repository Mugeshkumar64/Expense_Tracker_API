using Expense_Tracker.Data;
using Expense_Tracker.DTOs;
using Expense_Tracker.DTOs.Common;
using Expense_Tracker.Interfaces;
using Expense_Tracker.Models;
using Microsoft.EntityFrameworkCore;

namespace Expense_Tracker.Services;

public class SettingsService : ISettingsService
{
    private readonly ApplicationDbContext _context;

    public SettingsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<UserSettingsDto>> GetSettingsAsync()
    {
        var settings = await _context.UserSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new UserSettings
            {
                Currency = "₹",
                MonthBudget = 0,
                ShopBudget = 0,
                ListBudget = 0,
                UpdatedAt = DateTime.UtcNow
            };
            _context.UserSettings.Add(settings);
            await _context.SaveChangesAsync();
        }

        var dto = new UserSettingsDto
        {
            Id = settings.Id,
            Currency = settings.Currency,
            MonthBudget = settings.MonthBudget,
            ShopBudget = settings.ShopBudget,
            ListBudget = settings.ListBudget,
            UpdatedAt = settings.UpdatedAt
        };

        return ApiResponse<UserSettingsDto>.Ok(dto);
    }

    public async Task<ApiResponse<UserSettingsDto>> UpdateSettingsAsync(UpdateSettingsDto dto)
    {
        var settings = await _context.UserSettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            settings = new UserSettings();
            _context.UserSettings.Add(settings);
        }

        settings.Currency = string.IsNullOrWhiteSpace(dto.Currency) ? "₹" : dto.Currency.Trim();
        settings.MonthBudget = Math.Max(0, dto.MonthBudget);
        settings.ShopBudget = Math.Max(0, dto.ShopBudget);
        settings.ListBudget = Math.Max(0, dto.ListBudget);
        settings.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var result = new UserSettingsDto
        {
            Id = settings.Id,
            Currency = settings.Currency,
            MonthBudget = settings.MonthBudget,
            ShopBudget = settings.ShopBudget,
            ListBudget = settings.ListBudget,
            UpdatedAt = settings.UpdatedAt
        };

        return ApiResponse<UserSettingsDto>.Ok(result, "Settings updated successfully.");
    }

    public async Task<ApiResponse> LoadDemoDataAsync()
    {
        var hasDemo = await _context.Purchases.AnyAsync(p => p.IsDemo);
        if (hasDemo)
        {
            return ApiResponse.Fail("Demo data already loaded.");
        }

        var demoItems = new[]
        {
            new { Product = "Rice", Qty = 5m, Unit = "kg", Price = 60m, Shop = "Shop A", DaysAgo = 50 },
            new { Product = "Rice", Qty = 5m, Unit = "kg", Price = 54m, Shop = "Shop B", DaysAgo = 35 },
            new { Product = "Rice", Qty = 5m, Unit = "kg", Price = 58m, Shop = "Shop C", DaysAgo = 20 },
            new { Product = "Rice", Qty = 5m, Unit = "kg", Price = 62m, Shop = "Shop A", DaysAgo = 6 },
            new { Product = "Cooking Oil", Qty = 2m, Unit = "litre", Price = 135m, Shop = "Shop A", DaysAgo = 30 },
            new { Product = "Cooking Oil", Qty = 3m, Unit = "litre", Price = 120m, Shop = "Shop B", DaysAgo = 9 },
            new { Product = "Sugar", Qty = 2m, Unit = "kg", Price = 46m, Shop = "Shop A", DaysAgo = 25 },
            new { Product = "Sugar", Qty = 2m, Unit = "kg", Price = 42m, Shop = "Shop C", DaysAgo = 12 },
            new { Product = "Milk", Qty = 3m, Unit = "litre", Price = 54m, Shop = "Shop B", DaysAgo = 8 },
            new { Product = "Milk", Qty = 3m, Unit = "litre", Price = 56m, Shop = "Shop A", DaysAgo = 3 }
        };

        // Ensure Groceries category exists
        var cat = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "Groceries" && c.Type == "expense");
        if (cat == null)
        {
            cat = new Category { Name = "Groceries", Type = "expense", CreatedAt = DateTime.UtcNow };
            _context.Categories.Add(cat);
            await _context.SaveChangesAsync();
        }

        var today = DateTime.UtcNow.Date;

        foreach (var item in demoItems)
        {
            var shop = await _context.Shops.FirstOrDefaultAsync(s => s.Name == item.Shop);
            if (shop == null)
            {
                shop = new Shop { Name = item.Shop, IsDemo = true, CreatedAt = DateTime.UtcNow };
                _context.Shops.Add(shop);
                await _context.SaveChangesAsync();
            }

            var date = today.AddDays(-item.DaysAgo);
            var total = Math.Round(item.Qty * item.Price, 2);

            var purchase = new Purchase
            {
                ProductName = item.Product,
                CategoryId = cat.Id,
                Quantity = item.Qty,
                Unit = item.Unit,
                UnitPrice = item.Price,
                TotalPrice = total,
                ShopId = shop.Id,
                ShopName = shop.Name,
                PaymentMethod = "UPI",
                Date = date,
                Notes = "Demo data",
                IsDemo = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Purchases.Add(purchase);
            await _context.SaveChangesAsync();

            var tx = new Transaction
            {
                Title = $"{item.Product} @ {shop.Name}",
                Amount = total,
                Type = "expense",
                CategoryId = cat.Id,
                Date = date,
                PaymentMethod = "UPI",
                Notes = "Demo data",
                Shop = shop.Name,
                PurchaseId = purchase.Id,
                IsDemo = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Transactions.Add(tx);
            await _context.SaveChangesAsync();

            purchase.TransactionId = tx.Id;
        }

        await _context.SaveChangesAsync();
        return ApiResponse.Ok("Demo data loaded successfully.");
    }

    public async Task<ApiResponse> ClearDemoDataAsync()
    {
        var demoTransactions = await _context.Transactions.Where(t => t.IsDemo).ToListAsync();
        var demoPurchases = await _context.Purchases.Where(p => p.IsDemo).ToListAsync();

        _context.Transactions.RemoveRange(demoTransactions);
        _context.Purchases.RemoveRange(demoPurchases);

        // Remove demo shops that have no other purchases
        var demoShops = await _context.Shops.Where(s => s.IsDemo).ToListAsync();
        foreach (var shop in demoShops)
        {
            var hasOther = await _context.Purchases.AnyAsync(p => p.ShopId == shop.Id && !p.IsDemo);
            if (!hasOther)
            {
                _context.Shops.Remove(shop);
            }
        }

        await _context.SaveChangesAsync();
        return ApiResponse.Ok("Demo data removed successfully.");
    }

    public async Task<ApiResponse<BackupDataDto>> ExportBackupAsync()
    {
        var settingsRes = await GetSettingsAsync();
        var settings = settingsRes.Data ?? new UserSettingsDto();

        var categories = await _context.Categories
            .AsNoTracking()
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Type = c.Type,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        var shops = await _context.Shops
            .AsNoTracking()
            .Select(s => new ShopDto
            {
                Id = s.Id,
                Name = s.Name,
                Location = s.Location,
                Contact = s.Contact,
                Notes = s.Notes,
                IsDemo = s.IsDemo,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        var transactions = await _context.Transactions
            .Include(t => t.Category)
            .AsNoTracking()
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
            .ToListAsync();

        var purchases = await _context.Purchases
            .Include(p => p.Category)
            .AsNoTracking()
            .Select(p => new PurchaseDto
            {
                Id = p.Id,
                ProductName = p.ProductName,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : "Groceries",
                Quantity = p.Quantity,
                Unit = p.Unit,
                UnitPrice = p.UnitPrice,
                TotalPrice = p.TotalPrice,
                ShopId = p.ShopId,
                ShopName = p.ShopName,
                PaymentMethod = p.PaymentMethod,
                Date = p.Date,
                Notes = p.Notes,
                FromList = p.FromList,
                IsDemo = p.IsDemo,
                TransactionId = p.TransactionId,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        var shoppingList = await _context.ShoppingListItems
            .AsNoTracking()
            .Select(i => new ShoppingListItemDto
            {
                Id = i.Id,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                Unit = i.Unit,
                IsPurchased = i.IsPurchased,
                CreatedAt = i.CreatedAt
            })
            .ToListAsync();

        var backup = new BackupDataDto
        {
            Settings = settings,
            Categories = categories,
            Shops = shops,
            Transactions = transactions,
            Purchases = purchases,
            ShoppingList = shoppingList
        };

        return ApiResponse<BackupDataDto>.Ok(backup);
    }

    public async Task<ApiResponse> ImportBackupAsync(BackupDataDto backupData)
    {
        if (backupData == null)
        {
            return ApiResponse.Fail("Invalid backup data.");
        }

        // Clear existing data
        _context.Transactions.RemoveRange(await _context.Transactions.ToListAsync());
        _context.Purchases.RemoveRange(await _context.Purchases.ToListAsync());
        _context.ShoppingListItems.RemoveRange(await _context.ShoppingListItems.ToListAsync());
        _context.Shops.RemoveRange(await _context.Shops.ToListAsync());
        _context.Categories.RemoveRange(await _context.Categories.ToListAsync());

        await _context.SaveChangesAsync();

        // Restore Categories
        var catMap = new Dictionary<string, Category>();
        foreach (var c in backupData.Categories)
        {
            var cat = new Category
            {
                Name = c.Name,
                Type = c.Type,
                CreatedAt = c.CreatedAt
            };
            _context.Categories.Add(cat);
            catMap[c.Name.ToLower()] = cat;
        }
        await _context.SaveChangesAsync();

        // Restore Shops
        var shopMap = new Dictionary<string, Shop>();
        foreach (var s in backupData.Shops)
        {
            var shop = new Shop
            {
                Name = s.Name,
                Location = s.Location,
                Contact = s.Contact,
                Notes = s.Notes,
                IsDemo = s.IsDemo,
                CreatedAt = s.CreatedAt
            };
            _context.Shops.Add(shop);
            shopMap[s.Name.ToLower()] = shop;
        }
        await _context.SaveChangesAsync();

        // Restore Purchases and Transactions
        foreach (var p in backupData.Purchases)
        {
            catMap.TryGetValue((p.CategoryName ?? "groceries").ToLower(), out var cat);
            shopMap.TryGetValue(p.ShopName.ToLower(), out var shop);

            var purchase = new Purchase
            {
                ProductName = p.ProductName,
                CategoryId = cat?.Id ?? catMap.Values.First().Id,
                Quantity = p.Quantity,
                Unit = p.Unit,
                UnitPrice = p.UnitPrice,
                TotalPrice = p.TotalPrice,
                ShopId = shop?.Id,
                ShopName = p.ShopName,
                PaymentMethod = p.PaymentMethod,
                Date = p.Date,
                Notes = p.Notes,
                FromList = p.FromList,
                IsDemo = p.IsDemo,
                CreatedAt = p.CreatedAt
            };
            _context.Purchases.Add(purchase);
            await _context.SaveChangesAsync();

            var tx = new Transaction
            {
                Title = $"{p.ProductName} @ {p.ShopName}",
                Amount = p.TotalPrice,
                Type = "expense",
                CategoryId = purchase.CategoryId,
                Date = p.Date,
                PaymentMethod = p.PaymentMethod,
                Notes = p.Notes,
                Shop = p.ShopName,
                PurchaseId = purchase.Id,
                IsDemo = p.IsDemo,
                CreatedAt = p.CreatedAt
            };
            _context.Transactions.Add(tx);
            await _context.SaveChangesAsync();

            purchase.TransactionId = tx.Id;
        }

        // Restore other non-purchase transactions
        foreach (var t in backupData.Transactions.Where(t => !t.PurchaseId.HasValue))
        {
            catMap.TryGetValue((t.CategoryName ?? "other").ToLower(), out var cat);
            var tx = new Transaction
            {
                Title = t.Title,
                Amount = t.Amount,
                Type = t.Type,
                CategoryId = cat?.Id ?? catMap.Values.First().Id,
                Date = t.Date,
                PaymentMethod = t.PaymentMethod,
                Notes = t.Notes,
                Shop = t.Shop,
                IsDemo = t.IsDemo,
                CreatedAt = t.CreatedAt
            };
            _context.Transactions.Add(tx);
        }

        // Restore Shopping List
        foreach (var it in backupData.ShoppingList)
        {
            _context.ShoppingListItems.Add(new ShoppingListItem
            {
                ProductName = it.ProductName,
                Quantity = it.Quantity,
                Unit = it.Unit,
                IsPurchased = it.IsPurchased,
                CreatedAt = it.CreatedAt
            });
        }

        // Restore Settings
        if (backupData.Settings != null)
        {
            var settings = await _context.UserSettings.FirstOrDefaultAsync();
            if (settings == null)
            {
                settings = new UserSettings();
                _context.UserSettings.Add(settings);
            }
            settings.Currency = backupData.Settings.Currency;
            settings.MonthBudget = backupData.Settings.MonthBudget;
            settings.ShopBudget = backupData.Settings.ShopBudget;
            settings.ListBudget = backupData.Settings.ListBudget;
            settings.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return ApiResponse.Ok("Backup restored successfully.");
    }

    public async Task<ApiResponse> ResetDatabaseAsync()
    {
        _context.Transactions.RemoveRange(await _context.Transactions.ToListAsync());
        _context.Purchases.RemoveRange(await _context.Purchases.ToListAsync());
        _context.ShoppingListItems.RemoveRange(await _context.ShoppingListItems.ToListAsync());
        _context.Shops.RemoveRange(await _context.Shops.ToListAsync());
        await _context.SaveChangesAsync();

        return ApiResponse.Ok("Ledger cleared successfully.");
    }
}

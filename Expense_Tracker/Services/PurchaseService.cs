using System.Text;
using Expense_Tracker.Common;
using Expense_Tracker.Data;
using Expense_Tracker.DTOs;
using Expense_Tracker.DTOs.Common;
using Expense_Tracker.Interfaces;
using Expense_Tracker.Models;
using Microsoft.EntityFrameworkCore;

namespace Expense_Tracker.Services;

public class PurchaseService : IPurchaseService
{
    private readonly ApplicationDbContext _context;

    public PurchaseService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<PurchaseDto>>> GetAllPurchasesAsync(string? search = null)
    {
        var query = _context.Purchases
            .Include(p => p.Category)
            .Include(p => p.Shop)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLower();
            query = query.Where(p =>
                p.ProductName.ToLower().Contains(q) ||
                p.ShopName.ToLower().Contains(q) ||
                (p.Notes != null && p.Notes.ToLower().Contains(q)));
        }

        var purchases = await query
            .OrderByDescending(p => p.Date)
            .ThenByDescending(p => p.CreatedAt)
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
                ShopLocation = p.Shop != null ? p.Shop.Location : null,
                PaymentMethod = p.PaymentMethod,
                Date = p.Date,
                Notes = p.Notes,
                FromList = p.FromList,
                IsDemo = p.IsDemo,
                TransactionId = p.TransactionId,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .ToListAsync();

        return ApiResponse<List<PurchaseDto>>.Ok(purchases);
    }

    public async Task<ApiResponse<PurchaseDto>> GetPurchaseByIdAsync(int id)
    {
        var p = await _context.Purchases
            .Include(x => x.Category)
            .Include(x => x.Shop)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (p == null)
        {
            return ApiResponse<PurchaseDto>.Fail("Purchase record not found.");
        }

        var dto = new PurchaseDto
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
            ShopLocation = p.Shop != null ? p.Shop.Location : null,
            PaymentMethod = p.PaymentMethod,
            Date = p.Date,
            Notes = p.Notes,
            FromList = p.FromList,
            IsDemo = p.IsDemo,
            TransactionId = p.TransactionId,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };

        return ApiResponse<PurchaseDto>.Ok(dto);
    }

    public async Task<ApiResponse<PurchaseDto>> CreatePurchaseAsync(CreatePurchaseDto dto, int? listItemId = null)
    {
        var product = dto.ProductName.Trim();
        var shopName = dto.ShopName.Trim();
        var total = Math.Round(dto.Quantity * dto.UnitPrice, 2);

        // Find or create shop
        var shop = await _context.Shops.FirstOrDefaultAsync(s => s.Name.ToLower() == shopName.ToLower());
        if (shop == null)
        {
            shop = new Shop
            {
                Name = shopName,
                Location = dto.ShopLocation?.Trim(),
                IsDemo = dto.IsDemo,
                CreatedAt = DateTime.UtcNow
            };
            _context.Shops.Add(shop);
            await _context.SaveChangesAsync();
        }
        else if (string.IsNullOrEmpty(shop.Location) && !string.IsNullOrWhiteSpace(dto.ShopLocation))
        {
            shop.Location = dto.ShopLocation.Trim();
        }

        // Find or create category
        var catName = string.IsNullOrWhiteSpace(dto.CategoryName) ? "Groceries" : dto.CategoryName.Trim();
        var category = await _context.Categories.FirstOrDefaultAsync(c =>
            (dto.CategoryId.HasValue && c.Id == dto.CategoryId.Value) ||
            (c.Name.ToLower() == catName.ToLower() && c.Type == "expense"));

        if (category == null)
        {
            category = new Category
            {
                Name = catName,
                Type = "expense",
                CreatedAt = DateTime.UtcNow
            };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
        }

        // Create Purchase
        var purchase = new Purchase
        {
            ProductName = product,
            CategoryId = category.Id,
            Quantity = dto.Quantity,
            Unit = dto.Unit.Trim().ToLowerInvariant(),
            UnitPrice = dto.UnitPrice,
            TotalPrice = total,
            ShopId = shop.Id,
            ShopName = shop.Name,
            PaymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? "UPI" : dto.PaymentMethod.Trim(),
            Date = dto.Date,
            Notes = dto.Notes?.Trim(),
            FromList = dto.FromList || listItemId.HasValue,
            IsDemo = dto.IsDemo,
            CreatedAt = DateTime.UtcNow
        };

        _context.Purchases.Add(purchase);
        await _context.SaveChangesAsync();

        // Create linked Expense Transaction
        var transaction = new Transaction
        {
            Title = $"{product} @ {shop.Name}",
            Amount = total,
            Type = "expense",
            CategoryId = category.Id,
            Date = dto.Date,
            PaymentMethod = purchase.PaymentMethod,
            Notes = purchase.Notes,
            Shop = shop.Name,
            PurchaseId = purchase.Id,
            IsDemo = dto.IsDemo,
            CreatedAt = DateTime.UtcNow
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        purchase.TransactionId = transaction.Id;
        await _context.SaveChangesAsync();

        // If from shopping list, remove the list item
        if (listItemId.HasValue)
        {
            var item = await _context.ShoppingListItems.FindAsync(listItemId.Value);
            if (item != null)
            {
                _context.ShoppingListItems.Remove(item);
                await _context.SaveChangesAsync();
            }
        }

        var result = new PurchaseDto
        {
            Id = purchase.Id,
            ProductName = purchase.ProductName,
            CategoryId = category.Id,
            CategoryName = category.Name,
            Quantity = purchase.Quantity,
            Unit = purchase.Unit,
            UnitPrice = purchase.UnitPrice,
            TotalPrice = purchase.TotalPrice,
            ShopId = shop.Id,
            ShopName = shop.Name,
            ShopLocation = shop.Location,
            PaymentMethod = purchase.PaymentMethod,
            Date = purchase.Date,
            Notes = purchase.Notes,
            FromList = purchase.FromList,
            IsDemo = purchase.IsDemo,
            TransactionId = transaction.Id,
            CreatedAt = purchase.CreatedAt
        };

        return ApiResponse<PurchaseDto>.Ok(result, "Purchase saved and linked expense transaction created.");
    }

    public async Task<ApiResponse<PurchaseDto>> UpdatePurchaseAsync(int id, UpdatePurchaseDto dto)
    {
        var purchase = await _context.Purchases
            .Include(p => p.Transaction)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (purchase == null)
        {
            return ApiResponse<PurchaseDto>.Fail("Purchase record not found.");
        }

        var product = dto.ProductName.Trim();
        var shopName = dto.ShopName.Trim();
        var total = Math.Round(dto.Quantity * dto.UnitPrice, 2);

        // Find or create shop
        var shop = await _context.Shops.FirstOrDefaultAsync(s => s.Name.ToLower() == shopName.ToLower());
        if (shop == null)
        {
            shop = new Shop
            {
                Name = shopName,
                Location = dto.ShopLocation?.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            _context.Shops.Add(shop);
            await _context.SaveChangesAsync();
        }
        else if (string.IsNullOrEmpty(shop.Location) && !string.IsNullOrWhiteSpace(dto.ShopLocation))
        {
            shop.Location = dto.ShopLocation.Trim();
        }

        // Category
        var catName = string.IsNullOrWhiteSpace(dto.CategoryName) ? "Groceries" : dto.CategoryName.Trim();
        var category = await _context.Categories.FirstOrDefaultAsync(c =>
            (dto.CategoryId.HasValue && c.Id == dto.CategoryId.Value) ||
            (c.Name.ToLower() == catName.ToLower() && c.Type == "expense"));

        if (category == null)
        {
            category = new Category
            {
                Name = catName,
                Type = "expense",
                CreatedAt = DateTime.UtcNow
            };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
        }

        purchase.ProductName = product;
        purchase.CategoryId = category.Id;
        purchase.Quantity = dto.Quantity;
        purchase.Unit = dto.Unit.Trim().ToLowerInvariant();
        purchase.UnitPrice = dto.UnitPrice;
        purchase.TotalPrice = total;
        purchase.ShopId = shop.Id;
        purchase.ShopName = shop.Name;
        purchase.PaymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? "UPI" : dto.PaymentMethod.Trim();
        purchase.Date = dto.Date;
        purchase.Notes = dto.Notes?.Trim();
        purchase.UpdatedAt = DateTime.UtcNow;

        // Keep linked transaction in sync
        if (purchase.Transaction != null)
        {
            purchase.Transaction.Title = $"{product} @ {shop.Name}";
            purchase.Transaction.Amount = total;
            purchase.Transaction.CategoryId = category.Id;
            purchase.Transaction.Date = dto.Date;
            purchase.Transaction.PaymentMethod = purchase.PaymentMethod;
            purchase.Transaction.Notes = purchase.Notes;
            purchase.Transaction.Shop = shop.Name;
            purchase.Transaction.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        var result = new PurchaseDto
        {
            Id = purchase.Id,
            ProductName = purchase.ProductName,
            CategoryId = category.Id,
            CategoryName = category.Name,
            Quantity = purchase.Quantity,
            Unit = purchase.Unit,
            UnitPrice = purchase.UnitPrice,
            TotalPrice = purchase.TotalPrice,
            ShopId = shop.Id,
            ShopName = shop.Name,
            ShopLocation = shop.Location,
            PaymentMethod = purchase.PaymentMethod,
            Date = purchase.Date,
            Notes = purchase.Notes,
            FromList = purchase.FromList,
            IsDemo = purchase.IsDemo,
            TransactionId = purchase.TransactionId,
            CreatedAt = purchase.CreatedAt,
            UpdatedAt = purchase.UpdatedAt
        };

        return ApiResponse<PurchaseDto>.Ok(result, "Purchase updated successfully.");
    }

    public async Task<ApiResponse> DeletePurchaseAsync(int id)
    {
        var purchase = await _context.Purchases
            .Include(p => p.Transaction)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (purchase == null)
        {
            return ApiResponse.Fail("Purchase record not found.");
        }

        if (purchase.Transaction != null)
        {
            _context.Transactions.Remove(purchase.Transaction);
        }

        _context.Purchases.Remove(purchase);
        await _context.SaveChangesAsync();

        return ApiResponse.Ok("Purchase and its linked transaction removed successfully.");
    }

    public async Task<byte[]> ExportCsvAsync()
    {
        var purchases = await _context.Purchases
            .Include(p => p.Category)
            .AsNoTracking()
            .OrderByDescending(p => p.Date)
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Date,Product,Category,Shop,Qty,Unit,UnitPrice,Total");

        foreach (var p in purchases)
        {
            var date = EscapeCsv(p.Date.ToString("yyyy-MM-dd"));
            var prod = EscapeCsv(p.ProductName);
            var cat = EscapeCsv(p.Category?.Name ?? "Groceries");
            var shop = EscapeCsv(p.ShopName);
            var qty = p.Quantity.ToString("0.###");
            var unit = EscapeCsv(p.Unit);
            var unitPrice = p.UnitPrice.ToString("0.00");
            var total = p.TotalPrice.ToString("0.00");

            sb.AppendLine($"{date},{prod},{cat},{shop},{qty},{unit},{unitPrice},{total}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "\"\"";
        var escaped = value.Replace("\"", "\"\"");
        if (escaped.StartsWith("=") || escaped.StartsWith("+") || escaped.StartsWith("-") || escaped.StartsWith("@"))
        {
            escaped = "'" + escaped;
        }
        return $"\"{escaped}\"";
    }
}

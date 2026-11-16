using System.Text;
using Expense_Tracker.Data;
using Expense_Tracker.DTOs;
using Expense_Tracker.DTOs.Common;
using Expense_Tracker.Interfaces;
using Expense_Tracker.Models;
using Microsoft.EntityFrameworkCore;

namespace Expense_Tracker.Services;

public class TransactionService : ITransactionService
{
    private readonly ApplicationDbContext _context;

    public TransactionService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<TransactionDto>>> GetTransactionsAsync(TransactionFilterDto filter)
    {
        var query = _context.Transactions
            .Include(t => t.Category)
            .AsNoTracking()
            .AsQueryable();

        // Filter by Type
        if (!string.IsNullOrWhiteSpace(filter.Type) && !filter.Type.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var typeLower = filter.Type.Trim().ToLower();
            query = query.Where(t => t.Type.ToLower() == typeLower);
        }

        // Filter by Category
        if (!string.IsNullOrWhiteSpace(filter.Category) && !filter.Category.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var catLower = filter.Category.Trim().ToLower();
            query = query.Where(t => t.Category != null && t.Category.Name.ToLower() == catLower);
        }

        // Filter by Shop
        if (!string.IsNullOrWhiteSpace(filter.Shop) && !filter.Shop.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var shopLower = filter.Shop.Trim().ToLower();
            query = query.Where(t => t.Shop != null && t.Shop.ToLower() == shopLower);
        }

        // Filter by Date Range
        if (!string.IsNullOrWhiteSpace(filter.DateRange) && !filter.DateRange.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            var now = DateTime.UtcNow.Date;
            switch (filter.DateRange.Trim().ToLower())
            {
                case "today":
                    query = query.Where(t => t.Date.Date == now);
                    break;
                case "week":
                    var startOfWeek = now.AddDays(-7);
                    query = query.Where(t => t.Date.Date >= startOfWeek && t.Date.Date <= now);
                    break;
                case "month":
                    query = query.Where(t => t.Date.Year == now.Year && t.Date.Month == now.Month);
                    break;
                case "lastmonth":
                    var lastMonthDate = now.AddMonths(-1);
                    query = query.Where(t => t.Date.Year == lastMonthDate.Year && t.Date.Month == lastMonthDate.Month);
                    break;
            }
        }

        // Search text
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(t =>
                t.Title.ToLower().Contains(search) ||
                (t.Category != null && t.Category.Name.ToLower().Contains(search)) ||
                (t.Notes != null && t.Notes.ToLower().Contains(search)) ||
                (t.Shop != null && t.Shop.ToLower().Contains(search)));
        }

        // Sorting
        query = (filter.Sort?.Trim().ToLower()) switch
        {
            "old" => query.OrderBy(t => t.Date).ThenBy(t => t.CreatedAt),
            "high" => query.OrderByDescending(t => t.Amount),
            "low" => query.OrderBy(t => t.Amount),
            "shop" => query.OrderBy(t => t.Shop ?? "zzzz"),
            _ => query.OrderByDescending(t => t.Date).ThenByDescending(t => t.CreatedAt) // "new" default
        };

        var transactions = await query
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
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt
            })
            .ToListAsync();

        return ApiResponse<List<TransactionDto>>.Ok(transactions);
    }

    public async Task<ApiResponse<TransactionDto>> GetTransactionByIdAsync(int id)
    {
        var t = await _context.Transactions
            .Include(x => x.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id);

        if (t == null)
        {
            return ApiResponse<TransactionDto>.Fail("Transaction not found.");
        }

        var dto = new TransactionDto
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
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        };

        return ApiResponse<TransactionDto>.Ok(dto);
    }

    public async Task<ApiResponse<TransactionDto>> CreateTransactionAsync(CreateTransactionDto dto)
    {
        var category = await ResolveCategoryAsync(dto.CategoryId, dto.CategoryName, dto.Type);
        if (category == null)
        {
            return ApiResponse<TransactionDto>.Fail("Valid category is required.");
        }

        var transaction = new Transaction
        {
            Title = dto.Title.Trim(),
            Amount = dto.Amount,
            Type = dto.Type.Trim().ToLowerInvariant(),
            CategoryId = category.Id,
            Date = dto.Date,
            PaymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? "UPI" : dto.PaymentMethod.Trim(),
            Notes = dto.Notes?.Trim(),
            Shop = dto.Shop?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync();

        var result = new TransactionDto
        {
            Id = transaction.Id,
            Title = transaction.Title,
            Amount = transaction.Amount,
            Type = transaction.Type,
            CategoryId = category.Id,
            CategoryName = category.Name,
            Date = transaction.Date,
            PaymentMethod = transaction.PaymentMethod,
            Notes = transaction.Notes,
            Shop = transaction.Shop,
            PurchaseId = transaction.PurchaseId,
            IsDemo = transaction.IsDemo,
            CreatedAt = transaction.CreatedAt
        };

        return ApiResponse<TransactionDto>.Ok(result, "Transaction recorded successfully.");
    }

    public async Task<ApiResponse<TransactionDto>> UpdateTransactionAsync(int id, UpdateTransactionDto dto)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Category)
            .Include(t => t.Purchase)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transaction == null)
        {
            return ApiResponse<TransactionDto>.Fail("Transaction not found.");
        }

        var category = await ResolveCategoryAsync(dto.CategoryId, dto.CategoryName, dto.Type);
        if (category == null)
        {
            return ApiResponse<TransactionDto>.Fail("Valid category is required.");
        }

        transaction.Title = dto.Title.Trim();
        transaction.Amount = dto.Amount;
        transaction.Type = dto.Type.Trim().ToLowerInvariant();
        transaction.CategoryId = category.Id;
        transaction.Date = dto.Date;
        transaction.PaymentMethod = string.IsNullOrWhiteSpace(dto.PaymentMethod) ? "UPI" : dto.PaymentMethod.Trim();
        transaction.Notes = dto.Notes?.Trim();
        transaction.Shop = dto.Shop?.Trim();
        transaction.UpdatedAt = DateTime.UtcNow;

        // If this transaction was linked to a purchase, sync purchase fields
        if (transaction.Purchase != null)
        {
            transaction.Purchase.TotalPrice = dto.Amount;
            transaction.Purchase.Date = dto.Date;
            transaction.Purchase.Notes = dto.Notes?.Trim();
            transaction.Purchase.PaymentMethod = transaction.PaymentMethod;
            transaction.Purchase.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        var result = new TransactionDto
        {
            Id = transaction.Id,
            Title = transaction.Title,
            Amount = transaction.Amount,
            Type = transaction.Type,
            CategoryId = category.Id,
            CategoryName = category.Name,
            Date = transaction.Date,
            PaymentMethod = transaction.PaymentMethod,
            Notes = transaction.Notes,
            Shop = transaction.Shop,
            PurchaseId = transaction.PurchaseId,
            IsDemo = transaction.IsDemo,
            CreatedAt = transaction.CreatedAt,
            UpdatedAt = transaction.UpdatedAt
        };

        return ApiResponse<TransactionDto>.Ok(result, "Transaction updated successfully.");
    }

    public async Task<ApiResponse> DeleteTransactionAsync(int id)
    {
        var transaction = await _context.Transactions
            .Include(t => t.Purchase)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (transaction == null)
        {
            return ApiResponse.Fail("Transaction not found.");
        }

        if (transaction.Purchase != null)
        {
            _context.Purchases.Remove(transaction.Purchase);
        }

        _context.Transactions.Remove(transaction);
        await _context.SaveChangesAsync();

        return ApiResponse.Ok("Transaction and associated records deleted successfully.");
    }

    public async Task<byte[]> ExportCsvAsync(TransactionFilterDto filter)
    {
        var res = await GetTransactionsAsync(filter);
        var list = res.Data ?? new List<TransactionDto>();

        var sb = new StringBuilder();
        sb.AppendLine("Date,Title,Type,Category,Shop,PaymentMethod,Amount,Notes");

        foreach (var t in list)
        {
            var date = EscapeCsv(t.Date.ToString("yyyy-MM-dd"));
            var title = EscapeCsv(t.Title);
            var type = EscapeCsv(t.Type);
            var cat = EscapeCsv(t.CategoryName);
            var shop = EscapeCsv(t.Shop ?? string.Empty);
            var payment = EscapeCsv(t.PaymentMethod);
            var amount = t.Amount.ToString("0.00");
            var notes = EscapeCsv(t.Notes ?? string.Empty);

            sb.AppendLine($"{date},{title},{type},{cat},{shop},{payment},{amount},{notes}");
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private async Task<Category?> ResolveCategoryAsync(int? categoryId, string? categoryName, string type)
    {
        if (categoryId.HasValue && categoryId.Value > 0)
        {
            return await _context.Categories.FindAsync(categoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            var trimmed = categoryName.Trim();
            var cat = await _context.Categories.FirstOrDefaultAsync(c =>
                c.Name.ToLower() == trimmed.ToLower() && c.Type.ToLower() == type.ToLower());

            if (cat != null) return cat;

            // Auto-create category if missing
            cat = new Category
            {
                Name = trimmed,
                Type = type.ToLowerInvariant(),
                CreatedAt = DateTime.UtcNow
            };
            _context.Categories.Add(cat);
            await _context.SaveChangesAsync();
            return cat;
        }

        return await _context.Categories.FirstOrDefaultAsync(c => c.Type.ToLower() == type.ToLower());
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

using System.Globalization;
using System.Text;
using Expense_Tracker.Common;
using Expense_Tracker.Data;
using Expense_Tracker.DTOs;
using Expense_Tracker.DTOs.Common;
using Expense_Tracker.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Expense_Tracker.Services;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;

    public ReportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<string>>> GetAvailableMonthsAsync()
    {
        var dates = await _context.Transactions
            .AsNoTracking()
            .Select(t => t.Date)
            .ToListAsync();

        var months = dates
            .Select(d => d.ToString("yyyy-MM"))
            .Distinct()
            .OrderByDescending(m => m)
            .ToList();

        if (!months.Any())
        {
            months.Add(DateTime.UtcNow.ToString("yyyy-MM"));
        }

        return ApiResponse<List<string>>.Ok(months);
    }

    public async Task<ApiResponse<MonthlyReportDto>> GetMonthlyReportAsync(string? month = null)
    {
        var targetMonth = string.IsNullOrWhiteSpace(month) ? DateTime.UtcNow.ToString("yyyy-MM") : month.Trim();
        var dt = DateTime.ParseExact(targetMonth + "-01", "yyyy-MM-dd", CultureInfo.InvariantCulture);

        var transactions = await _context.Transactions
            .Include(t => t.Category)
            .AsNoTracking()
            .Where(t => t.Date.Year == dt.Year && t.Date.Month == dt.Month)
            .ToListAsync();

        var income = transactions.Where(t => t.Type == "income").Sum(t => t.Amount);
        var expense = transactions.Where(t => t.Type == "expense").Sum(t => t.Amount);
        var net = income - expense;
        var savingsRate = income > 0 ? (int)Math.Round((net / income) * 100) : 0;

        var expenseTx = transactions.Where(t => t.Type == "expense").ToList();

        var catExpenses = expenseTx
            .GroupBy(t => t.Category != null ? t.Category.Name : "Other")
            .Select(g => new CategoryExpenseAllocationDto
            {
                CategoryName = g.Key,
                Amount = g.Sum(t => t.Amount),
                Percentage = expense > 0 ? Math.Round((g.Sum(t => t.Amount) / expense) * 100, 1) : 0
            })
            .OrderByDescending(c => c.Amount)
            .ToList();

        var topCat = catExpenses.FirstOrDefault();
        var highestTx = expenseTx.OrderByDescending(t => t.Amount).FirstOrDefault();
        var avgTicket = expenseTx.Any() ? Math.Round(expense / expenseTx.Count, 2) : 0;

        var report = new MonthlyReportDto
        {
            Month = targetMonth,
            MonthName = dt.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
            TotalInflow = income,
            TotalOutflow = expense,
            NetPosition = net,
            SavingsRate = savingsRate,
            CategoryExpenses = catExpenses,
            TopOutflowCategory = topCat?.CategoryName,
            TopOutflowCategoryAmount = topCat?.Amount ?? 0,
            PeakSingleOutflowTitle = highestTx?.Title,
            PeakSingleOutflowAmount = highestTx?.Amount ?? 0,
            AverageOutflowTicket = avgTicket
        };

        return ApiResponse<MonthlyReportDto>.Ok(report);
    }

    public async Task<ApiResponse<List<ProductPriceReportDto>>> GetProductPriceReportAsync()
    {
        var purchases = await _context.Purchases
            .AsNoTracking()
            .ToListAsync();

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
                        Price = sg.Last().NormalizedPrice
                    })
                    .OrderBy(x => x.Price)
                    .ToList();

                var low = shopGroups.FirstOrDefault();
                var last = recs.LastOrDefault();
                var totalSpent = recs.Sum(r => r.Purchase.TotalPrice);
                var gap = (shopGroups.Count > 1 && last != null && low != null)
                    ? Math.Max(0, last.NormalizedPrice - low.Price)
                    : (decimal?)null;

                return new ProductPriceReportDto
                {
                    ProductName = g.First().ProductName,
                    BaseUnit = g.Key.BaseUnit,
                    ShopsCount = shopGroups.Count,
                    TotalSpent = totalSpent,
                    LowestPrice = low?.Price ?? 0,
                    LowestShop = low?.Shop ?? string.Empty,
                    LastPaidPrice = last?.NormalizedPrice ?? 0,
                    LastShop = last?.Purchase.ShopName ?? string.Empty,
                    GapEstimate = gap
                };
            })
            .OrderByDescending(r => r.TotalSpent)
            .ToList();

        return ApiResponse<List<ProductPriceReportDto>>.Ok(productGroups);
    }

    public async Task<byte[]> ExportProductReportCsvAsync()
    {
        var res = await GetProductPriceReportAsync();
        var list = res.Data ?? new List<ProductPriceReportDto>();

        var sb = new StringBuilder();
        sb.AppendLine("Product,Unit,Shops,TotalSpent,LowestPrice,LowestShop,LastPaid,LastShop,GapEstimate");

        foreach (var r in list)
        {
            var name = EscapeCsv(r.ProductName);
            var unit = EscapeCsv(r.BaseUnit);
            var shops = r.ShopsCount;
            var spent = r.TotalSpent.ToString("0.00");
            var lowPrice = r.LowestPrice.ToString("0.00");
            var lowShop = EscapeCsv(r.LowestShop);
            var lastPaid = r.LastPaidPrice.ToString("0.00");
            var lastShop = EscapeCsv(r.LastShop);
            var gap = r.GapEstimate.HasValue ? r.GapEstimate.Value.ToString("0.00") : "n/a";

            sb.AppendLine($"{name},{unit},{shops},{spent},{lowPrice},{lowShop},{lastPaid},{lastShop},{gap}");
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

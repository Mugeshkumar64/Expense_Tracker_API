using Expense_Tracker.Common;
using Expense_Tracker.Data;
using Expense_Tracker.DTOs;
using Expense_Tracker.DTOs.Common;
using Expense_Tracker.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Expense_Tracker.Services;

public class ProductService : IProductService
{
    private readonly ApplicationDbContext _context;
    private readonly IReportService _reportService;

    public ProductService(ApplicationDbContext context, IReportService reportService)
    {
        _context = context;
        _reportService = reportService;
    }

    public async Task<ApiResponse<List<string>>> GetProductNamesAsync()
    {
        var names = await _context.Purchases
            .AsNoTracking()
            .Select(p => p.ProductName.Trim())
            .Distinct()
            .OrderBy(n => n)
            .ToListAsync();

        return ApiResponse<List<string>>.Ok(names);
    }

    public async Task<ApiResponse<List<ProductPriceReportDto>>> GetProductsAsync()
    {
        return await _reportService.GetProductPriceReportAsync();
    }

    public async Task<ApiResponse<List<ProductPriceHistoryRecordDto>>> GetProductHistoryAsync(string productName)
    {
        if (string.IsNullOrWhiteSpace(productName))
        {
            return ApiResponse<List<ProductPriceHistoryRecordDto>>.Fail("Product name is required.");
        }

        var search = productName.Trim().ToLowerInvariant();
        var purchases = await _context.Purchases
            .AsNoTracking()
            .Where(p => p.ProductName.ToLower() == search)
            .OrderBy(p => p.Date)
            .ThenBy(p => p.CreatedAt)
            .ToListAsync();

        if (!purchases.Any())
        {
            return ApiResponse<List<ProductPriceHistoryRecordDto>>.Ok(new List<ProductPriceHistoryRecordDto>());
        }

        var primary = purchases.First();
        var (baseUnit, _) = UnitConverter.GetUnitInfo(primary.Unit);

        var list = purchases.Select(p => new
        {
            Date = p.Date,
            ShopName = p.ShopName,
            NormalizedPrice = UnitConverter.NormalizePrice(p.UnitPrice, p.Unit),
            BaseUnit = baseUnit
        }).ToList();

        var maxPrice = list.Max(x => x.NormalizedPrice);
        if (maxPrice <= 0) maxPrice = 1m;

        var history = list.Select(x => new ProductPriceHistoryRecordDto
        {
            Date = x.Date,
            ShopName = x.ShopName,
            NormalizedPrice = x.NormalizedPrice,
            BaseUnit = x.BaseUnit,
            PercentageOfMax = Math.Round((x.NormalizedPrice / maxPrice) * 100, 1)
        }).ToList();

        return ApiResponse<List<ProductPriceHistoryRecordDto>>.Ok(history);
    }
}

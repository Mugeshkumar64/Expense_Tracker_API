using Expense_Tracker.DTOs;
using Expense_Tracker.DTOs.Common;

namespace Expense_Tracker.Interfaces;

public interface IProductService
{
    Task<ApiResponse<List<string>>> GetProductNamesAsync();
    Task<ApiResponse<List<ProductPriceReportDto>>> GetProductsAsync();
    Task<ApiResponse<List<ProductPriceHistoryRecordDto>>> GetProductHistoryAsync(string productName);
}

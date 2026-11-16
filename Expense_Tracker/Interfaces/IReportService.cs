using Expense_Tracker.DTOs;
using Expense_Tracker.DTOs.Common;

namespace Expense_Tracker.Interfaces;

public interface IReportService
{
    Task<ApiResponse<List<string>>> GetAvailableMonthsAsync();
    Task<ApiResponse<MonthlyReportDto>> GetMonthlyReportAsync(string? month = null);
    Task<ApiResponse<List<ProductPriceReportDto>>> GetProductPriceReportAsync();
    Task<byte[]> ExportProductReportCsvAsync();
}

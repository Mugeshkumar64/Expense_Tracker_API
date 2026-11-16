using Expense_Tracker.DTOs;
using Expense_Tracker.DTOs.Common;

namespace Expense_Tracker.Interfaces;

public interface IPriceComparisonService
{
    Task<ApiResponse<PriceComparisonResultDto>> CompareProductPricesAsync(string productName, decimal plannedQuantity = 1);
}

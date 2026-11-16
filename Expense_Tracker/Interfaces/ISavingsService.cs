using Expense_Tracker.DTOs;
using Expense_Tracker.DTOs.Common;

namespace Expense_Tracker.Interfaces;

public interface ISavingsService
{
    Task<ApiResponse<SavingsSummaryDto>> GetSavingsSummaryAsync();
}

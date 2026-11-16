using Expense_Tracker.DTOs;
using Expense_Tracker.DTOs.Common;

namespace Expense_Tracker.Interfaces;

public interface IPurchaseService
{
    Task<ApiResponse<List<PurchaseDto>>> GetAllPurchasesAsync(string? search = null);
    Task<ApiResponse<PurchaseDto>> GetPurchaseByIdAsync(int id);
    Task<ApiResponse<PurchaseDto>> CreatePurchaseAsync(CreatePurchaseDto dto, int? listItemId = null);
    Task<ApiResponse<PurchaseDto>> UpdatePurchaseAsync(int id, UpdatePurchaseDto dto);
    Task<ApiResponse> DeletePurchaseAsync(int id);
    Task<byte[]> ExportCsvAsync();
}

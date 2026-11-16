using Expense_Tracker.DTOs;
using Expense_Tracker.DTOs.Common;

namespace Expense_Tracker.Interfaces;

public interface IShoppingListService
{
    Task<ApiResponse<ShoppingListSummaryDto>> GetShoppingListSummaryAsync();
    Task<ApiResponse<ShoppingListItemDto>> AddItemAsync(CreateShoppingListItemDto dto);
    Task<ApiResponse> RemoveItemAsync(int id);
    Task<ApiResponse<PurchaseDto>> BuyItemAsync(int id, CreatePurchaseDto purchaseDto);
}

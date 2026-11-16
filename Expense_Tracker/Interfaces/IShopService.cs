using Expense_Tracker.DTOs;
using Expense_Tracker.DTOs.Common;

namespace Expense_Tracker.Interfaces;

public interface IShopService
{
    Task<ApiResponse<List<ShopDto>>> GetAllShopsAsync();
    Task<ApiResponse<ShopDto>> GetShopByIdAsync(int id);
    Task<ApiResponse<ShopDto>> CreateShopAsync(CreateShopDto dto);
    Task<ApiResponse<ShopDto>> UpdateShopAsync(int id, UpdateShopDto dto);
    Task<ApiResponse> DeleteShopAsync(int id);
}

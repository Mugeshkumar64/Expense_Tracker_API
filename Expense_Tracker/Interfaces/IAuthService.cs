using Expense_Tracker.DTOs;
using Expense_Tracker.DTOs.Common;

namespace Expense_Tracker.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<UserResponseDto>> RegisterAsync(RegisterDto dto);
    Task<ApiResponse<UserResponseDto>> LoginAsync(LoginDto dto);
    Task<ApiResponse<UserResponseDto>> GetCurrentUserAsync(string email);
}

using Expense_Tracker.DTOs;
using Expense_Tracker.DTOs.Common;

namespace Expense_Tracker.Interfaces;

public interface ISettingsService
{
    Task<ApiResponse<UserSettingsDto>> GetSettingsAsync();
    Task<ApiResponse<UserSettingsDto>> UpdateSettingsAsync(UpdateSettingsDto dto);
    Task<ApiResponse> LoadDemoDataAsync();
    Task<ApiResponse> ClearDemoDataAsync();
    Task<ApiResponse<BackupDataDto>> ExportBackupAsync();
    Task<ApiResponse> ImportBackupAsync(BackupDataDto backupData);
    Task<ApiResponse> ResetDatabaseAsync();
}

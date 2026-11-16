using Expense_Tracker.DTOs;
using Expense_Tracker.DTOs.Common;

namespace Expense_Tracker.Interfaces;

public interface ITransactionService
{
    Task<ApiResponse<List<TransactionDto>>> GetTransactionsAsync(TransactionFilterDto filter);
    Task<ApiResponse<TransactionDto>> GetTransactionByIdAsync(int id);
    Task<ApiResponse<TransactionDto>> CreateTransactionAsync(CreateTransactionDto dto);
    Task<ApiResponse<TransactionDto>> UpdateTransactionAsync(int id, UpdateTransactionDto dto);
    Task<ApiResponse> DeleteTransactionAsync(int id);
    Task<byte[]> ExportCsvAsync(TransactionFilterDto filter);
}

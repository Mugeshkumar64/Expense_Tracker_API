namespace Expense_Tracker.Models;

public class Transaction
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = "expense"; // "income" or "expense"
    
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public DateTime Date { get; set; }
    public string PaymentMethod { get; set; } = "UPI"; // UPI, Credit Card, Debit Card, Bank Transfer, Cash, Other
    public string? Notes { get; set; }
    public string? Shop { get; set; }

    public int? PurchaseId { get; set; }
    public Purchase? Purchase { get; set; }

    public bool IsDemo { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace Expense_Tracker.DTOs;

public class TransactionDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Type { get; set; } = "expense";
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string PaymentMethod { get; set; } = "UPI";
    public string? Notes { get; set; }
    public string? Shop { get; set; }
    public int? PurchaseId { get; set; }
    public bool IsDemo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateTransactionDto
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 200 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, 1000000000, ErrorMessage = "Amount must be greater than zero")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Type is required")]
    [RegularExpression("^(expense|income)$", ErrorMessage = "Type must be either 'expense' or 'income'")]
    public string Type { get; set; } = "expense";

    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }

    [Required(ErrorMessage = "Date is required")]
    public DateTime Date { get; set; }

    [Required(ErrorMessage = "Payment method is required")]
    public string PaymentMethod { get; set; } = "UPI";

    public string? Notes { get; set; }
    public string? Shop { get; set; }
}

public class UpdateTransactionDto
{
    [Required(ErrorMessage = "Title is required")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 200 characters")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Amount is required")]
    [Range(0.01, 1000000000, ErrorMessage = "Amount must be greater than zero")]
    public decimal Amount { get; set; }

    [Required(ErrorMessage = "Type is required")]
    [RegularExpression("^(expense|income)$", ErrorMessage = "Type must be either 'expense' or 'income'")]
    public string Type { get; set; } = "expense";

    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }

    [Required(ErrorMessage = "Date is required")]
    public DateTime Date { get; set; }

    [Required(ErrorMessage = "Payment method is required")]
    public string PaymentMethod { get; set; } = "UPI";

    public string? Notes { get; set; }
    public string? Shop { get; set; }
}

public class TransactionFilterDto
{
    public string? Type { get; set; } = "all"; // all, income, expense
    public string? Category { get; set; } = "all";
    public string? Shop { get; set; } = "all";
    public string? DateRange { get; set; } = "all"; // all, today, week, month, lastmonth
    public string? Search { get; set; }
    public string? Sort { get; set; } = "new"; // new, old, high, low, shop
}

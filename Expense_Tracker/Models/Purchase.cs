namespace Expense_Tracker.Models;

public class Purchase
{
    public int Id { get; set; }
    public string ProductName { get; set; } = string.Empty;

    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public decimal Quantity { get; set; }
    public string Unit { get; set; } = "kg"; // piece, kg, g, litre, ml, packet, dozen
    public decimal UnitPrice { get; set; }
    public decimal TotalPrice { get; set; }

    public int? ShopId { get; set; }
    public Shop? Shop { get; set; }
    public string ShopName { get; set; } = string.Empty;

    public string PaymentMethod { get; set; } = "UPI";
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public bool FromList { get; set; }
    public bool IsDemo { get; set; }

    public int? TransactionId { get; set; }
    public Transaction? Transaction { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

namespace Expense_Tracker.Models;

public class UserSettings
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public User? User { get; set; }
    public string Currency { get; set; } = "₹";
    public decimal MonthBudget { get; set; } = 0;
    public decimal ShopBudget { get; set; } = 0;
    public decimal ListBudget { get; set; } = 0;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

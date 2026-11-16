using System.ComponentModel.DataAnnotations;

namespace Expense_Tracker.DTOs;

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "expense";
    public DateTime CreatedAt { get; set; }
    public int TotalEntries { get; set; }
    public decimal TotalAmount { get; set; }
}

public class CreateCategoryDto
{
    [Required(ErrorMessage = "Category name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Category name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category type is required")]
    [RegularExpression("^(expense|income)$", ErrorMessage = "Type must be either 'expense' or 'income'")]
    public string Type { get; set; } = "expense";
}

public class UpdateCategoryDto
{
    [Required(ErrorMessage = "Category name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Category name must be between 2 and 100 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Category type is required")]
    [RegularExpression("^(expense|income)$", ErrorMessage = "Type must be either 'expense' or 'income'")]
    public string Type { get; set; } = "expense";
}

using Expense_Tracker.Models;
using Microsoft.EntityFrameworkCore;

namespace Expense_Tracker.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        await context.Database.MigrateAsync();

        // Seed default categories if none exist
        if (!await context.Categories.AnyAsync())
        {
            var defaultExpenseCategories = new[]
            {
                "Food", "Transport", "Shopping", "Bills", "Entertainment",
                "Health", "Education", "Groceries", "Healthcare", "Other"
            };

            var defaultIncomeCategories = new[]
            {
                "Salary", "Freelance", "Business", "Investment", "Other"
            };

            foreach (var cat in defaultExpenseCategories)
            {
                context.Categories.Add(new Category
                {
                    Name = cat,
                    Type = "expense",
                    CreatedAt = DateTime.UtcNow
                });
            }

            foreach (var cat in defaultIncomeCategories)
            {
                // In case "Other" already exists as expense, EF composite index is (Name, Type), so "Other" (income) is allowed
                context.Categories.Add(new Category
                {
                    Name = cat,
                    Type = "income",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await context.SaveChangesAsync();
        }

        // Seed default settings if none exist
        if (!await context.UserSettings.AnyAsync())
        {
            context.UserSettings.Add(new UserSettings
            {
                Currency = "₹",
                MonthBudget = 0,
                ShopBudget = 0,
                ListBudget = 0,
                UpdatedAt = DateTime.UtcNow
            });

            await context.SaveChangesAsync();
        }
    }
}

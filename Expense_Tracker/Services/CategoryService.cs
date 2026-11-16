using Expense_Tracker.Data;
using Expense_Tracker.DTOs;
using Expense_Tracker.DTOs.Common;
using Expense_Tracker.Interfaces;
using Expense_Tracker.Models;
using Microsoft.EntityFrameworkCore;

namespace Expense_Tracker.Services;

public class CategoryService : ICategoryService
{
    private readonly ApplicationDbContext _context;

    public CategoryService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<CategoryDto>>> GetAllCategoriesAsync(string? type = null, string? search = null)
    {
        var query = _context.Categories
            .Include(c => c.Transactions)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(type) && !type.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(c => c.Type.ToLower() == type.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(c => c.Name.ToLower().Contains(searchLower));
        }

        var categories = await query
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Type = c.Type,
                CreatedAt = c.CreatedAt,
                TotalEntries = c.Transactions.Count,
                TotalAmount = c.Transactions.Sum(t => t.Amount)
            })
            .ToListAsync();

        return ApiResponse<List<CategoryDto>>.Ok(categories);
    }

    public async Task<ApiResponse<CategoryDto>> GetCategoryByIdAsync(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Transactions)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            return ApiResponse<CategoryDto>.Fail("Category not found.");
        }

        var dto = new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Type = category.Type,
            CreatedAt = category.CreatedAt,
            TotalEntries = category.Transactions.Count,
            TotalAmount = category.Transactions.Sum(t => t.Amount)
        };

        return ApiResponse<CategoryDto>.Ok(dto);
    }

    public async Task<ApiResponse<CategoryDto>> CreateCategoryAsync(CreateCategoryDto dto)
    {
        var name = dto.Name.Trim();
        var type = dto.Type.Trim().ToLowerInvariant();

        var exists = await _context.Categories.AnyAsync(c =>
            c.Name.ToLower() == name.ToLower() && c.Type.ToLower() == type);

        if (exists)
        {
            return ApiResponse<CategoryDto>.Fail($"Category '{name}' of type '{type}' already exists.");
        }

        var category = new Category
        {
            Name = name,
            Type = type,
            CreatedAt = DateTime.UtcNow
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var result = new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Type = category.Type,
            CreatedAt = category.CreatedAt,
            TotalEntries = 0,
            TotalAmount = 0
        };

        return ApiResponse<CategoryDto>.Ok(result, "Category created successfully.");
    }

    public async Task<ApiResponse<CategoryDto>> UpdateCategoryAsync(int id, UpdateCategoryDto dto)
    {
        var category = await _context.Categories
            .Include(c => c.Transactions)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            return ApiResponse<CategoryDto>.Fail("Category not found.");
        }

        var name = dto.Name.Trim();
        var type = dto.Type.Trim().ToLowerInvariant();

        var duplicate = await _context.Categories.AnyAsync(c =>
            c.Id != id && c.Name.ToLower() == name.ToLower() && c.Type.ToLower() == type);

        if (duplicate)
        {
            return ApiResponse<CategoryDto>.Fail($"Another category '{name}' of type '{type}' already exists.");
        }

        category.Name = name;
        category.Type = type;
        category.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var result = new CategoryDto
        {
            Id = category.Id,
            Name = category.Name,
            Type = category.Type,
            CreatedAt = category.CreatedAt,
            TotalEntries = category.Transactions.Count,
            TotalAmount = category.Transactions.Sum(t => t.Amount)
        };

        return ApiResponse<CategoryDto>.Ok(result, "Category updated successfully.");
    }

    public async Task<ApiResponse> DeleteCategoryAsync(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Transactions)
            .Include(c => c.Purchases)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (category == null)
        {
            return ApiResponse.Fail("Category not found.");
        }

        if (category.Transactions.Any() || category.Purchases.Any())
        {
            return ApiResponse.Fail("Category is currently linked to records and cannot be deleted.");
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return ApiResponse.Ok("Category deleted successfully.");
    }

    public async Task<ApiResponse> DeleteCategoryByNameAsync(string name)
    {
        var trimmedName = name.Trim().ToLowerInvariant();
        var category = await _context.Categories
            .Include(c => c.Transactions)
            .Include(c => c.Purchases)
            .FirstOrDefaultAsync(c => c.Name.ToLower() == trimmedName);

        if (category == null)
        {
            return ApiResponse.Fail("Category not found.");
        }

        if (category.Transactions.Any() || category.Purchases.Any())
        {
            return ApiResponse.Fail("Category is currently linked to records and cannot be deleted.");
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return ApiResponse.Ok("Category deleted successfully.");
    }
}

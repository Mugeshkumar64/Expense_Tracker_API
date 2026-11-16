using Expense_Tracker.DTOs;
using Expense_Tracker.DTOs.Common;
using Expense_Tracker.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Expense_Tracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetAll([FromQuery] string? type, [FromQuery] string? search)
    {
        var result = await _categoryService.GetAllCategoriesAsync(type, search);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> GetById(int id)
    {
        var result = await _categoryService.GetCategoryByIdAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> Create([FromBody] CreateCategoryDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<CategoryDto>.Fail("Validation failed", errors));
        }

        var result = await _categoryService.CreateCategoryAsync(dto);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> Update(int id, [FromBody] UpdateCategoryDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<CategoryDto>.Fail("Validation failed", errors));
        }

        var result = await _categoryService.UpdateCategoryAsync(id, dto);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        var result = await _categoryService.DeleteCategoryAsync(id);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    [HttpDelete("by-name/{name}")]
    public async Task<ActionResult<ApiResponse>> DeleteByName(string name)
    {
        var result = await _categoryService.DeleteCategoryByNameAsync(name);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}

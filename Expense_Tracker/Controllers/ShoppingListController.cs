using Expense_Tracker.DTOs;
using Expense_Tracker.DTOs.Common;
using Expense_Tracker.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Expense_Tracker.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/shopping-list")]
public class ShoppingListController : ControllerBase
{
    private readonly IShoppingListService _shoppingListService;

    public ShoppingListController(IShoppingListService shoppingListService)
    {
        _shoppingListService = shoppingListService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<ShoppingListSummaryDto>>> GetSummary()
    {
        var result = await _shoppingListService.GetShoppingListSummaryAsync();
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<ShoppingListItemDto>>> AddItem([FromBody] CreateShoppingListItemDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<ShoppingListItemDto>.Fail("Validation failed", errors));
        }

        var result = await _shoppingListService.AddItemAsync(dto);
        return CreatedAtAction(nameof(GetSummary), result);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse>> RemoveItem(int id)
    {
        var result = await _shoppingListService.RemoveItemAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpPost("{id:int}/buy")]
    public async Task<ActionResult<ApiResponse<PurchaseDto>>> BuyItem(int id, [FromBody] CreatePurchaseDto purchaseDto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<PurchaseDto>.Fail("Validation failed", errors));
        }

        var result = await _shoppingListService.BuyItemAsync(id, purchaseDto);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }
}

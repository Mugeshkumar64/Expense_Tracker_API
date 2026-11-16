using Expense_Tracker.DTOs;
using Expense_Tracker.DTOs.Common;
using Expense_Tracker.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Expense_Tracker.Controllers;

[ApiController]
[Route("api/[controller]")]
[Route("api/price-comparison")]
public class PriceComparisonController : ControllerBase
{
    private readonly IPriceComparisonService _priceComparisonService;

    public PriceComparisonController(IPriceComparisonService priceComparisonService)
    {
        _priceComparisonService = priceComparisonService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PriceComparisonResultDto>>> Compare([FromQuery] string product, [FromQuery] decimal quantity = 1)
    {
        if (string.IsNullOrWhiteSpace(product))
        {
            return BadRequest(ApiResponse<PriceComparisonResultDto>.Fail("Product name query parameter is required."));
        }

        var result = await _priceComparisonService.CompareProductPricesAsync(product, quantity);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }
}

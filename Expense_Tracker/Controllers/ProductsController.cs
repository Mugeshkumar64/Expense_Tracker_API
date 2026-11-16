using Expense_Tracker.DTOs;
using Expense_Tracker.DTOs.Common;
using Expense_Tracker.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Expense_Tracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<ProductPriceReportDto>>>> GetAll()
    {
        var result = await _productService.GetProductsAsync();
        return Ok(result);
    }

    [HttpGet("names")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetNames()
    {
        var result = await _productService.GetProductNamesAsync();
        return Ok(result);
    }

    [HttpGet("{name}/history")]
    public async Task<ActionResult<ApiResponse<List<ProductPriceHistoryRecordDto>>>> GetHistory(string name)
    {
        var result = await _productService.GetProductHistoryAsync(name);
        return Ok(result);
    }
}

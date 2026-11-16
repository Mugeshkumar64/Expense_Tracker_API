using Expense_Tracker.DTOs;
using Expense_Tracker.DTOs.Common;
using Expense_Tracker.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Expense_Tracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet("available-months")]
    public async Task<ActionResult<ApiResponse<List<string>>>> GetAvailableMonths()
    {
        var result = await _reportService.GetAvailableMonthsAsync();
        return Ok(result);
    }

    [HttpGet("monthly")]
    public async Task<ActionResult<ApiResponse<MonthlyReportDto>>> GetMonthlyReport([FromQuery] string? month)
    {
        var result = await _reportService.GetMonthlyReportAsync(month);
        return Ok(result);
    }

    [HttpGet("products")]
    public async Task<ActionResult<ApiResponse<List<ProductPriceReportDto>>>> GetProductPriceReport()
    {
        var result = await _reportService.GetProductPriceReportAsync();
        return Ok(result);
    }

    [HttpGet("products/export-csv")]
    public async Task<IActionResult> ExportProductPriceReportCsv()
    {
        var csvBytes = await _reportService.ExportProductReportCsvAsync();
        return File(csvBytes, "text/csv", $"product_price_report_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
    }
}

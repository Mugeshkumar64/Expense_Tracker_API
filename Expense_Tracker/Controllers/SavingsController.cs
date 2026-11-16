using Expense_Tracker.DTOs;
using Expense_Tracker.DTOs.Common;
using Expense_Tracker.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Expense_Tracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SavingsController : ControllerBase
{
    private readonly ISavingsService _savingsService;

    public SavingsController(ISavingsService savingsService)
    {
        _savingsService = savingsService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<SavingsSummaryDto>>> GetSummary()
    {
        var result = await _savingsService.GetSavingsSummaryAsync();
        return Ok(result);
    }
}

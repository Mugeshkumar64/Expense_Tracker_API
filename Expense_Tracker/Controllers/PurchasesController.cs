using Expense_Tracker.DTOs;
using Expense_Tracker.DTOs.Common;
using Expense_Tracker.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Expense_Tracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PurchasesController : ControllerBase
{
    private readonly IPurchaseService _purchaseService;

    public PurchasesController(IPurchaseService purchaseService)
    {
        _purchaseService = purchaseService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PurchaseDto>>>> GetAll([FromQuery] string? search)
    {
        var result = await _purchaseService.GetAllPurchasesAsync(search);
        return Ok(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<PurchaseDto>>> GetById(int id)
    {
        var result = await _purchaseService.GetPurchaseByIdAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PurchaseDto>>> Create([FromBody] CreatePurchaseDto dto, [FromQuery] int? listItemId)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<PurchaseDto>.Fail("Validation failed", errors));
        }

        var result = await _purchaseService.CreatePurchaseAsync(dto, listItemId);
        if (!result.Success)
        {
            return BadRequest(result);
        }

        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<PurchaseDto>>> Update(int id, [FromBody] UpdatePurchaseDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<PurchaseDto>.Fail("Validation failed", errors));
        }

        var result = await _purchaseService.UpdatePurchaseAsync(id, dto);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse>> Delete(int id)
    {
        var result = await _purchaseService.DeletePurchaseAsync(id);
        if (!result.Success)
        {
            return NotFound(result);
        }

        return Ok(result);
    }

    [HttpGet("export-csv")]
    public async Task<IActionResult> ExportCsv()
    {
        var csvBytes = await _purchaseService.ExportCsvAsync();
        return File(csvBytes, "text/csv", $"purchases_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv");
    }
}

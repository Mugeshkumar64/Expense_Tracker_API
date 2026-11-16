using Expense_Tracker.DTOs;
using Expense_Tracker.DTOs.Common;
using Expense_Tracker.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Expense_Tracker.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SettingsController : ControllerBase
{
    private readonly ISettingsService _settingsService;

    public SettingsController(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<UserSettingsDto>>> GetSettings()
    {
        var result = await _settingsService.GetSettingsAsync();
        return Ok(result);
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<UserSettingsDto>>> UpdateSettings([FromBody] UpdateSettingsDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<UserSettingsDto>.Fail("Validation failed", errors));
        }

        var result = await _settingsService.UpdateSettingsAsync(dto);
        return Ok(result);
    }

    [HttpPost("demo/load")]
    public async Task<ActionResult<ApiResponse>> LoadDemo()
    {
        var result = await _settingsService.LoadDemoDataAsync();
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("demo/clear")]
    public async Task<ActionResult<ApiResponse>> ClearDemo()
    {
        var result = await _settingsService.ClearDemoDataAsync();
        return Ok(result);
    }

    [HttpGet("backup/export")]
    public async Task<ActionResult<ApiResponse<BackupDataDto>>> ExportBackup()
    {
        var result = await _settingsService.ExportBackupAsync();
        return Ok(result);
    }

    [HttpPost("backup/import")]
    public async Task<ActionResult<ApiResponse>> ImportBackup([FromBody] BackupDataDto backupData)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse.Fail("Validation failed", errors));
        }

        var result = await _settingsService.ImportBackupAsync(backupData);
        if (!result.Success)
        {
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpPost("reset")]
    public async Task<ActionResult<ApiResponse>> ResetDatabase()
    {
        var result = await _settingsService.ResetDatabaseAsync();
        return Ok(result);
    }
}

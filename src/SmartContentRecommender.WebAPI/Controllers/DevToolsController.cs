using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartContentRecommender.Domain.Entities;
using SmartContentRecommender.Domain.Enums;
using SmartContentRecommender.Infrastructure.Persistence;

namespace SmartContentRecommender.WebAPI.Controllers;

[ApiController]
[Route("api/dev")]
public class DevToolsController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly ApplicationDbContext _db;

    public DevToolsController(IWebHostEnvironment env, ApplicationDbContext db)
    {
        _env = env;
        _db = db;
    }

    [HttpGet("status")]
    [AllowAnonymous]
    public IActionResult Status()
    {
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }

        return Ok(new { isDevelopment = true, demoHistorySupported = true });
    }

    [HttpPost("generate-demo-history")]
    [Authorize]
    public async Task<IActionResult> GenerateDemoHistory(CancellationToken cancellationToken)
    {
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }

        var userId = GetCurrentUserId();
        if (userId is null)
        {
            return Unauthorized("Не удалось определить пользователя из JWT токена.");
        }

        var contents = await _db.Contents
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAtUtc)
            .Take(10)
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);

        if (contents.Count == 0)
        {
            return BadRequest("Нет контента для генерации истории.");
        }

        var existingCount = await _db.UserActions
            .AsNoTracking()
            .CountAsync(a => a.UserId == userId.Value, cancellationToken);

        if (existingCount >= 20)
        {
            return Ok(new { Message = "У пользователя уже достаточно истории.", Added = 0, Total = existingCount });
        }

        var toAdd = Math.Min(20 - existingCount, 12);
        var now = DateTime.UtcNow;
        var actions = new List<UserAction>();

        for (var i = 0; i < toAdd; i++)
        {
            var contentId = contents[i % contents.Count];
            var type = i % 3 == 0 ? UserActionType.Like : i % 3 == 1 ? UserActionType.Click : UserActionType.View;
            actions.Add(new UserAction
            {
                UserId = userId.Value,
                ContentId = contentId,
                Type = type,
                CreatedAtUtc = now.AddMinutes(-i)
            });
        }

        _db.UserActions.AddRange(actions);
        await _db.SaveChangesAsync(cancellationToken);

        var total = existingCount + actions.Count;
        return Ok(new { Message = "Демо-история добавлена.", Added = actions.Count, Total = total });
    }

    private Guid? GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}


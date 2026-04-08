using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartContentRecommender.Infrastructure.Persistence;

namespace SmartContentRecommender.WebAPI.Controllers;

[ApiController]
[Route("api/admin/db")]
[Authorize(Roles = "Admin")]
public class AdminDbController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminDbController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> Overview(CancellationToken cancellationToken)
    {
        var users = await _db.Users.AsNoTracking().CountAsync(cancellationToken);
        var categories = await _db.Categories.AsNoTracking().CountAsync(cancellationToken);
        var tags = await _db.Tags.AsNoTracking().CountAsync(cancellationToken);
        var contents = await _db.Contents.AsNoTracking().CountAsync(cancellationToken);
        var actions = await _db.UserActions.AsNoTracking().CountAsync(cancellationToken);

        return Ok(new { users, categories, tags, contents, actions });
    }

    [HttpGet("users")]
    public async Task<IActionResult> Users([FromQuery] int limit = 200, CancellationToken cancellationToken = default)
    {
        var safe = Math.Clamp(limit, 1, 1000);
        var rows = await _db.Users.AsNoTracking()
            .OrderBy(u => u.Email)
            .Take(safe)
            .ToListAsync(cancellationToken);
        var items = rows.Select(u => new { u.Id, u.Email, Role = u.Role.ToString(), u.IsBlocked, u.CreatedAtUtc });

        return Ok(items);
    }

    [HttpGet("categories")]
    public async Task<IActionResult> Categories([FromQuery] int limit = 200, CancellationToken cancellationToken = default)
    {
        var safe = Math.Clamp(limit, 1, 1000);
        var items = await _db.Categories.AsNoTracking()
            .OrderBy(c => c.Name)
            .Take(safe)
            .Select(c => new { c.Id, c.Name })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("tags")]
    public async Task<IActionResult> Tags([FromQuery] int limit = 200, CancellationToken cancellationToken = default)
    {
        var safe = Math.Clamp(limit, 1, 1000);
        var items = await _db.Tags.AsNoTracking()
            .OrderBy(t => t.Name)
            .Take(safe)
            .Select(t => new { t.Id, t.Name })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("contents")]
    public async Task<IActionResult> Contents([FromQuery] int limit = 200, CancellationToken cancellationToken = default)
    {
        var safe = Math.Clamp(limit, 1, 1000);
        var items = await _db.Contents.AsNoTracking()
            .Include(c => c.Category)
            .OrderByDescending(c => c.CreatedAtUtc)
            .Take(safe)
            .Select(c => new
            {
                c.Id,
                c.Title,
                c.Url,
                c.CreatedAtUtc,
                Category = c.Category != null ? c.Category.Name : ""
            })
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("actions")]
    public async Task<IActionResult> Actions([FromQuery] int limit = 200, CancellationToken cancellationToken = default)
    {
        var safe = Math.Clamp(limit, 1, 1000);
        var rows = await _db.UserActions.AsNoTracking()
            .Include(a => a.User)
            .Include(a => a.Content)
            .OrderByDescending(a => a.CreatedAtUtc)
            .Take(safe)
            .ToListAsync(cancellationToken);
        var items = rows.Select(a => new
        {
            a.Id,
            UserEmail = a.User != null ? a.User.Email : "",
            ContentTitle = a.Content != null ? a.Content.Title : "",
            Type = a.Type.ToString(),
            a.CreatedAtUtc
        });

        return Ok(items);
    }
}


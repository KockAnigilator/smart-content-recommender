using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartContentRecommender.Application.Admin.Interfaces;
using SmartContentRecommender.Application.Admin.Models;

namespace SmartContentRecommender.WebAPI.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly IUserAdminService _userAdminService;

    public AdminUsersController(IUserAdminService userAdminService)
    {
        _userAdminService = userAdminService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var users = await _userAdminService.GetAllUsersAsync(cancellationToken);
        return Ok(users);
    }

    [HttpPut("{id:guid}/role")]
    public async Task<IActionResult> ChangeRole(Guid id, [FromBody] ChangeUserRoleRequest request, CancellationToken cancellationToken)
    {
        var changed = await _userAdminService.ChangeRoleAsync(id, request.Role, cancellationToken);
        return changed ? Ok(new { Message = "Роль пользователя обновлена." }) : NotFound();
    }

    [HttpPut("{id:guid}/block")]
    public async Task<IActionResult> SetBlocked(Guid id, [FromBody] SetUserBlockRequest request, CancellationToken cancellationToken)
    {
        var changed = await _userAdminService.SetBlockedAsync(id, request.IsBlocked, cancellationToken);
        return changed ? Ok(new { Message = "Статус блокировки пользователя обновлен." }) : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _userAdminService.DeleteUserAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}


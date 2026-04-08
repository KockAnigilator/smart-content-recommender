using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SmartContentRecommender.WebClient.Models;
using SmartContentRecommender.WebClient.Services;

namespace SmartContentRecommender.WebClient.Controllers;

public class HomeController : Controller
{
    private readonly ScrApiClient _api;
    private readonly ITokenStore _tokenStore;

    public HomeController(ScrApiClient api, ITokenStore tokenStore)
    {
        _api = api;
        _tokenStore = tokenStore;
    }

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var model = new HomeIndexViewModel();
        model.Error = TempData["Error"] as string;
        model.Info = TempData["Info"] as string;

        model.ApiOnline = await _api.ApiAvailabilityCheckAsync(cancellationToken);

        var token = _tokenStore.GetToken();
        model.IsAuthenticated = !string.IsNullOrWhiteSpace(token);
        model.Role = _tokenStore.GetRole() ?? "Guest";

        try
        {
            model.Contents = await _api.GetContentAsync(cancellationToken);
            model.Popular = await _api.GetPopularAsync(cancellationToken);

            if (!model.IsAuthenticated)
            {
                return View(model);
            }

            var me = await _api.GetMeAsync(cancellationToken);
            if (me is null || string.IsNullOrWhiteSpace(me.Role))
            {
                _tokenStore.Clear();
                model.IsAuthenticated = false;
                model.Role = "Guest";
                model.ByCategories = [];
                model.Knn = [];
                model.AdminUsers = [];
                return View(model);
            }

            model.Role = me.Role ?? "User";
            _tokenStore.SetRole(model.Role);

            model.ByCategories = await _api.GetByCategoriesAsync(cancellationToken);
            model.Knn = await _api.GetKnnAsync(cancellationToken);

            if (model.Role.Equals("Admin", StringComparison.OrdinalIgnoreCase))
            {
                model.AdminUsers = await _api.GetAdminUsersAsync(cancellationToken);
            }
        }
        catch
        {
            // For UI stability on demo: keep page open, show message to user.
            model.Error ??= "Не удалось загрузить данные. Проверьте доступность API.";
        }

        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> LogAction(Guid contentId, int type, CancellationToken cancellationToken)
    {
        var token = _tokenStore.GetToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            return RedirectToAction(nameof(Index));
        }

        var success = await _api.LogActionAsync(contentId, type, cancellationToken);
        if (!success)
        {
            TempData["Error"] = "Не удалось сохранить действие.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> ChangeRole(Guid userId, string role, CancellationToken cancellationToken)
    {
        var currentRole = _tokenStore.GetRole();
        if (string.IsNullOrWhiteSpace(currentRole) || !currentRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var success = await _api.ChangeRoleAsync(userId, role, cancellationToken);
        if (!success)
        {
            TempData["Error"] = "Не удалось изменить роль пользователя.";
        }
        else
        {
            TempData["Info"] = "Роль пользователя обновлена.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SetBlocked(Guid userId, bool isBlocked, CancellationToken cancellationToken)
    {
        var currentRole = _tokenStore.GetRole();
        if (string.IsNullOrWhiteSpace(currentRole) || !currentRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var success = await _api.SetBlockedAsync(userId, isBlocked, cancellationToken);
        if (!success)
        {
            TempData["Error"] = "Не удалось обновить блокировку.";
        }
        else
        {
            TempData["Info"] = "Статус блокировки обновлён.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUser(Guid userId, CancellationToken cancellationToken)
    {
        var currentRole = _tokenStore.GetRole();
        if (string.IsNullOrWhiteSpace(currentRole) || !currentRole.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            return Forbid();
        }

        var success = await _api.DeleteUserAsync(userId, cancellationToken);
        if (!success)
        {
            TempData["Error"] = "Не удалось удалить пользователя.";
        }
        else
        {
            TempData["Info"] = "Пользователь удалён.";
        }

        return RedirectToAction(nameof(Index));
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

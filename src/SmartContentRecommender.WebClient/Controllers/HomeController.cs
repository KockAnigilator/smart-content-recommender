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

    public async Task<IActionResult> Index(
        Guid? metricsUserId = null,
        string metricsAlgorithm = "knn",
        string activeTab = "content",
        string activeRecTab = "popular-rec",
        CancellationToken cancellationToken = default)
    {
        var model = new HomeIndexViewModel();
        model.Error = TempData["Error"] as string;
        model.Info = TempData["Info"] as string;

        model.ApiOnline = await _api.ApiAvailabilityCheckAsync(cancellationToken);
        if (!model.ApiOnline)
        {
            model.Info ??= "API сейчас недоступен. Сначала запустите WebAPI, затем обновите страницу.";
            return View(model);
        }

        var token = _tokenStore.GetToken();
        model.IsAuthenticated = !string.IsNullOrWhiteSpace(token);
        model.Role = NormalizeRole(_tokenStore.GetRole());
        model.IsAdmin = IsAdminRole(model.Role);
        model.ShowDemoHistoryButton = false;
        model.ActiveTab = activeTab;
        model.ActiveRecTab = activeRecTab;

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
                model.IsAdmin = false;
                model.ByCategories = [];
                model.Knn = [];
                model.AdminUsers = [];
                return View(model);
            }

            model.Role = NormalizeRole(me.Role);
            model.IsAdmin = IsAdminRole(model.Role);
            _tokenStore.SetRole(model.Role);

            model.ByCategories = await _api.GetByCategoriesAsync(cancellationToken);
            model.Knn = await _api.GetKnnAsync(cancellationToken);
            try
            {
                model.InterestProfile = await _api.GetInterestProfileAsync(cancellationToken);
            }
            catch (HttpRequestException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
            {
                model.Error ??= "Профиль интересов: нет доступа. Перелогиньтесь.";
            }

            try
            {
                model.ExplainKnn = await _api.GetExplainAsync("knn", 10, cancellationToken);
            }
            catch (HttpRequestException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
            {
                model.Error ??= "Explainability: нет доступа. Перелогиньтесь.";
            }
            if (model.InterestProfile is null)
            {
                model.Info ??= "Профиль интересов пуст. Сделайте несколько действий View/Like/Click и обновите страницу.";
            }

            if (model.IsAdmin)
            {
                model.AdminUsers = await _api.GetAdminUsersAsync(cancellationToken);
                model.Categories = await _api.GetCategoriesAsync(cancellationToken);
                model.Tags = await _api.GetTagsAsync(cancellationToken);
                if (model.AdminUsers.Count == 0)
                {
                    model.Info ??= "Список пользователей пуст. Проверьте seed-данные и доступ к API admin/users.";
                }

                var selectedUserId = metricsUserId ?? model.AdminUsers.FirstOrDefault()?.Id;
                if (selectedUserId.HasValue && selectedUserId.Value != Guid.Empty)
                {
                    model.SelectedMetricsUserId = selectedUserId;
                    model.SelectedMetricsAlgorithm = metricsAlgorithm;
                    model.SelectedMetrics = await _api.GetAdminMetricsAsync(selectedUserId.Value, metricsAlgorithm, 10, cancellationToken);
                }
            }
        }
        catch
        {
            // Оставляем страницу рабочей и даем понятное сообщение.
            model.Error ??= "Не удалось загрузить данные с API. Обновите страницу или проверьте WebAPI.";
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
        if (!IsAdminRole(currentRole))
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

        return RedirectToAction(nameof(Index), new { activeTab = "admin" });
    }

    [HttpPost]
    public async Task<IActionResult> SetBlocked(Guid userId, bool isBlocked, CancellationToken cancellationToken)
    {
        var currentRole = _tokenStore.GetRole();
        if (!IsAdminRole(currentRole))
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

        return RedirectToAction(nameof(Index), new { activeTab = "admin" });
    }

    [HttpPost]
    public IActionResult ShowMetrics(Guid userId, string algorithm = "knn")
    {
        return RedirectToAction(nameof(Index), new { metricsUserId = userId, metricsAlgorithm = algorithm, activeTab = "admin" });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteUser(Guid userId, CancellationToken cancellationToken)
    {
        var currentRole = _tokenStore.GetRole();
        if (!IsAdminRole(currentRole))
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

        return RedirectToAction(nameof(Index), new { activeTab = "admin" });
    }

    [HttpPost]
    public async Task<IActionResult> DownloadReportCsv(CancellationToken cancellationToken)
    {
        var currentRole = _tokenStore.GetRole();
        if (!IsAdminRole(currentRole))
        {
            return Forbid();
        }

        var file = await _api.DownloadAdminReportAsync("csv", cancellationToken);
        if (file is null)
        {
            TempData["Error"] = "Не удалось выгрузить CSV отчет.";
            return RedirectToAction(nameof(Index), new { activeTab = "admin" });
        }

        return File(file.Value.Bytes, file.Value.ContentType, file.Value.FileName);
    }

    [HttpPost]
    public async Task<IActionResult> DownloadReportPdf(CancellationToken cancellationToken)
    {
        var currentRole = _tokenStore.GetRole();
        if (!IsAdminRole(currentRole))
        {
            return Forbid();
        }

        var file = await _api.DownloadAdminReportAsync("pdf", cancellationToken);
        if (file is null)
        {
            TempData["Error"] = "Не удалось выгрузить PDF отчет.";
            return RedirectToAction(nameof(Index), new { activeTab = "admin" });
        }

        return File(file.Value.Bytes, file.Value.ContentType, file.Value.FileName);
    }

    [HttpPost]
    public async Task<IActionResult> CreateContent(CreateContentVm vm, CancellationToken cancellationToken)
    {
        var currentRole = _tokenStore.GetRole();
        if (!IsAdminRole(currentRole))
        {
            return Forbid();
        }

        var ok = await _api.CreateContentAsync(vm, cancellationToken);
        TempData[ok ? "Info" : "Error"] = ok ? "Контент создан." : "Не удалось создать контент.";
        return RedirectToAction(nameof(Index), new { activeTab = "admin" });
    }

    [HttpPost]
    public async Task<IActionResult> UpdateContent(UpdateContentVm vm, CancellationToken cancellationToken)
    {
        var currentRole = _tokenStore.GetRole();
        if (!IsAdminRole(currentRole))
        {
            return Forbid();
        }

        var ok = await _api.UpdateContentAsync(vm, cancellationToken);
        TempData[ok ? "Info" : "Error"] = ok ? "Контент обновлён." : "Не удалось обновить контент.";
        return RedirectToAction(nameof(Index), new { activeTab = "admin" });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteContent(Guid id, CancellationToken cancellationToken)
    {
        var currentRole = _tokenStore.GetRole();
        if (!IsAdminRole(currentRole))
        {
            return Forbid();
        }

        var ok = await _api.DeleteContentAsync(id, cancellationToken);
        TempData[ok ? "Info" : "Error"] = ok ? "Контент удалён." : "Не удалось удалить контент.";
        return RedirectToAction(nameof(Index), new { activeTab = "admin" });
    }

    public IActionResult Help()
    {
        return View("Help");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    private static string NormalizeRole(string? role)
    {
        return role switch
        {
            "1" => "Admin",
            "0" => "User",
            _ when string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) => "Admin",
            _ when string.Equals(role, "User", StringComparison.OrdinalIgnoreCase) => "User",
            _ => role ?? "Guest"
        };
    }

    private static bool IsAdminRole(string? role)
    {
        return string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) || role == "1";
    }
}

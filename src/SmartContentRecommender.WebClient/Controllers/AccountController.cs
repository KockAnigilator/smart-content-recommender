using Microsoft.AspNetCore.Mvc;
using SmartContentRecommender.WebClient.Models;
using SmartContentRecommender.WebClient.Services;

namespace SmartContentRecommender.WebClient.Controllers;

public class AccountController : Controller
{
    private readonly ScrApiClient _api;
    private readonly ITokenStore _tokenStore;

    public AccountController(ScrApiClient api, ITokenStore tokenStore)
    {
        _api = api;
        _tokenStore = tokenStore;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(AuthPayload payload, CancellationToken cancellationToken)
    {
        var response = await _api.RegisterAsync(payload, cancellationToken);
        if (response is null || !response.IsSuccess)
        {
            TempData["Error"] = response?.Message ?? "Ошибка регистрации.";
            return RedirectToAction("Index", "Home");
        }

        TempData["Info"] = response.Message;
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AuthPayload payload, CancellationToken cancellationToken)
    {
        var response = await _api.LoginAsync(payload, cancellationToken);
        if (response is null || !response.IsSuccess || response.Data is null)
        {
            TempData["Error"] = response?.Message ?? "Ошибка авторизации.";
            return RedirectToAction("Index", "Home");
        }

        _tokenStore.SetToken(response.Data.Token);

        var me = await _api.GetMeAsync(cancellationToken);
        if (me?.Role is not null)
        {
            _tokenStore.SetRole(me.Role);
        }
        else
        {
            _tokenStore.Clear();
            TempData["Error"] = "Не удалось получить роль пользователя.";
            return RedirectToAction("Index", "Home");
        }

        TempData["Info"] = "Вход выполнен успешно.";
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        _tokenStore.Clear();
        TempData["Info"] = "Вы вышли из системы.";
        return RedirectToAction("Index", "Home");
    }
}


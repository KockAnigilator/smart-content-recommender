using System.Collections.ObjectModel;
using SmartContentRecommender.WpfClient.Infrastructure;
using SmartContentRecommender.WpfClient.Models;
using SmartContentRecommender.WpfClient.Services;

namespace SmartContentRecommender.WpfClient.ViewModels;

public class MainViewModel : ObservableObject
{
    private readonly ApiClient _apiClient = new("http://localhost:5078");

    private string _email = string.Empty;
    private string _password = string.Empty;
    private string _status = "Готово к работе";
    private bool _isAuthorized;
    private string _currentRole = "Guest";
    private ContentItem? _selectedContent;
    private AdminUserItem? _selectedAdminUser;

    public MainViewModel()
    {
        RegisterCommand = new RelayCommand(async () => await RegisterAsync());
        LoginCommand = new RelayCommand(async () => await LoginAsync());
        LogoutCommand = new RelayCommand(Logout, () => IsAuthorized);

        LoadContentCommand = new RelayCommand(async () => await LoadContentAsync());
        LogViewCommand = new RelayCommand(async () => await LogActionAsync(0), () => SelectedContent is not null && IsAuthorized);
        LogLikeCommand = new RelayCommand(async () => await LogActionAsync(1), () => SelectedContent is not null && IsAuthorized);
        LogClickCommand = new RelayCommand(async () => await LogActionAsync(2), () => SelectedContent is not null && IsAuthorized);

        LoadPopularCommand = new RelayCommand(async () => await LoadPopularAsync());
        LoadByCategoriesCommand = new RelayCommand(async () => await LoadByCategoriesAsync(), () => IsAuthorized);
        LoadKnnCommand = new RelayCommand(async () => await LoadKnnAsync(), () => IsAuthorized);

        LoadAdminUsersCommand = new RelayCommand(async () => await LoadAdminUsersAsync(), () => IsAdmin);
        MakeAdminCommand = new RelayCommand(async () => await ChangeRoleAsync("Admin"), () => IsAdmin && SelectedAdminUser is not null);
        MakeUserCommand = new RelayCommand(async () => await ChangeRoleAsync("User"), () => IsAdmin && SelectedAdminUser is not null);
        BlockUserCommand = new RelayCommand(async () => await SetBlockedAsync(true), () => IsAdmin && SelectedAdminUser is not null);
        UnblockUserCommand = new RelayCommand(async () => await SetBlockedAsync(false), () => IsAdmin && SelectedAdminUser is not null);
        DeleteUserCommand = new RelayCommand(async () => await DeleteUserAsync(), () => IsAdmin && SelectedAdminUser is not null);
    }

    public string Email
    {
        get => _email;
        set => SetProperty(ref _email, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }

    public bool IsAuthorized
    {
        get => _isAuthorized;
        set
        {
            if (SetProperty(ref _isAuthorized, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }

    public string CurrentRole
    {
        get => _currentRole;
        set
        {
            if (SetProperty(ref _currentRole, value))
            {
                OnPropertyChanged(nameof(IsAdmin));
                RaiseCanExecuteChanged();
            }
        }
    }

    public ContentItem? SelectedContent
    {
        get => _selectedContent;
        set
        {
            if (SetProperty(ref _selectedContent, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }

    public ObservableCollection<ContentItem> ContentItems { get; } = [];
    public ObservableCollection<RecommendationItem> RecommendationItems { get; } = [];
    public ObservableCollection<AdminUserItem> AdminUsers { get; } = [];

    public AdminUserItem? SelectedAdminUser
    {
        get => _selectedAdminUser;
        set
        {
            if (SetProperty(ref _selectedAdminUser, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }

    public RelayCommand RegisterCommand { get; }
    public RelayCommand LoginCommand { get; }
    public RelayCommand LogoutCommand { get; }
    public RelayCommand LoadContentCommand { get; }
    public RelayCommand LogViewCommand { get; }
    public RelayCommand LogLikeCommand { get; }
    public RelayCommand LogClickCommand { get; }
    public RelayCommand LoadPopularCommand { get; }
    public RelayCommand LoadByCategoriesCommand { get; }
    public RelayCommand LoadKnnCommand { get; }
    public RelayCommand LoadAdminUsersCommand { get; }
    public RelayCommand MakeAdminCommand { get; }
    public RelayCommand MakeUserCommand { get; }
    public RelayCommand BlockUserCommand { get; }
    public RelayCommand UnblockUserCommand { get; }
    public RelayCommand DeleteUserCommand { get; }

    public bool IsAdmin => CurrentRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);

    private async Task RegisterAsync()
    {
        var response = await _apiClient.RegisterAsync(new AuthPayload
        {
            Email = Email.Trim(),
            Password = Password.Trim()
        });

        Status = response?.Message ?? "Ошибка регистрации";
    }

    private async Task LoginAsync()
    {
        var response = await _apiClient.LoginAsync(new AuthPayload
        {
            Email = Email.Trim(),
            Password = Password.Trim()
        });

        if (response is null || !response.IsSuccess || response.Data is null)
        {
            Status = response?.Message ?? "Ошибка авторизации";
            return;
        }

        _apiClient.SetToken(response.Data.Token);
        IsAuthorized = true;

        var me = await _apiClient.GetMeAsync();
        CurrentRole = me?.Role ?? "User";

        Status = $"Авторизация успешна. Роль: {CurrentRole}";
        await LoadContentAsync();
        await LoadPopularAsync();
        if (IsAdmin)
        {
            await LoadAdminUsersAsync();
        }
    }

    private void Logout()
    {
        _apiClient.SetToken(null);
        IsAuthorized = false;
        CurrentRole = "Guest";
        Status = "Вы вышли из системы";
        RecommendationItems.Clear();
        AdminUsers.Clear();
    }

    private async Task LoadContentAsync()
    {
        var items = await _apiClient.GetContentAsync();
        ReplaceItems(ContentItems, items);
        Status = $"Контент загружен: {items.Count}";
    }

    private async Task LogActionAsync(int type)
    {
        if (SelectedContent is null)
        {
            return;
        }

        var success = await _apiClient.LogActionAsync(SelectedContent.Id, type);
        Status = success ? "Действие сохранено" : "Не удалось сохранить действие";
    }

    private async Task LoadPopularAsync()
    {
        var items = await _apiClient.GetPopularAsync();
        ReplaceItems(RecommendationItems, items);
        Status = $"Популярные рекомендации: {items.Count}";
    }

    private async Task LoadByCategoriesAsync()
    {
        var items = await _apiClient.GetByCategoriesAsync();
        ReplaceItems(RecommendationItems, items);
        Status = $"Рекомендации по категориям: {items.Count}";
    }

    private async Task LoadKnnAsync()
    {
        var items = await _apiClient.GetKnnAsync();
        ReplaceItems(RecommendationItems, items);
        Status = $"KNN рекомендации: {items.Count}";
    }

    private async Task LoadAdminUsersAsync()
    {
        var users = await _apiClient.GetAdminUsersAsync();
        ReplaceItems(AdminUsers, users);
        Status = $"Пользователей загружено: {users.Count}";
    }

    private async Task ChangeRoleAsync(string role)
    {
        if (SelectedAdminUser is null)
        {
            return;
        }

        var success = await _apiClient.ChangeUserRoleAsync(SelectedAdminUser.Id, role);
        Status = success ? "Роль пользователя обновлена." : "Не удалось обновить роль.";
        if (success)
        {
            await LoadAdminUsersAsync();
        }
    }

    private async Task SetBlockedAsync(bool isBlocked)
    {
        if (SelectedAdminUser is null)
        {
            return;
        }

        var success = await _apiClient.SetBlockedAsync(SelectedAdminUser.Id, isBlocked);
        Status = success ? "Статус блокировки обновлен." : "Не удалось обновить блокировку.";
        if (success)
        {
            await LoadAdminUsersAsync();
        }
    }

    private async Task DeleteUserAsync()
    {
        if (SelectedAdminUser is null)
        {
            return;
        }

        var success = await _apiClient.DeleteUserAsync(SelectedAdminUser.Id);
        Status = success ? "Пользователь удален." : "Не удалось удалить пользователя.";
        if (success)
        {
            await LoadAdminUsersAsync();
        }
    }

    private void ReplaceItems<T>(ObservableCollection<T> target, IEnumerable<T> source)
    {
        target.Clear();
        foreach (var item in source)
        {
            target.Add(item);
        }
    }

    private void RaiseCanExecuteChanged()
    {
        LogoutCommand.RaiseCanExecuteChanged();
        LogViewCommand.RaiseCanExecuteChanged();
        LogLikeCommand.RaiseCanExecuteChanged();
        LogClickCommand.RaiseCanExecuteChanged();
        LoadByCategoriesCommand.RaiseCanExecuteChanged();
        LoadKnnCommand.RaiseCanExecuteChanged();
        LoadAdminUsersCommand.RaiseCanExecuteChanged();
        MakeAdminCommand.RaiseCanExecuteChanged();
        MakeUserCommand.RaiseCanExecuteChanged();
        BlockUserCommand.RaiseCanExecuteChanged();
        UnblockUserCommand.RaiseCanExecuteChanged();
        DeleteUserCommand.RaiseCanExecuteChanged();
    }

}


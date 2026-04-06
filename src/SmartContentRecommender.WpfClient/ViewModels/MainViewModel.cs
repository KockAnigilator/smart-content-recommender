using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows.Media;
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
    private bool _isBusy;
    private bool _isApiOnline;
    private string _apiStatusText = "Проверка API...";
    private Brush _apiStatusBrush = Brushes.DarkOrange;
    private ContentItem? _selectedContent;
    private AdminUserItem? _selectedAdminUser;

    public MainViewModel()
    {
        RegisterCommand = new RelayCommand(async () => await RegisterAsync());
        LoginCommand = new RelayCommand(async () => await LoginAsync());
        LogoutCommand = new RelayCommand(Logout, () => IsAuthorized && !IsBusy);

        LoadContentCommand = new RelayCommand(async () => await LoadContentAsync(), () => !IsBusy);
        LogViewCommand = new RelayCommand(async () => await LogActionAsync(0), () => SelectedContent is not null && IsAuthorized && !IsBusy);
        LogLikeCommand = new RelayCommand(async () => await LogActionAsync(1), () => SelectedContent is not null && IsAuthorized && !IsBusy);
        LogClickCommand = new RelayCommand(async () => await LogActionAsync(2), () => SelectedContent is not null && IsAuthorized && !IsBusy);

        LoadPopularCommand = new RelayCommand(async () => await LoadPopularAsync(), () => !IsBusy);
        LoadByCategoriesCommand = new RelayCommand(async () => await LoadByCategoriesAsync(), () => IsAuthorized && !IsBusy);
        LoadKnnCommand = new RelayCommand(async () => await LoadKnnAsync(), () => IsAuthorized && !IsBusy);

        LoadAdminUsersCommand = new RelayCommand(async () => await LoadAdminUsersAsync(), () => IsAdmin && !IsBusy);
        MakeAdminCommand = new RelayCommand(async () => await ChangeRoleAsync("Admin"), () => IsAdmin && SelectedAdminUser is not null && !IsBusy);
        MakeUserCommand = new RelayCommand(async () => await ChangeRoleAsync("User"), () => IsAdmin && SelectedAdminUser is not null && !IsBusy);
        BlockUserCommand = new RelayCommand(async () => await SetBlockedAsync(true), () => IsAdmin && SelectedAdminUser is not null && !IsBusy);
        UnblockUserCommand = new RelayCommand(async () => await SetBlockedAsync(false), () => IsAdmin && SelectedAdminUser is not null && !IsBusy);
        DeleteUserCommand = new RelayCommand(async () => await DeleteUserAsync(), () => IsAdmin && SelectedAdminUser is not null && !IsBusy);
        CheckApiCommand = new RelayCommand(async () => await UpdateApiStatusAsync(), () => !IsBusy);

        _ = UpdateApiStatusAsync();
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

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsNotBusy));
                RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsNotBusy => !IsBusy;

    public bool IsApiOnline
    {
        get => _isApiOnline;
        set => SetProperty(ref _isApiOnline, value);
    }

    public string ApiStatusText
    {
        get => _apiStatusText;
        set => SetProperty(ref _apiStatusText, value);
    }

    public Brush ApiStatusBrush
    {
        get => _apiStatusBrush;
        set => SetProperty(ref _apiStatusBrush, value);
    }

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
    public RelayCommand CheckApiCommand { get; }

    public bool IsAdmin => CurrentRole.Equals("Admin", StringComparison.OrdinalIgnoreCase);

    private async Task RegisterAsync()
    {
        await ExecuteWithUiStateAsync(async () =>
        {
            var response = await _apiClient.RegisterAsync(new AuthPayload
            {
                Email = Email.Trim(),
                Password = Password.Trim()
            });

            Status = response?.Message ?? "Ошибка регистрации";
            await UpdateApiStatusAsync();
        });
    }

    private async Task LoginAsync()
    {
        await ExecuteWithUiStateAsync(async () =>
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

            await UpdateApiStatusAsync();
        });
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
        await ExecuteWithUiStateAsync(async () =>
        {
            var items = await _apiClient.GetContentAsync();
            ReplaceItems(ContentItems, items);
            Status = $"Контент загружен: {items.Count}";
            await UpdateApiStatusAsync();
        });
    }

    private async Task LogActionAsync(int type)
    {
        if (SelectedContent is null)
        {
            return;
        }

        await ExecuteWithUiStateAsync(async () =>
        {
            var success = await _apiClient.LogActionAsync(SelectedContent.Id, type);
            Status = success ? "Действие сохранено" : "Не удалось сохранить действие";
            await UpdateApiStatusAsync();
        });
    }

    private async Task LoadPopularAsync()
    {
        await ExecuteWithUiStateAsync(async () =>
        {
            var items = await _apiClient.GetPopularAsync();
            ReplaceItems(RecommendationItems, items);
            Status = $"Популярные рекомендации: {items.Count}";
            await UpdateApiStatusAsync();
        });
    }

    private async Task LoadByCategoriesAsync()
    {
        await ExecuteWithUiStateAsync(async () =>
        {
            var items = await _apiClient.GetByCategoriesAsync();
            ReplaceItems(RecommendationItems, items);
            Status = $"Рекомендации по категориям: {items.Count}";
            await UpdateApiStatusAsync();
        });
    }

    private async Task LoadKnnAsync()
    {
        await ExecuteWithUiStateAsync(async () =>
        {
            var items = await _apiClient.GetKnnAsync();
            ReplaceItems(RecommendationItems, items);
            Status = $"KNN рекомендации: {items.Count}";
            await UpdateApiStatusAsync();
        });
    }

    private async Task LoadAdminUsersAsync()
    {
        await ExecuteWithUiStateAsync(async () =>
        {
            var users = await _apiClient.GetAdminUsersAsync();
            ReplaceItems(AdminUsers, users);
            Status = $"Пользователей загружено: {users.Count}";
            await UpdateApiStatusAsync();
        });
    }

    private async Task ChangeRoleAsync(string role)
    {
        if (SelectedAdminUser is null)
        {
            return;
        }

        await ExecuteWithUiStateAsync(async () =>
        {
            var success = await _apiClient.ChangeUserRoleAsync(SelectedAdminUser.Id, role);
            Status = success ? "Роль пользователя обновлена." : "Не удалось обновить роль.";
            if (success)
            {
                await LoadAdminUsersAsync();
            }
        });
    }

    private async Task SetBlockedAsync(bool isBlocked)
    {
        if (SelectedAdminUser is null)
        {
            return;
        }

        await ExecuteWithUiStateAsync(async () =>
        {
            var success = await _apiClient.SetBlockedAsync(SelectedAdminUser.Id, isBlocked);
            Status = success ? "Статус блокировки обновлен." : "Не удалось обновить блокировку.";
            if (success)
            {
                await LoadAdminUsersAsync();
            }
        });
    }

    private async Task DeleteUserAsync()
    {
        if (SelectedAdminUser is null)
        {
            return;
        }

        await ExecuteWithUiStateAsync(async () =>
        {
            var success = await _apiClient.DeleteUserAsync(SelectedAdminUser.Id);
            Status = success ? "Пользователь удален." : "Не удалось удалить пользователя.";
            if (success)
            {
                await LoadAdminUsersAsync();
            }
        });
    }

    private async Task ExecuteWithUiStateAsync(Func<Task> action)
    {
        try
        {
            IsBusy = true;
            await action();
        }
        catch (HttpRequestException)
        {
            Status = "Ошибка сети: API недоступен.";
            SetApiOffline();
        }
        catch (TaskCanceledException)
        {
            Status = "Превышено время ожидания ответа API.";
            SetApiOffline();
        }
        catch (Exception ex)
        {
            Status = $"Ошибка: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task UpdateApiStatusAsync()
    {
        var available = await _apiClient.IsApiAvailableAsync();
        if (available)
        {
            IsApiOnline = true;
            ApiStatusText = $"API online ({_apiClient.BaseUrl.TrimEnd('/')})";
            ApiStatusBrush = Brushes.ForestGreen;
        }
        else
        {
            SetApiOffline();
        }
    }

    private void SetApiOffline()
    {
        IsApiOnline = false;
        ApiStatusText = $"API offline ({_apiClient.BaseUrl.TrimEnd('/')})";
        ApiStatusBrush = Brushes.Firebrick;
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
        CheckApiCommand.RaiseCanExecuteChanged();
    }

}


using System.Collections.ObjectModel;
using System.Net.Http;
using System.Windows;
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
    private RecommendationMetricsItem? _selectedMetrics;
    private CategoryItem? _selectedCategory;
    private TagItem? _selectedTag;
    private DbOverview? _dbOverview;
    private string _contentTitle = string.Empty;
    private string _contentUrl = string.Empty;
    private string _contentDescription = string.Empty;
    private string _contentTagIdsCsv = string.Empty;
    private bool _showDemoHistoryButton;
    private string _dashboardSummary = "Откройте дашборд и нажмите 'Обновить'.";
    private string _algorithmComparisonStatus = "Сравнение алгоритмов не загружено.";
    private string _knnDistributionStatus = "Распределение KNN не загружено.";
    private PointCollection _knnDistributionPoints = [];
    private string _categoryDistributionStatus = "Распределение категорий не загружено.";
    private PointCollection _interestCategoryPoints = [];

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
        LoadExplainCommand = new RelayCommand(async () => await LoadExplainAsync(), () => IsAuthorized && !IsBusy);
        LoadInterestProfileCommand = new RelayCommand(async () => await LoadInterestProfileAsync(), () => IsAuthorized && !IsBusy);

        LoadAdminUsersCommand = new RelayCommand(async () => await LoadAdminUsersAsync(), () => IsAdmin && !IsBusy);
        MakeAdminCommand = new RelayCommand(async () => await ChangeRoleAsync("Admin"), () => IsAdmin && SelectedAdminUser is not null && !IsBusy);
        MakeUserCommand = new RelayCommand(async () => await ChangeRoleAsync("User"), () => IsAdmin && SelectedAdminUser is not null && !IsBusy);
        BlockUserCommand = new RelayCommand(async () => await SetBlockedAsync(true), () => IsAdmin && SelectedAdminUser is not null && !IsBusy);
        UnblockUserCommand = new RelayCommand(async () => await SetBlockedAsync(false), () => IsAdmin && SelectedAdminUser is not null && !IsBusy);
        DeleteUserCommand = new RelayCommand(async () => await DeleteUserAsync(), () => IsAdmin && SelectedAdminUser is not null && !IsBusy);
        LoadMetricsCommand = new RelayCommand(async () => await LoadMetricsAsync(), () => IsAdmin && SelectedAdminUser is not null && !IsBusy);
        ExportCsvCommand = new RelayCommand(async () => await ExportReportAsync("csv"), () => IsAdmin && !IsBusy);
        ExportPdfCommand = new RelayCommand(async () => await ExportReportAsync("pdf"), () => IsAdmin && !IsBusy);
        LoadAdminDictionariesCommand = new RelayCommand(async () => await LoadAdminDictionariesAsync(), () => IsAdmin && !IsBusy);
        CreateContentCommand = new RelayCommand(async () => await CreateContentAsync(), () => IsAdmin && SelectedCategory is not null && !IsBusy);
        UpdateContentCommand = new RelayCommand(async () => await UpdateContentAsync(), () => IsAdmin && SelectedCategory is not null && SelectedContent is not null && !IsBusy);
        DeleteContentCommand = new RelayCommand(async () => await DeleteContentAsync(), () => IsAdmin && SelectedContent is not null && !IsBusy);
        LoadDbViewerCommand = new RelayCommand(async () => await LoadDbViewerAsync(), () => IsAdmin && !IsBusy);
        GenerateDemoHistoryCommand = new RelayCommand(async () => await GenerateDemoHistoryAsync(), () => IsAuthorized && ShowDemoHistoryButton && !IsBusy);
        LoadDesktopDashboardCommand = new RelayCommand(async () => await LoadDesktopDashboardAsync(), () => IsAuthorized && !IsBusy);
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
    public ObservableCollection<RecommendationExplanationItem> ExplainItems { get; } = [];
    public ObservableCollection<InterestProfileItem> InterestCategories { get; } = [];
    public ObservableCollection<InterestProfileItem> InterestTags { get; } = [];
    public ObservableCollection<CategoryItem> Categories { get; } = [];
    public ObservableCollection<TagItem> Tags { get; } = [];
    public ObservableCollection<DbUserRow> DbUsers { get; } = [];
    public ObservableCollection<CategoryItem> DbCategories { get; } = [];
    public ObservableCollection<TagItem> DbTags { get; } = [];
    public ObservableCollection<DbContentRow> DbContents { get; } = [];
    public ObservableCollection<DbActionRow> DbActions { get; } = [];
    public ObservableCollection<ChartBarItem> InterestCategoryBars { get; } = [];
    public ObservableCollection<ChartBarItem> KnnDistributionBars { get; } = [];

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

    public RecommendationMetricsItem? SelectedMetrics
    {
        get => _selectedMetrics;
        set => SetProperty(ref _selectedMetrics, value);
    }

    public CategoryItem? SelectedCategory
    {
        get => _selectedCategory;
        set
        {
            if (SetProperty(ref _selectedCategory, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }

    public TagItem? SelectedTag
    {
        get => _selectedTag;
        set => SetProperty(ref _selectedTag, value);
    }

    public DbOverview? DbOverview
    {
        get => _dbOverview;
        set => SetProperty(ref _dbOverview, value);
    }

    public string ContentTitle
    {
        get => _contentTitle;
        set => SetProperty(ref _contentTitle, value);
    }

    public string ContentUrl
    {
        get => _contentUrl;
        set => SetProperty(ref _contentUrl, value);
    }

    public string ContentDescription
    {
        get => _contentDescription;
        set => SetProperty(ref _contentDescription, value);
    }

    public string ContentTagIdsCsv
    {
        get => _contentTagIdsCsv;
        set => SetProperty(ref _contentTagIdsCsv, value);
    }

    public bool ShowDemoHistoryButton
    {
        get => _showDemoHistoryButton;
        set
        {
            if (SetProperty(ref _showDemoHistoryButton, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }

    public string DashboardSummary
    {
        get => _dashboardSummary;
        set => SetProperty(ref _dashboardSummary, value);
    }

    public string AlgorithmComparisonStatus
    {
        get => _algorithmComparisonStatus;
        set => SetProperty(ref _algorithmComparisonStatus, value);
    }

    public string KnnDistributionStatus
    {
        get => _knnDistributionStatus;
        set => SetProperty(ref _knnDistributionStatus, value);
    }

    public PointCollection KnnDistributionPoints
    {
        get => _knnDistributionPoints;
        set => SetProperty(ref _knnDistributionPoints, value);
    }

    public string CategoryDistributionStatus
    {
        get => _categoryDistributionStatus;
        set => SetProperty(ref _categoryDistributionStatus, value);
    }

    public PointCollection InterestCategoryPoints
    {
        get => _interestCategoryPoints;
        set => SetProperty(ref _interestCategoryPoints, value);
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
    public RelayCommand LoadExplainCommand { get; }
    public RelayCommand LoadInterestProfileCommand { get; }
    public RelayCommand LoadAdminUsersCommand { get; }
    public RelayCommand MakeAdminCommand { get; }
    public RelayCommand MakeUserCommand { get; }
    public RelayCommand BlockUserCommand { get; }
    public RelayCommand UnblockUserCommand { get; }
    public RelayCommand DeleteUserCommand { get; }
    public RelayCommand LoadMetricsCommand { get; }
    public RelayCommand ExportCsvCommand { get; }
    public RelayCommand ExportPdfCommand { get; }
    public RelayCommand LoadAdminDictionariesCommand { get; }
    public RelayCommand CreateContentCommand { get; }
    public RelayCommand UpdateContentCommand { get; }
    public RelayCommand DeleteContentCommand { get; }
    public RelayCommand LoadDbViewerCommand { get; }
    public RelayCommand GenerateDemoHistoryCommand { get; }
    public RelayCommand LoadDesktopDashboardCommand { get; }
    public RelayCommand CheckApiCommand { get; }

    public bool IsAdmin =>
        CurrentRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) ||
        CurrentRole.Equals("Админ", StringComparison.OrdinalIgnoreCase) ||
        CurrentRole == "1";

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
            await LoadDesktopDashboardAsync();
            if (IsAdmin)
            {
                await LoadAdminDictionariesAsync();
            }
            if (IsAdmin)
            {
                await LoadAdminUsersAsync();
            }

            await UpdateApiStatusAsync();
        });
    }

    private async Task GenerateDemoHistoryAsync()
    {
        await ExecuteWithUiStateAsync(async () =>
        {
            var ok = await _apiClient.GenerateDemoHistoryAsync();
            Status = ok ? "Демо-история сгенерирована." : "Не удалось сгенерировать демо-историю.";
            if (ok)
            {
                await LoadInterestProfileAsync();
                await LoadExplainAsync();
                await LoadKnnAsync();
                await LoadDesktopDashboardAsync();
            }
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
            Status = items.Count == 0
                ? "By Categories: пока нет данных. Сделайте несколько действий или сгенерируйте демо-историю."
                : $"Рекомендации по категориям: {items.Count}";
            await UpdateApiStatusAsync();
        });
    }

    private async Task LoadKnnAsync()
    {
        await ExecuteWithUiStateAsync(async () =>
        {
            var items = await _apiClient.GetKnnAsync();
            ReplaceItems(RecommendationItems, items);
            Status = items.Count == 0
                ? "KNN: пока нет данных для персонализации. Сделайте действия View/Like/Click."
                : $"KNN рекомендации: {items.Count}";
            await UpdateApiStatusAsync();
        });
    }

    private async Task LoadExplainAsync()
    {
        await ExecuteWithUiStateAsync(async () =>
        {
            var items = await _apiClient.GetExplainAsync("knn", 10);
            ReplaceItems(ExplainItems, items);
            Status = items.Count == 0
                ? "Explain KNN: пока нет explainability данных. Накопите историю действий или сгенерируйте демо-историю."
                : $"Explainability записей: {items.Count}";
            await UpdateApiStatusAsync();
        });
    }

    private async Task LoadInterestProfileAsync()
    {
        await ExecuteWithUiStateAsync(async () =>
        {
            var profile = await _apiClient.GetInterestProfileAsync(5);
            ReplaceItems(InterestCategories, profile?.TopCategories ?? []);
            ReplaceItems(InterestTags, profile?.TopTags ?? []);
            ReplaceItems(InterestCategoryBars, BuildBars(profile?.TopCategories ?? [], Brushes.DodgerBlue));
            InterestCategoryPoints = BuildDistributionPoints(InterestCategoryBars.ToList());
            CategoryDistributionStatus = InterestCategoryBars.Count == 0
                ? "Нет данных категорий. Выполните действия в контенте."
                : $"Распределение категорий построено, элементов: {InterestCategoryBars.Count}";

            var knnItems = await _apiClient.GetKnnAsync();
            var knnBars = BuildRecommendationBars(knnItems);
            ReplaceItems(KnnDistributionBars, knnBars);
            KnnDistributionPoints = BuildDistributionPoints(knnBars);
            KnnDistributionStatus = knnBars.Count == 0
                ? "Нет данных KNN. Сделайте действия View/Like/Click."
                : $"KNN-распределение построено, элементов: {knnBars.Count}";
            Status = profile is null
                ? "Профиль интересов недоступен."
                : profile.TotalActions == 0
                    ? "Профиль интересов пуст. Сделайте действия или нажмите 'Сгенерировать демо-историю (Dev)'."
                    : $"Профиль интересов загружен. Действий: {profile.TotalActions}";
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

    private async Task LoadAdminDictionariesAsync()
    {
        await ExecuteWithUiStateAsync(async () =>
        {
            var categories = await _apiClient.GetCategoriesAsync();
            ReplaceItems(Categories, categories);
            var tags = await _apiClient.GetTagsAsync();
            ReplaceItems(Tags, tags);
            Status = $"Справочники загружены: categories={categories.Count}, tags={tags.Count}";
        });
    }

    private static List<Guid> ParseGuids(string csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return [];
        return csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
            .Where(g => g != Guid.Empty)
            .ToList();
    }

    private async Task CreateContentAsync()
    {
        if (SelectedCategory is null) return;

        await ExecuteWithUiStateAsync(async () =>
        {
            var ok = await _apiClient.CreateContentAsync(new CreateContentRequest
            {
                Title = ContentTitle.Trim(),
                Url = ContentUrl.Trim(),
                Description = string.IsNullOrWhiteSpace(ContentDescription) ? null : ContentDescription.Trim(),
                CategoryId = SelectedCategory.Id,
                TagIds = ParseGuids(ContentTagIdsCsv)
            });

            Status = ok ? "Контент создан." : "Не удалось создать контент.";
            if (ok) await LoadContentAsync();
        });
    }

    private async Task UpdateContentAsync()
    {
        if (SelectedCategory is null || SelectedContent is null) return;

        await ExecuteWithUiStateAsync(async () =>
        {
            var ok = await _apiClient.UpdateContentAsync(SelectedContent.Id, new UpdateContentRequest
            {
                Title = ContentTitle.Trim(),
                Url = ContentUrl.Trim(),
                Description = string.IsNullOrWhiteSpace(ContentDescription) ? null : ContentDescription.Trim(),
                CategoryId = SelectedCategory.Id,
                TagIds = ParseGuids(ContentTagIdsCsv)
            });

            Status = ok ? "Контент обновлён." : "Не удалось обновить контент.";
            if (ok) await LoadContentAsync();
        });
    }

    private async Task DeleteContentAsync()
    {
        if (SelectedContent is null) return;

        await ExecuteWithUiStateAsync(async () =>
        {
            var ok = await _apiClient.DeleteContentAsync(SelectedContent.Id);
            Status = ok ? "Контент удалён." : "Не удалось удалить контент.";
            if (ok) await LoadContentAsync();
        });
    }

    private async Task LoadDbViewerAsync()
    {
        await ExecuteWithUiStateAsync(async () =>
        {
            DbOverview = await _apiClient.GetDbOverviewAsync();
            ReplaceItems(DbUsers, await _apiClient.GetDbUsersAsync());
            var dbCategories = await _apiClient.GetDbCategoriesAsync();
            if (dbCategories.Count == 0)
            {
                dbCategories = await _apiClient.GetCategoriesAsync();
            }
            ReplaceItems(DbCategories, dbCategories);

            var dbTags = await _apiClient.GetDbTagsAsync();
            if (dbTags.Count == 0)
            {
                dbTags = await _apiClient.GetTagsAsync();
            }
            ReplaceItems(DbTags, dbTags);
            ReplaceItems(DbContents, await _apiClient.GetDbContentsAsync());
            ReplaceItems(DbActions, await _apiClient.GetDbActionsAsync());
            Status = $"DB viewer: users={DbUsers.Count}, categories={DbCategories.Count}, tags={DbTags.Count}, contents={DbContents.Count}, actions={DbActions.Count}";
        });
    }

    private async Task LoadDesktopDashboardAsync()
    {
        await ExecuteWithUiStateAsync(async () =>
        {
            var profile = await _apiClient.GetInterestProfileAsync(5);
            ReplaceItems(InterestCategories, profile?.TopCategories ?? []);
            ReplaceItems(InterestTags, profile?.TopTags ?? []);
            ReplaceItems(InterestCategoryBars, BuildBars(profile?.TopCategories ?? [], Brushes.DodgerBlue));
            InterestCategoryPoints = BuildDistributionPoints(InterestCategoryBars.ToList());
            CategoryDistributionStatus = InterestCategoryBars.Count == 0
                ? "Нет данных категорий. Выполните действия в контенте."
                : $"Распределение категорий построено, элементов: {InterestCategoryBars.Count}";

            var knnItems = await _apiClient.GetKnnAsync();
            var knnBars = BuildRecommendationBars(knnItems);
            ReplaceItems(KnnDistributionBars, knnBars);
            KnnDistributionPoints = BuildDistributionPoints(knnBars);
            KnnDistributionStatus = knnBars.Count == 0
                ? "Нет данных KNN. Сделайте действия View/Like/Click."
                : $"KNN-распределение построено, элементов: {knnBars.Count}";

            DashboardSummary = $"Графики: категории={InterestCategoryBars.Count}, KNN={KnnDistributionBars.Count}";
            Status = "Графики и аналитика обновлены.";
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

    private async Task LoadMetricsAsync()
    {
        if (SelectedAdminUser is null)
        {
            return;
        }

        await ExecuteWithUiStateAsync(async () =>
        {
            SelectedMetrics = await _apiClient.GetAdminMetricsAsync(SelectedAdminUser.Id, "knn", 10);
            Status = SelectedMetrics is null
                ? "Не удалось загрузить метрики."
                : $"Метрики KNN: P@K={SelectedMetrics.PrecisionAtK:F3}, R@K={SelectedMetrics.RecallAtK:F3}, NDCG={SelectedMetrics.NdcgAtK:F3}";
        });
    }

    private async Task ExportReportAsync(string format)
    {
        await ExecuteWithUiStateAsync(async () =>
        {
            var path = await _apiClient.DownloadReportAsync(format);
            Status = path is null
                ? $"Не удалось выгрузить {format.ToUpperInvariant()}."
                : $"Отчет сохранен: {path}";
        });
    }

    private async Task ExecuteWithUiStateAsync(Func<Task> action)
    {
        try
        {
            IsBusy = true;
            await action();
        }
        catch (HttpRequestException ex) when (ex.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.Forbidden)
        {
            Status = "Нет доступа (нужно войти / недостаточно прав).";
            await UpdateApiStatusAsync();
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
            ShowDemoHistoryButton = IsAuthorized && await _apiClient.IsDevModeAsync();
        }
        else
        {
            SetApiOffline();
            ShowDemoHistoryButton = false;
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
        LoadExplainCommand.RaiseCanExecuteChanged();
        LoadInterestProfileCommand.RaiseCanExecuteChanged();
        LoadAdminUsersCommand.RaiseCanExecuteChanged();
        MakeAdminCommand.RaiseCanExecuteChanged();
        MakeUserCommand.RaiseCanExecuteChanged();
        BlockUserCommand.RaiseCanExecuteChanged();
        UnblockUserCommand.RaiseCanExecuteChanged();
        DeleteUserCommand.RaiseCanExecuteChanged();
        LoadMetricsCommand.RaiseCanExecuteChanged();
        ExportCsvCommand.RaiseCanExecuteChanged();
        ExportPdfCommand.RaiseCanExecuteChanged();
        LoadAdminDictionariesCommand.RaiseCanExecuteChanged();
        CreateContentCommand.RaiseCanExecuteChanged();
        UpdateContentCommand.RaiseCanExecuteChanged();
        DeleteContentCommand.RaiseCanExecuteChanged();
        LoadDbViewerCommand.RaiseCanExecuteChanged();
        GenerateDemoHistoryCommand.RaiseCanExecuteChanged();
        LoadDesktopDashboardCommand.RaiseCanExecuteChanged();
        CheckApiCommand.RaiseCanExecuteChanged();
    }

    private static List<ChartBarItem> BuildBars(List<InterestProfileItem> source, Brush brush)
    {
        if (source.Count == 0)
        {
            return [];
        }

        var max = Math.Max(1, source.Max(x => x.Score));
        return source
            .Select(x => new ChartBarItem
            {
                Label = x.Name,
                Value = x.Score,
                Percent = Math.Clamp((x.Score / max) * 100, 0, 100),
                Brush = brush
            })
            .ToList();
    }

    private static List<ChartBarItem> BuildRecommendationBars(List<RecommendationItem> source)
    {
        if (source.Count == 0)
        {
            return [];
        }

        var top = source
            .OrderByDescending(x => x.Score)
            .Take(8)
            .ToList();

        var max = Math.Max(0.0001, top.Max(x => x.Score));

        return top.Select(x => new ChartBarItem
            {
                Label = x.Title.Length > 28 ? $"{x.Title[..28]}..." : x.Title,
                Value = x.Score,
                Percent = Math.Clamp((x.Score / max) * 100, 0, 100),
                Brush = Brushes.MediumPurple
            })
            .ToList();
    }

    private static PointCollection BuildDistributionPoints(List<ChartBarItem> source)
    {
        var points = new PointCollection();
        if (source.Count == 0)
        {
            return points;
        }

        const double width = 520;
        const double height = 140;
        const double left = 40;
        const double top = 16;

        var step = source.Count == 1 ? width : width / (source.Count - 1);

        for (var i = 0; i < source.Count; i++)
        {
            var x = left + (i * step);
            var y = top + ((100 - source[i].Percent) / 100.0 * height);
            points.Add(new Point(x, y));
        }

        return points;
    }

}


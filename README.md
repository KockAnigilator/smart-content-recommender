# Smart Content Recommender

Дипломный проект:
**"Система рекомендаций контента на основе пользовательского поведения"**.

## Технологии

- C#
- ASP.NET Core Web API
- Entity Framework Core
- PostgreSQL
- WPF (клиент)
- Clean Architecture (Domain / Application / Infrastructure / WebAPI)

## Структура решения

- `src/SmartContentRecommender.Domain` — сущности и enum'ы
- `src/SmartContentRecommender.Application` — контракты сервисов и DTO
- `src/SmartContentRecommender.Infrastructure` — EF Core, сервисы, JWT, seed
- `src/SmartContentRecommender.WebAPI` — контроллеры, аутентификация, Swagger
- `src/SmartContentRecommender.WpfClient` — WPF клиент (MVVM)

## Быстрый запуск API

1. Проверить строку подключения в `src/SmartContentRecommender.WebAPI/appsettings.json`.
2. Выполнить миграции:

```bash
dotnet ef database update --project src/SmartContentRecommender.Infrastructure/SmartContentRecommender.Infrastructure.csproj --startup-project src/SmartContentRecommender.WebAPI/SmartContentRecommender.WebAPI.csproj
```

3. Запустить API:

```bash
dotnet run --project src/SmartContentRecommender.WebAPI/SmartContentRecommender.WebAPI.csproj
```

4. Открыть Swagger:
- [http://localhost:5078/swagger](http://localhost:5078/swagger)

### Seed-данные

В режиме Development автоматически добавляются тестовые данные:
- пользователи: `admin@local`, `user1@local`, `user2@local`
- пароль admin: `Admin123!`
- пароль user: `User123!`
- категории, теги, контент и действия для демонстрации рекомендаций.

## Запуск WPF-клиента

```bash
dotnet run --project src/SmartContentRecommender.WpfClient/SmartContentRecommender.WpfClient.csproj
```

Клиент работает с API по адресу `http://localhost:5078`.

## Краткий сценарий тестирования (Smoke Test)

1. Запустить API и открыть Swagger.
2. Выполнить `POST /api/auth/login` под `admin@local`.
3. Нажать `Authorize` в Swagger и вставить:

```text
Bearer <token>
```

4. Проверить `GET /api/content`, `GET /api/recommendations/popular`.
5. Проверить запись действий: `POST /api/useractions/log`.
6. Проверить персональные рекомендации: `GET /api/recommendations/by-categories`, `GET /api/recommendations/knn`.
7. Проверить админ-функции: `GET /api/admin/users`, смена роли и блокировка.

## Основной функционал

- Регистрация/авторизация (JWT)
- Роли: `Admin`, `User`
- CRUD контента, категорий, тегов
- Логирование действий пользователей (`View`, `Like`, `Click`)
- Рекомендации:
  - по категориям
  - популярный контент
  - упрощенный KNN
- Админ-управление пользователями
- WPF-клиент для пользователя и администратора

## Диаграммы

Подробные архитектурные диаграммы:
- `docs/architecture.md`
- Сценарий демонстрации: `docs/demo-scenario-ru.md`
- Пошаговая памятка использования: `docs/user-guide-ru.md`
- Скрипт запуска демо: `scripts/run-demo.ps1`

## Логическое микросервисное разделение (демо)

В репозитории дополнительно добавлены сервисы:
- `src/SmartContentRecommender.AuthService`
- `src/SmartContentRecommender.ContentService`
- `src/SmartContentRecommender.RecommendationService`

Это демонстрационный шаг для диплома (без Docker/Kafka), чтобы показать декомпозицию монолита.

Запуск примера сервисов:

```bash
dotnet run --project src/SmartContentRecommender.AuthService/SmartContentRecommender.AuthService.csproj
dotnet run --project src/SmartContentRecommender.ContentService/SmartContentRecommender.ContentService.csproj
dotnet run --project src/SmartContentRecommender.RecommendationService/SmartContentRecommender.RecommendationService.csproj
```


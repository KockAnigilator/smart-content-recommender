# Памятка по использованию проекта (пошагово)

## 0) Предусловия
- Установлен .NET SDK 9+
- Установлен PostgreSQL, доступен `localhost:5432`
- Создана БД `recommendation_system`
- В `src/SmartContentRecommender.WebAPI/appsettings.json` корректная строка подключения

## 1) Подготовка БД

```powershell
dotnet ef database update --project src/SmartContentRecommender.Infrastructure/SmartContentRecommender.Infrastructure.csproj --startup-project src/SmartContentRecommender.WebAPI/SmartContentRecommender.WebAPI.csproj
```

## 2) Способ A: запуск одной командой (рекомендуется)

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-demo.ps1
```

Откроются окна:
- WebAPI (монолит)
- WPF клиент

## 3) Способ B: запуск вручную

### 3.1 Запустить API
```powershell
dotnet run --project src/SmartContentRecommender.WebAPI/SmartContentRecommender.WebAPI.csproj
```

### 3.2 Открыть Swagger
- `http://localhost:5078/swagger`

### 3.3 Запустить WPF
```powershell
dotnet run --project src/SmartContentRecommender.WpfClient/SmartContentRecommender.WpfClient.csproj
```

## 4) Авторизация и роли

Seed-пользователи (создаются автоматически в Development):
- Admin: `admin@local` / `Admin123!`
- User: `user1@local` / `User123!`
- User: `user2@local` / `User123!`

### Через Swagger
1. `POST /api/auth/login`
2. Скопировать `token`
3. Нажать `Authorize` и вставить: `Bearer <token>`

### Через WPF
1. Ввести email/password
2. Нажать `Вход`
3. Вверху отобразится роль

## 5) Что протестировать (чек-лист)

### 5.1 Пользователь
1. Загрузить контент
2. Выбрать запись
3. Нажать `View`, `Like`, `Click`
4. Нажать `Popular`, `By Categories`, `KNN`

### 5.2 Админ
1. Войти как `admin@local`
2. В админ-вкладке:
   - загрузить пользователей
   - сменить роль
   - заблокировать/разблокировать
   - удалить пользователя

### 5.3 Проверка блокировки
1. Заблокировать пользователя
2. Попробовать вход от этого пользователя
3. Убедиться, что вход отклоняется

## 6) Запуск микросервисного демо

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\run-demo.ps1 -Microservices
```

Запустятся:
- `AuthService`
- `ContentService`
- `RecommendationService`
- WPF клиент

Это демонстрационное логическое разделение (без Docker/Kafka).


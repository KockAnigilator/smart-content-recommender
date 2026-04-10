# Диаграммы проекта (Mermaid)

Ниже набор диаграмм для защиты диплома. Их можно вставлять в Markdown-рендер, Mermaid Live Editor, draw.io (через Mermaid), Notion, Obsidian или скриншотить в презентацию.

---

## 1) Диаграмма компонентов (высокий уровень)

```mermaid
flowchart LR
    User[User]
    Admin[Admin]

    WebClient[WebClient MVC]
    WpfClient[WPF Client MVVM]
    WebAPI[ASP.NET Core WebAPI]
    App[Application Layer]
    Domain[Domain Layer]
    Infra[Infrastructure Layer]
    Db[(PostgreSQL)]

    User --> WebClient
    User --> WpfClient
    Admin --> WebClient
    Admin --> WpfClient

    WebClient --> WebAPI
    WpfClient --> WebAPI

    WebAPI --> App
    WebAPI --> Infra
    Infra --> App
    Infra --> Domain
    App --> Domain
    Infra --> Db
```

---

## 2) Диаграмма слоев (Clean Architecture)

```mermaid
flowchart TB
    subgraph Presentation
        WebClient[WebClient]
        WpfClient[WpfClient]
        Api[WebAPI Controllers]
    end

    subgraph Application
        UseCases[Services Interfaces + DTO]
    end

    subgraph Domain
        Entities[Entities + Enums]
    end

    subgraph Infrastructure
        Ef[EF Core DbContext]
        Impl[Service Implementations]
        Auth[JWT/Auth/RateLimit]
    end

    WebClient --> Api
    WpfClient --> Api
    Api --> UseCases
    Impl --> UseCases
    UseCases --> Entities
    Impl --> Entities
    Impl --> Ef
    Auth --> Api
```

---

## 3) ER-диаграмма базы данных

```mermaid
erDiagram
    USERS ||--o{ USER_ACTIONS : performs
    CONTENTS ||--o{ USER_ACTIONS : has
    CATEGORIES ||--o{ CONTENTS : groups
    CONTENTS ||--o{ CONTENT_TAGS : mapped
    TAGS ||--o{ CONTENT_TAGS : mapped

    USERS {
        uuid id PK
        string email
        string password_hash
        int role
        bool is_blocked
        datetime created_at_utc
    }

    CATEGORIES {
        uuid id PK
        string name
    }

    TAGS {
        uuid id PK
        string name
    }

    CONTENTS {
        uuid id PK
        string title
        string description
        string url
        uuid category_id FK
        datetime created_at_utc
    }

    CONTENT_TAGS {
        uuid content_id FK
        uuid tag_id FK
    }

    USER_ACTIONS {
        uuid id PK
        uuid user_id FK
        uuid content_id FK
        int type
        datetime created_at_utc
    }
```

---

## 4) Поток данных (DFD, уровень 1)

```mermaid
flowchart LR
    U[Пользователь]
    A[Администратор]

    P1[Авторизация и роли]
    P2[Каталог контента]
    P3[Логирование действий]
    P4[Рекомендательный модуль]
    P5[Админ-модуль]

    D1[(users)]
    D2[(contents/categories/tags/content_tags)]
    D3[(user_actions)]

    U --> P1
    U --> P2
    U --> P3
    U --> P4

    A --> P1
    A --> P5

    P1 <--> D1
    P2 <--> D2
    P3 --> D3
    P4 --> D2
    P4 --> D3
    P5 --> D1
    P5 --> D2
    P5 --> D3
```

---

## 5) Sequence: получение рекомендаций

```mermaid
sequenceDiagram
    participant U as User
    participant C as Client (Web/WPF)
    participant API as RecommendationsController
    participant S as RecommendationService
    participant DB as PostgreSQL

    U->>C: Запросить KNN рекомендации
    C->>API: GET /api/recommendations/knn (JWT)
    API->>S: GetKnnAsync(userId, limit)
    S->>DB: Чтение user_actions + contents
    DB-->>S: История действий и контент
    S-->>API: Список рекомендаций + score + reason
    API-->>C: 200 OK (JSON)
    C-->>U: Отображение рекомендаций
```

---

## 6) Sequence: действие пользователя -> обновление профиля интересов

```mermaid
sequenceDiagram
    participant U as User
    participant C as Client
    participant UA as UserActionsController
    participant US as UserActionService
    participant AN as AnalyticsService
    participant DB as PostgreSQL

    U->>C: Нажимает Like/View/Click
    C->>UA: POST /api/useractions/log
    UA->>US: LogActionAsync(...)
    US->>DB: INSERT user_actions
    DB-->>US: OK
    US-->>UA: true
    UA-->>C: 200 OK

    U->>C: Открывает профиль интересов
    C->>UA: GET /api/useractions/interest-profile
    UA->>AN: GetInterestProfileAsync(userId)
    AN->>DB: SELECT actions + joins
    DB-->>AN: Данные
    AN-->>UA: TopCategories/TopTags
    UA-->>C: 200 OK
```

---

## 7) UML-диаграмма классов (упрощенная)

```mermaid
classDiagram
    class User {
      +Guid Id
      +string Email
      +string PasswordHash
      +UserRole Role
      +bool IsBlocked
      +DateTime CreatedAtUtc
    }

    class Content {
      +Guid Id
      +string Title
      +string Description
      +string Url
      +Guid CategoryId
      +DateTime CreatedAtUtc
    }

    class Category {
      +Guid Id
      +string Name
    }

    class Tag {
      +Guid Id
      +string Name
    }

    class ContentTag {
      +Guid ContentId
      +Guid TagId
    }

    class UserAction {
      +Guid Id
      +Guid UserId
      +Guid ContentId
      +UserActionType Type
      +DateTime CreatedAtUtc
    }

    class IRecommendationService {
      <<interface>>
      +GetPopularAsync(limit)
      +GetByCategoriesAsync(userId, limit)
      +GetKnnAsync(userId, limit)
    }

    class RecommendationService {
      +GetPopularAsync(limit)
      +GetByCategoriesAsync(userId, limit)
      +GetKnnAsync(userId, limit)
    }

    class IAnalyticsService {
      <<interface>>
      +GetInterestProfileAsync(userId, top)
      +ExplainRecommendationsAsync(userId, algorithm, limit)
      +BuildDefenseReportAsync(from, to, topUsers)
    }

    class AnalyticsService {
      +GetInterestProfileAsync(...)
      +ExplainRecommendationsAsync(...)
      +BuildDefenseReportAsync(...)
    }

    IRecommendationService <|.. RecommendationService
    IAnalyticsService <|.. AnalyticsService

    Category "1" --> "many" Content
    Content "1" --> "many" UserAction
    User "1" --> "many" UserAction
    Content "1" --> "many" ContentTag
    Tag "1" --> "many" ContentTag
```

---

## 8) Deployment-диаграмма (для демонстрации)

```mermaid
flowchart LR
    DevPC[ПК пользователя/демонстрация]
    Browser[Browser]
    Wpf[WPF App]
    ApiNode[ASP.NET Core WebAPI :5078]
    DbNode[(PostgreSQL :5432)]

    DevPC --> Browser
    DevPC --> Wpf
    Browser --> ApiNode
    Wpf --> ApiNode
    ApiNode --> DbNode
```

---

## 9) Логическое микросервисное разделение (для слайда с перспективой)

```mermaid
flowchart LR
    Client[Clients Web/WPF]
    AuthSvc[AuthService]
    ContentSvc[ContentService]
    RecSvc[RecommendationService]
    Db[(PostgreSQL)]

    Client --> AuthSvc
    Client --> ContentSvc
    Client --> RecSvc

    AuthSvc --> Db
    ContentSvc --> Db
    RecSvc --> Db
```

# Архитектура проекта

## Clean Architecture

```mermaid
flowchart LR
    Domain[Domain]
    Application[Application]
    Infrastructure[Infrastructure]
    WebAPI[WebAPI]
    WpfClient[WpfClient]

    Application --> Domain
    Infrastructure --> Domain
    Infrastructure --> Application
    WebAPI --> Application
    WebAPI --> Infrastructure
    WpfClient --> WebAPI
```

## Поток данных: действие пользователя -> рекомендации

```mermaid
flowchart TD
    userAction[UserActionFromClient] --> apiUserActions[WebAPI_UserActionsController]
    apiUserActions --> actionService[UserActionService]
    actionService --> dbActions[(PostgreSQL_user_actions)]
    dbActions --> recommendationService[RecommendationService]
    recommendationService --> apiRecommendations[WebAPI_RecommendationsController]
    apiRecommendations --> wpfClient[WpfClient_RecommendationsView]
```

## Модель данных (основные таблицы)

```mermaid
flowchart TD
    users[users]
    contents[contents]
    categories[categories]
    tags[tags]
    contentTags[content_tags]
    userActions[user_actions]

    contents --> categories
    contentTags --> contents
    contentTags --> tags
    userActions --> users
    userActions --> contents
```

## Краткое описание алгоритмов рекомендаций

- `Popular`: ранжирование по суммарному весу действий (`View=1`, `Click=2`, `Like=3`).
- `ByCategories`: приоритет контента из категорий, где у пользователя больше активности.
- `KNN`: поиск похожих пользователей по cosine similarity вектора действий, затем рекомендация контента ближайших соседей.


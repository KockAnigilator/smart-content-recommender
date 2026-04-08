namespace SmartContentRecommender.WebClient.Models;

public class DbOverviewVm
{
    public int Users { get; set; }
    public int Categories { get; set; }
    public int Tags { get; set; }
    public int Contents { get; set; }
    public int Actions { get; set; }
}

public class DbUserRowVm
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsBlocked { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class DbCategoryRowVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class DbTagRowVm
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class DbContentRowVm
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public class DbActionRowVm
{
    public Guid Id { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string ContentTitle { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}


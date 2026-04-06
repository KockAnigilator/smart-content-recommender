using SmartContentRecommender.Domain.Enums;

namespace SmartContentRecommender.Application.Admin.Models;

public class UserAdminItemDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool IsBlocked { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class ChangeUserRoleRequest
{
    public UserRole Role { get; set; }
}

public class SetUserBlockRequest
{
    public bool IsBlocked { get; set; }
}


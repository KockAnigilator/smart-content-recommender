namespace SmartContentRecommender.Application.Auth.Models;

public class AuthResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public AuthResponse? Data { get; set; }

    public static AuthResult Success(AuthResponse response, string message = "Успешно")
    {
        return new AuthResult
        {
            IsSuccess = true,
            Message = message,
            Data = response
        };
    }

    public static AuthResult Fail(string message)
    {
        return new AuthResult
        {
            IsSuccess = false,
            Message = message
        };
    }
}


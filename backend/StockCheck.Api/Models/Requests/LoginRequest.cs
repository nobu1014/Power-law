namespace StockCheck.Api.Models.Requests;

/// <summary>
/// ログインリクエスト
/// </summary>
public class LoginRequest
{
    public string LoginId { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

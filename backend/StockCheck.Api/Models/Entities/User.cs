namespace StockCheck.Api.Models.Entities;

/// <summary>
/// ユーザーエンティティ（users テーブル）
/// </summary>
public class User
{
    public int Id { get; set; }
    public string LoginId { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

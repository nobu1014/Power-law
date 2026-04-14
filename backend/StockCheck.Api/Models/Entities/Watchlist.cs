namespace StockCheck.Api.Models.Entities;

/// <summary>
/// ウォッチリストエンティティ
/// user_id でユーザーごとに管理される
/// </summary>
public class Watchlist
{
    public int Id { get; set; }
    public int UserId { get; set; }  // ★ 追加
    public string Symbol { get; set; } = string.Empty;
    public string Market { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
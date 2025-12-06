using System.ComponentModel.DataAnnotations;

namespace VirtualList.Datas;

public class LoginInfo
{
    [Key]
    public byte[] Token { get; set; } = null!;
    public UserInfo User { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

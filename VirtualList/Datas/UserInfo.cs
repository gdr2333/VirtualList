using System.ComponentModel.DataAnnotations;

namespace VirtualList.Datas;

public class UserInfo
{
    [Key]
    public long UserId { get; set; }
    public string UserName { get; set; } = null!;
    // HMACSHA3_384(salt, utf8encoding(password))
    public byte[] PasswordHash { get; set; } = null!;
    public byte[] PasswordSalt { get; set; } = null!;
    public ICollection<SharedFileInfo> SharedFiles { get; set; } = new List<SharedFileInfo>();
    public ICollection<FileNamespaceInfo> OwnNamespaces { get; set; } = new List<FileNamespaceInfo>();
    public ICollection<FileNamespaceInfo> ReadableNamespaces { get; set; } = new List<FileNamespaceInfo>();
    public ICollection<FileNamespaceInfo> WriteableNamespaces { get; set; } = new List<FileNamespaceInfo>();
}

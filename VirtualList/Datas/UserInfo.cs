using System.ComponentModel.DataAnnotations;

namespace VirtualList.Datas;

public class UserInfo
{
    [Key]
    public string UserName { get; set; } = null!;
    // HMACSHA3_384(salt, utf8encoding(password))
    public byte[] PasswordHash { get; set; } = null!;
    public byte[] PasswordSalt { get; set; } = null!;
    public DateTime CreatedTime { get; set; }
    public DateTime LastLogin { get; set; } 
    public ICollection<LoginInfo> LoginInfos { get; set; } = new List<LoginInfo>();
    public ICollection<SharedFileInfo> SharedFiles { get; set; } = new List<SharedFileInfo>();
    public ICollection<FileNamespaceInfo> OwnNamespaces { get; set; } = new List<FileNamespaceInfo>();
    public ICollection<FileNamespaceInfo> ReadableNamespaces { get; set; } = new List<FileNamespaceInfo>();
    public ICollection<FileNamespaceInfo> WriteableNamespaces { get; set; } = new List<FileNamespaceInfo>();
}

using System.ComponentModel.DataAnnotations;

namespace VirtualList.Datas;

public class UserInfo
{
    [Key]
    public string Name { get; set; } = null!;
    // HMACSHA3_384(salt, utf8encoding(password))
    public byte[] PasswordHash { get; set; } = null!;
    public byte[] PasswordSalt { get; set; } = null!;
    public DateTime CreatedTime { get; set; }
    public DateTime LastLogin { get; set; } 
    public ICollection<LoginInfo> LoginInfos { get; set; } = new List<LoginInfo>();
    public ICollection<SharedFileInfo> SharedFiles { get; set; } = new List<SharedFileInfo>();
    public ICollection<FileSpaceInfo> OwnNamespaces { get; set; } = new List<FileSpaceInfo>();
    public ICollection<FileSpaceInfo> ReadableSpaces { get; set; } = new List<FileSpaceInfo>();
    public ICollection<FileSpaceInfo> WriteableSpaces { get; set; } = new List<FileSpaceInfo>();
}

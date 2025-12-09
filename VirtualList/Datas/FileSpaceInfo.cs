using System.ComponentModel.DataAnnotations;

namespace VirtualList.Datas;

public class FileSpaceInfo
{
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string OwnerName { get; set; } = null!;
    public UserInfo Owner { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime LastModifiedAt { get; set; }
    public ICollection<UserInfo> ReadAccessUsers { get; set; } = new List<UserInfo>();
    public ICollection<UserInfo> WriteAccessUsers { get; set; } = new List<UserInfo>();
    public ICollection<SharedFileInfo> SharedFileInfos { get; set; } = new List<SharedFileInfo>();
}

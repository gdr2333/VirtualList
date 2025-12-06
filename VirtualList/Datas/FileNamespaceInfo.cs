using System.ComponentModel.DataAnnotations;

namespace VirtualList.Datas;

public class FileNamespaceInfo
{
    [Key]
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string OwnerName { get; set; } = null!;
    public UserInfo Owner { get; set; } = null!;
    public ICollection<UserInfo> ReadAccessUsers { get; set; } = new List<UserInfo>();
    public ICollection<UserInfo> WriteAccessUsers { get; set; } = new List<UserInfo>();
}

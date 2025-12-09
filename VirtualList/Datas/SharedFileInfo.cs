using System.ComponentModel.DataAnnotations;

namespace VirtualList.Datas;

public class SharedFileInfo
{
    [Key]
    public Guid ShareId { get; set; }
    public string UserName { get; set; }
    public UserInfo User { get; set; } = null!;
    public string RealPath { get; set; } = null!;
    public FileNamespaceInfo FileNamespace { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}

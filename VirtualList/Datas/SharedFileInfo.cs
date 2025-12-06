using System.ComponentModel.DataAnnotations;

namespace VirtualList.Datas;

public class SharedFileInfo
{
    [Key]
    public Guid ShareId { get; set; }

    public UserInfo User { get; set; } = null!;

    public string RealPath { get; set; } = null!;

    public DateTime CreateOn { get; set; }

    public DateTime ExpireOn { get; set; }
}

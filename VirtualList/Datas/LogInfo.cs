using System.ComponentModel.DataAnnotations;

namespace VirtualList.Datas;

public class LogInfo
{
    [Key]
    public ulong Id { get; set; }
}

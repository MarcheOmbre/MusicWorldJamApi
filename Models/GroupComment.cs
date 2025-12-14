using System.ComponentModel.DataAnnotations;

namespace WorldMusicJam.Models;

public class GroupComment
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    public int GroupId { get; set; }
    
    [MaxLength(2000)]
    public string Comment { get; set; } = string.Empty;
}
using System.ComponentModel.DataAnnotations;

namespace WorldMusicJam.Models;

public class Group
{
    public int Id { get; set; }
    
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1700)]
    public string PictureUrl { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
    
    public int UserId { get; set; }
}
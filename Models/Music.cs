using System.ComponentModel.DataAnnotations;

namespace WorldMusicJam.Models;

public class Music
{
    public int Id { get; set; }
    
    [MaxLength(50)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
    
    [MaxLength(Int32.MaxValue)]
    public string Lyrics { get; set; } = string.Empty;
    
    [MaxLength(1700)]
    public string FileUrl { get; set; } = string.Empty;
    
    public int GroupId { get; set; }
}
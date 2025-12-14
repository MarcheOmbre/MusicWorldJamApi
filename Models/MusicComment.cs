using System.ComponentModel.DataAnnotations;

namespace WorldMusicJam.Models;

public class MusicComment
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    public int MusicId { get; set; }
    
    [MaxLength(2000)]
    public string Comment { get; set; } = string.Empty;
}
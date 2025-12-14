using System.ComponentModel.DataAnnotations;

namespace WorldMusicJam.Models;

public class Jam
{
    public int Id { get; set; }
    
    [MaxLength(50)]
    public string Title { get; set; } = string.Empty;
    
    [MaxLength(250)]
    public string Thema { get; set; } = string.Empty;
    
    public DateTime CreatedTime { get; set; }
    
    public DateTime UploadEndTime { get; set; }
    
    public DateTime VoteEndTime { get; set; }
}
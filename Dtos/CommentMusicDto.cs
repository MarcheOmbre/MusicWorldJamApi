using System.ComponentModel.DataAnnotations;

namespace WorldMusicJam.Dtos;

public class CommentMusicDto
{
    public int MusicId { get; set; }
    
    [MaxLength(2000)]
    public string Comment { get; set; } = string.Empty;
}
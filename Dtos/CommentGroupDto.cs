using System.ComponentModel.DataAnnotations;

namespace WorldMusicJam.Dtos;

public class CommentGroupDto
{
    public int GroupId { get; set; }
    
    [MaxLength(2000)]
    public string Comment { get; set; } = string.Empty;
}
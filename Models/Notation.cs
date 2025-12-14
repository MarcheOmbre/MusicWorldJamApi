namespace WorldMusicJam.Models;

public class Notation
{
    public int Id { get; set; }
    
    public int UserId { get; set; }
    
    public int JamId { get; set; }
    
    public int MusicId { get; set; }
    
    public Int16 Note { get; set; }
}
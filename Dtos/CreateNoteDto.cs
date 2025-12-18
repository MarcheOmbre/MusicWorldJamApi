namespace WorldMusicJam.Dtos;

public class CreateNoteDto
{
    public int JamId { get; set; }
    
    public int MusicId { get; set; }
    
    public Int16 Note { get; set; }
}
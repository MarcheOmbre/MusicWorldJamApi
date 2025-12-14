using System.ComponentModel.DataAnnotations;

namespace WorldMusicJam.Dtos;

public class UploadToJamDto
{
    public int JamId { get; init; } = -1;

    [MaxLength(50)] public string Title { get; init; } = string.Empty;

    [MaxLength(1000)] public string Description { get; init; } = string.Empty;

    [MaxLength(Int32.MaxValue)] public string Lyrics { get; init; } = string.Empty;

    [MaxLength(2048)] public string FileUrl { get; init; } = string.Empty;

    public int GroupId { get; init; }
}
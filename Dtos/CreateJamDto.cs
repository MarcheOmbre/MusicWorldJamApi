using System.ComponentModel.DataAnnotations;

namespace WorldMusicJam.Dtos;

public class CreateJamDto
{
    [MaxLength(50)] public string Title { get; init; } = string.Empty;

    [MaxLength(250)] public string Thema { get; init; } = string.Empty;

    public DateTime UploadEndTime { get; init; } = default;

    public DateTime VoteEndTime { get; init; } = default;
}
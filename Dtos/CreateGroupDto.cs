using System.ComponentModel.DataAnnotations;

namespace WorldMusicJam.Dtos;

public class CreateGroupDto
{
    [MaxLength(50)] public string Name { get; init; } = string.Empty;

    [MaxLength(2048)] public string PictureUrl { get; init; } = string.Empty;

    [MaxLength(1000)] public string Description { get; init; } = string.Empty;
}
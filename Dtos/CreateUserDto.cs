namespace WorldMusicJam.Dtos;

public class CreateUserDto
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string PictureUrl { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;
}
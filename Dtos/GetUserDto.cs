using WorldMusicJam.Models;

namespace WorldMusicJam.Dtos;

public class GetUserDto
{
    public int Id { get; init; }
    
    public string Name { get; init; } = string.Empty;

    public string PictureUrl { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    public User.RoleMap Role { get; init; }
}
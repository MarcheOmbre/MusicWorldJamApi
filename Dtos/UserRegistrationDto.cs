namespace WorldMusicJam.Dtos;

public class UserRegistrationDto
{
    public string Name { get; init; } = string.Empty;

    public string PictureUrl { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string PasswordConfirmation { get; init; } = string.Empty;
}
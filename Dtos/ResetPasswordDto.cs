namespace WorldMusicJam.Dtos;

public class ResetPasswordDto
{
    public string Password { get; init; } = string.Empty;

    public string PasswordConfirmation { get; init; } = string.Empty;
}
using System.ComponentModel.DataAnnotations;

namespace WorldMusicJam.Models;

public class AuthentificationUser
{
    public int Id { get; init; }
    
    [MaxLength(50)]
    public string Email { get; init; } = string.Empty;
    
    public byte[] PasswordHash { get; init; } = [];
    
    public byte[] PasswordSalt { get; init; } = [];
}
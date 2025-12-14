using System.ComponentModel.DataAnnotations;

namespace WorldMusicJam.Models;

public class User
{
    public enum RoleMap
    {
        Admin = 10, 
        User = 20,
        None = 0
    }
    
    public int Id { get; set; }
    
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [MaxLength(1700)]
    public string PictureUrl { get; set; } = string.Empty;
    
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
    
    [MaxLength(25)]
    public RoleMap Role { get; set; } = RoleMap.None;
}
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using WorldMusicJam.Models;
using WorldMusicJam.Repositories;

namespace WorldMusicJam.Helpers;

public static class TokenHelper
{
    private const string UserKey = "UserId";
    
    internal static string CreateToken(IConfiguration configuration,int value, TimeSpan timeSpan)
    {
        var tokenSecretKey = configuration["AppSettings:JWTSecret"] ?? throw new Exception("No JWTSecret not defined");

        // Create claims
        var claims = new Claim[] { new(UserKey, value.ToString()) };

        // Crete a security key from the secret key
        var tokenKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(tokenSecretKey));

        // Generate credentials
        var signingCredentials = new SigningCredentials(tokenKey, SecurityAlgorithms.HmacSha512Signature);

        // Create a token descriptor to be used for creating the token
        var securityTokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            SigningCredentials = signingCredentials,
            Expires = DateTime.UtcNow + timeSpan
        };

        // Create token
        var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();

        // Convert token to string
        return jwtSecurityTokenHandler.WriteToken(jwtSecurityTokenHandler.CreateToken(securityTokenDescriptor));
    }
    
    internal static bool CheckToken(ClaimsPrincipal? claimsPrincipal, IUserRepository userRepository, out int id)
    {
        id = -1;
        
        var claimsIdentity = claimsPrincipal?.Identity as ClaimsIdentity;
        if(claimsIdentity is null)
            return false;
        
        var idClaims = claimsIdentity.Claims.FirstOrDefault(x => x.Type == UserKey);
        if(idClaims is null || !int.TryParse(idClaims.Value, out id))
            return false;
        
        return userRepository.TryGetById<AuthentificationUser>(id, out _);
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorldMusicJam.Dtos;
using WorldMusicJam.Helpers;
using WorldMusicJam.Models;
using WorldMusicJam.Repositories;

namespace WorldMusicJam.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class AuthentificationController(IConfiguration configuration, IUserRepository userRepository) : ControllerBase
{
    private static readonly TimeSpan LoginTokenTimeSpan = new(1, 0, 0, 0);
    private static readonly TimeSpan PasswordForgotTokenTimeSpan = new(0, 0, 5, 0);


    #region Gets

    [HttpGet("RefreshToken")]
    public IActionResult RefreshToken()
    {
        if (!TokenHelper.CheckToken(User, userRepository, out var userId))
            return Unauthorized();

        return Ok(TokenHelper.CreateToken(configuration, userId, LoginTokenTimeSpan));
    }

    #endregion


    #region Posts

    /// <reponse code="200">User registered</reponse>
    /// <reponse code="400">Bad request: check the error message</reponse>
    [AllowAnonymous]
    [HttpPost("Register")]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    public IActionResult Register(UserRegistrationDto userRegistrationDto)
    {
        if (!EmailHelper.IsValidEmail(userRegistrationDto.Email))
            return BadRequest("The email is not valid");
        if (!EmailHelper.IsValidEmail(userRegistrationDto.Email))
            return BadRequest("The email is not valid");
        if (string.IsNullOrWhiteSpace(userRegistrationDto.Password))
            return BadRequest("Password can't be null");
        if (userRegistrationDto.Password != userRegistrationDto.PasswordConfirmation)
            return BadRequest("Passwords don't match");

        var salt = PasswordHelper.GenerateSalt();
        var passwordHash = PasswordHelper.GetPasswordHash(configuration, userRegistrationDto.Password, salt);

        var result = userRepository.ExecuteStoreProcedure<int>("dbo.spUserCreate",
            new Tuple<string, object>("email", userRegistrationDto.Email),
            new Tuple<string, object>("passwordHash", passwordHash),
            new Tuple<string, object>("passwordSalt", salt),
            new Tuple<string, object>("name", userRegistrationDto.Name),
            new Tuple<string, object>("pictureUrl", userRegistrationDto.PictureUrl),
            new Tuple<string, object>("pictureUrl", userRegistrationDto.Description));

        var resultCode = result.Length > 0 ? result[0] : -1;
        switch (resultCode)
        {
            case 1 :
                return BadRequest("One of the parameters is null");
            case 2 :
                return BadRequest("Email already registered");
            case 3 :
                return BadRequest("Name already registered");
            case 0 :
                return Ok("User created");
            default:
                return BadRequest("An unexpected error occured when subscribing the user");
        }
    }

    /// <reponse code="200">User logged</reponse>
    /// <reponse code="400">Bad request: check the error message</reponse>
    [AllowAnonymous]
    [HttpPost("Log")]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    public IActionResult Log(UserLoginDto userLoginDto)
    {
        if (!EmailHelper.IsValidEmail(userLoginDto.Email))
            return BadRequest("The email is not valid");

        if (string.IsNullOrWhiteSpace(userLoginDto.Password))
            return BadRequest("Password can't be null");

        var userAuthentification = userRepository.Get<AuthentificationUser>(x => x.Email == userLoginDto.Email);
        if (userAuthentification != null)
        {
            var passwordHash = PasswordHelper.GetPasswordHash(configuration, userLoginDto.Password,
                userAuthentification.PasswordSalt);

            if (passwordHash.SequenceEqual(userAuthentification.PasswordHash))
            {
                if (!userRepository.TryGetById<User>(userAuthentification.Id, out var user))
                    return BadRequest("User not found");

                return Ok(TokenHelper.CreateToken(configuration, user.Id, LoginTokenTimeSpan));
            }
        }

        return BadRequest("User or password incorrect");
    }

    [AllowAnonymous]
    [HttpPost("ForgotPassword")]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    public IActionResult ForgotPassword(string email)
    {
        if (!EmailHelper.IsValidEmail(email))
            return BadRequest("The email is not valid");
        
        var userAuthentification = userRepository.Get<AuthentificationUser>(x => x.Email == email);
        if (userAuthentification != null)
        {
            var token = TokenHelper.CreateToken(configuration, userAuthentification.Id, PasswordForgotTokenTimeSpan);
            
            // Send mail
            EmailHelper.SendEmail(configuration, email, "Reset your password", token);
        }

        return Ok("If the email exists, an email has been sent");
    }
    
    [HttpPost("ResetPassword")]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    public IActionResult ResetPassword(ResetPasswordDto resetPasswordDto)
    {
        if (!TokenHelper.CheckToken(User, userRepository, out var id))
            return Unauthorized();
        
        if (string.IsNullOrWhiteSpace(resetPasswordDto.Password))
            return BadRequest("Password can't be null");
        if (resetPasswordDto.Password != resetPasswordDto.PasswordConfirmation)
            return BadRequest("Passwords don't match");

        var salt = PasswordHelper.GenerateSalt();
        var passwordHash = PasswordHelper.GetPasswordHash(configuration, resetPasswordDto.Password, salt);

        var result = userRepository.ExecuteStoreProcedure<int>($"{Constants.AuthentificationSchema}.spUserPasswordReset",
            new Tuple<string, object>("userId", id),
            new Tuple<string, object>("passwordHash", passwordHash),
            new Tuple<string, object>("passwordSalt", salt));

        var resultCode = result.Length > 0 ? result[0] : -1;
        return resultCode switch
        {
            1 => BadRequest("One of the parameters is null"),
            0 => Ok("Password reset"),
            _ => BadRequest("An unexpected error occured when subscribing the user")
        };
    }

    #endregion
}
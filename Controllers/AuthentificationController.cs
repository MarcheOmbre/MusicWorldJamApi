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
    private static readonly TimeSpan LoginTokenTimeSpan = new(0, 1, 0, 0);
    private static readonly TimeSpan PasswordForgotTokenTimeSpan = new(0, 0, 5, 0);


    #region Gets

    [HttpGet("RefreshToken")]
    public IActionResult RefreshToken()
    {
        if (!TokenHelper.CheckToken(User, userRepository, out var userId, out var userRole))
            return Unauthorized();

        return Ok(TokenHelper.CreateToken(configuration, userId, userRole, LoginTokenTimeSpan));
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


        if (userRepository
            .GetAll<AuthentificationUser>(authentificationUser =>
                authentificationUser.Email == userRegistrationDto.Email).Any())
            return BadRequest("Email already registered");

        var users = userRepository.GetAll<User>(null);
        if (users.Any(user => user.Name == userRegistrationDto.Name))
            return BadRequest("Name already registered");

        if (users.Any(x => x.PictureUrl == userRegistrationDto.PictureUrl))
            return BadRequest("Cannot share the same picture url");

        var salt = PasswordHelper.GenerateSalt();
        var passwordHash = PasswordHelper.GetPasswordHash(configuration, userRegistrationDto.Password, salt);

        var authentificationUser = new AuthentificationUser
        {
            Email = userRegistrationDto.Email,
            PasswordHash = passwordHash,
            PasswordSalt = salt
        };

        if (!userRepository.Add(authentificationUser) || !userRepository.SaveChanges())
            return BadRequest("An error occured while registering the authentification user");

        // Get the authentification user id
        if (!UserController.CreateInternal(userRepository, new CreateUserDto
            {
                Id = authentificationUser.Id,
                Name = userRegistrationDto.Name,
                PictureUrl = userRegistrationDto.PictureUrl,
                Description = userRegistrationDto.Description,
                Email = userRegistrationDto.Email
            }, out var error))
            return BadRequest(error);

        return Ok("User registered");
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

                return Ok(TokenHelper.CreateToken(configuration, user.Id, user.Role, LoginTokenTimeSpan));
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
        if (userAuthentification != null && userRepository.TryGetById<User>(userAuthentification.Id, out var user))
        {
            var token = TokenHelper.CreateToken(configuration, user.Id, user.Role, PasswordForgotTokenTimeSpan);
            
            // Send mail
            EmailHelper.SendEmail(configuration, email, "Subject", token);
        }

        return Ok("If the email exists, an email has been sent");
    }

    #endregion
}
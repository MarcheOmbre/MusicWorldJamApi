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
public class UserController(IUserRepository userRepository) : ControllerBase
{
    internal static bool CreateInternal(IUserRepository userRepository, CreateUserDto createUserDto, out string error)
    {
        if(userRepository == null)
            throw new ArgumentNullException(nameof(userRepository));
        
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(createUserDto.Name))
        {
            error = "Name can't be null";
            return false;
        }

        var user = new User
        {
            Id = createUserDto.Id,
            Name = createUserDto.Name,
            PictureUrl = createUserDto.PictureUrl,
            Description = createUserDto.Description,
            Role = Models.User.RoleMap.User
        };

        if (!userRepository.Add(user) || !userRepository.SaveChanges())
        {
            error = "An error occured while adding the user";
            return false;
        }

        return true;
    }

    #region Gets
    
    [HttpGet("Get")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult Get(int id)
    {
        if (!TokenHelper.CheckToken(User, userRepository, out _))
            return Unauthorized();

        if (!userRepository.TryGetById<User>(id, out var user))
            return NotFound("User not found");

        return Ok(new GetUserDto
        {
            Name = user.Name,
            Description = user.Description,
            Role = user.Role,
            PictureUrl = user.PictureUrl
        });
    }

    [HttpGet("GetAll")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult GetAll()
    {
        if (!TokenHelper.CheckToken(User, userRepository, out _))
            return Unauthorized();

        var getUsersDtos = userRepository.GetAll<User>(null).Select(x => new GetUserDto
        {
            Name = x.Name,
            Description = x.Description,
            Role = x.Role,
            PictureUrl = x.PictureUrl
        });
        
        return Ok(getUsersDtos);
    }

    #endregion


    #region Deletes

        /// <reponse code="200">User edited</reponse>
    /// <reponse code="400">Bad request: One of the fields not valid</reponse>
    [HttpDelete("Delete")]
    [ProducesResponseType(statusCode: StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    public IActionResult Delete(int id)
    {
        if (!TokenHelper.CheckToken(User, userRepository, out var tokenUserId) || 
            !userRepository.TryGetById<User>(tokenUserId, out var user))
            return Unauthorized();
        
        if (user.Role != Models.User.RoleMap.Admin || user.Role == Models.User.RoleMap.Admin)
        {
            if (tokenUserId != id)
                return BadRequest("You can't delete another user");
        }
        
        var result = userRepository.ExecuteStoreProcedure<int>("dbo.spUserDelete", new Tuple<string, object>("id", id));
        if (result.Length <= 0 || result[0] != 1)
            return BadRequest("An error occured while deleting the user");
        
        return Ok("User deleted");
    }

    #endregion
}
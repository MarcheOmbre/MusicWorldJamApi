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
            Id = user.Id,
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
            Id = x.Id,
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

        var result = userRepository.ExecuteStoreProcedure<int>("dbo.spUserDelete",
            new Tuple<string, object>("deleteUserId", id),
            new Tuple<string, object>("userId", user.Id));
        
        var resultCode = result.Length > 0 ? result[0] : -1;
        switch (resultCode)
        {
            case 1 :
                return BadRequest("One of the parameters is null");
            case 2 :
                return BadRequest("The user does not exist");
            case 3 :
                return BadRequest("You do not have the permission to delete this user");
            case 0 :
                return Ok("User deleted");
            default:
                return BadRequest("An unexpected error occured");
        }
    }

    #endregion
}
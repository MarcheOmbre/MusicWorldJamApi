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
        if (!TokenHelper.CheckToken(User, userRepository, out _, out _))
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
        if (!TokenHelper.CheckToken(User, userRepository, out _, out _))
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
        if (!TokenHelper.CheckToken(User, userRepository, out var tokenUserId, out var tokenRoleId))
            return Unauthorized();
        
        if (!userRepository.TryGetById<User>(id, out var user))
            return BadRequest("User not found");
        
        if (tokenRoleId != Models.User.RoleMap.Admin || user.Role == Models.User.RoleMap.Admin)
        {
            if (tokenUserId != id)
                return BadRequest("You can't delete another user");
        }
        
        var groupInvitationsToDelete = userRepository.GetAll<GroupInvitation>(x => x.UserId == id);
        foreach (var groupInvitation in groupInvitationsToDelete)
            userRepository.Remove<GroupInvitation>(groupInvitation.Id);
        
        var notationsToDelete = userRepository.GetAll<Notation>(x => x.UserId == id);
        foreach (var notation in notationsToDelete)
            userRepository.Remove<Notation>(notation.Id);
        
        var musicCommentsToDelete = userRepository.GetAll<MusicComment>(x => x.UserId == id);
        foreach (var musicComment in musicCommentsToDelete)
            userRepository.Remove<MusicComment>(musicComment.Id);
        
        var groupCommentsToDelete = userRepository.GetAll<GroupComment>(x => x.UserId == id);
        foreach (var groupComment in groupCommentsToDelete)
            userRepository.Remove<GroupComment>(groupComment.Id);
        
        if((groupInvitationsToDelete.Count > 0 || 
            notationsToDelete.Count > 0 || 
            musicCommentsToDelete.Count > 0 ||
            groupCommentsToDelete.Count > 0) 
           && !userRepository.SaveChanges())
            return BadRequest("An error occured when deleting the user id from join tables");
            
        foreach (var groupId in GroupController.GetMembersInternal(userRepository, id))
        {
            if (!userRepository.TryGetById<Group>(groupId, out var group))
                continue;
            
            if (group.UserId != id && !GroupController.TryRemoveMemberInternal(userRepository, id, groupId, out var removeMemberError))
                return BadRequest(removeMemberError);
            
            if (group.UserId == id && !GroupController.TryDeleteInternal(userRepository, groupId, out var removeGroupError))
                return BadRequest(removeGroupError);
        }

        if (!userRepository.Remove<User>(id) || !userRepository.SaveChanges())
            return BadRequest("Cannot delete the user");

        if (!userRepository.Remove<AuthentificationUser>(id) || !userRepository.SaveChanges())
            return BadRequest("Cannot delete the authentification user");

        return Ok("User deleted");
    }

    #endregion
}
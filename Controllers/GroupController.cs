using Microsoft.AspNetCore.Mvc;
using WorldMusicJam.Dtos;
using WorldMusicJam.Helpers;
using WorldMusicJam.Models;
using WorldMusicJam.Repositories;

namespace WorldMusicJam.Controllers;

[ApiController]
[Route("[controller]")]
public class GroupController(IUserRepository userRepository) : ControllerBase
{
    #region Gets
    
    [HttpGet("GetUserGroups")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult GetUserGroups(int userId)
    {
        if(!TokenHelper.CheckToken(User, userRepository, out var tokenUserId))
            return Unauthorized();

        return Ok(userRepository.ExecuteStoreProcedure<Group>($"{Constants.MainSchema}.spUserGroupsGet",
            new Tuple<string, object>("targetUserId", userId),
            new Tuple<string, object>("userId", tokenUserId)));
    }
    
    [HttpGet("GetMusics")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult GetMusics(int id)
    {
        if(!TokenHelper.CheckToken(User, userRepository, out var tokenUserId))
            return Unauthorized();

        return Ok(userRepository.ExecuteStoreProcedure<Music>($"{Constants.MainSchema}.spGroupMusicsGet",
            new Tuple<string, object>("groupId", id),
            new Tuple<string, object>("userId", tokenUserId)));
    }
    
    [HttpGet("GetMembers")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult GetMembers(int id)
    {
        if(!TokenHelper.CheckToken(User, userRepository, out var tokenUserId))
            return Unauthorized();

        return Ok(userRepository.ExecuteStoreProcedure<GetUserDto>($"{Constants.MainSchema}.spGroupUsersGet",
            new Tuple<string, object>("groupId", id),
            new Tuple<string, object>("userId", tokenUserId)));
    }
    
    [HttpGet("GetComments")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult GetComments(int id)
    {
        if(!TokenHelper.CheckToken(User, userRepository, out var tokenUserId))
            return Unauthorized();

        return Ok(userRepository.ExecuteStoreProcedure<GroupComment>($"{Constants.MainSchema}.spGroupCommentsGet",
            new Tuple<string, object>("groupId", id),
            new Tuple<string, object>("userId", tokenUserId)));
    }
    
    [HttpGet("GetAll")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult GetAll()
    {
        if(!TokenHelper.CheckToken(User, userRepository, out _))
            return Unauthorized();
        
        return Ok(userRepository.GetAll<Group>(null));
    }
    
    #endregion
    
    
    #region Puts
    
    [HttpPut("Create")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult Create(CreateGroupDto groupDto)
    {
        if (string.IsNullOrWhiteSpace(groupDto.Name))
            return BadRequest("Group name is empty");
        
        if(!TokenHelper.CheckToken(User, userRepository, out var tokenUserId))
            return Unauthorized();

        var result = userRepository.ExecuteStoreProcedure<int>($"{Constants.MainSchema}.spGroupCreate",
            new Tuple<string, object>("name", groupDto.Name),
            new Tuple<string, object>("pictureUrl", groupDto.PictureUrl),
            new Tuple<string, object>("description", groupDto.Description),
            new Tuple<string, object>("userId", tokenUserId));

        var resultCode = result.Length > 0 ? result[0] : -1;
        switch (resultCode)
        {
            case 1:
                return BadRequest("One of the parameters is null");
            case 2:
                return BadRequest("User doesn't exist");
            case 3:
                return Unauthorized();
            case 0:
                return Ok("Group created");
            default:
                return BadRequest("An unexpected error occured");
        }
    }
    
    [HttpPut("SendInvitation")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult SendInvitation(int groupId, int userId)
    {
        if (!TokenHelper.CheckToken(User, userRepository, out var tokenUserId))
            return Unauthorized();
        
        var result = userRepository.ExecuteStoreProcedure<int>($"{Constants.MainSchema}.spGroupInvitationSend",
            new Tuple<string, object>("memberId", userId),
            new Tuple<string, object>("groupId", groupId),
            new Tuple<string, object>("userId", tokenUserId));

        var resultCode = result.Length > 0 ? result[0] : -1;
        switch (resultCode)
        {
            case 1:
                return BadRequest("One of the parameters is null");
            case 2:
                return BadRequest("You can't invite a user that is already a member of the group");
            case 3:
                return BadRequest("The user doesn't exist");
            case 4:
                return BadRequest("The group doesn't exist or you don't have permission");
            case 5:
                return BadRequest("An invitation already exists to this user from this group");
            case 0:
                return Ok("Invitation sent");
            default:
                return BadRequest("An unexpected error occured");
        }
    }
    
    [HttpPut("AddComment")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult AddComment(CommentGroupDto commentGroupDto)
    {
        if (!TokenHelper.CheckToken(User, userRepository, out var tokenUserId))
            return Unauthorized();
        
        if(string.IsNullOrEmpty(commentGroupDto.Comment))
            return BadRequest("Comment is empty");
        
        var result = userRepository.ExecuteStoreProcedure<int>($"{Constants.MainSchema}.spGroupCommentAdd",
            new Tuple<string, object>("groupId", commentGroupDto.GroupId),
            new Tuple<string, object>("comment", commentGroupDto.Comment),
            new Tuple<string, object>("userId", tokenUserId));

        var resultCode = result.Length > 0 ? result[0] : -1;
        switch (resultCode)
        {
            case 1:
                return BadRequest("One of the parameters is null");
            case 2:
                return BadRequest("You do not have permission or the group doesn't exist");
            case 3:
                return BadRequest("You can't send a comment to your own group");
            case 4:
                return BadRequest("You already sent a comment to this group");
            case 0:
                return Ok("Comment sent");
            default:
                return BadRequest("An unexpected error occured");
        }
    }
    
    #endregion
    
    
    #region Posts
    
    [HttpPost("AcceptInvitation")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult AcceptInvitation(int groupId)
    {
        if (!TokenHelper.CheckToken(User, userRepository, out var tokenUserId))
            return Unauthorized();
        
        var result = userRepository.ExecuteStoreProcedure<int>($"{Constants.MainSchema}.spGroupInvitationAccept",
            new Tuple<string, object>("groupId", groupId),
            new Tuple<string, object>("userId", tokenUserId));

        var resultCode = result.Length > 0 ? result[0] : -1;
        switch (resultCode)
        {
            case 1:
                return BadRequest("One of the parameters is null");
            case 2:
                return BadRequest("No invitation exists for you from this group");
            case 0:
                return Ok("Group joined");
            default:
                return BadRequest("An unexpected error occured");
        }
    }
    
    #endregion
    
    
    #region Deletes
    
    [HttpDelete("RemoveComment")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult RemoveComment(int commentId)
    {
        if (!TokenHelper.CheckToken(User, userRepository, out var tokenUserId))
            return Unauthorized();
        
        var result = userRepository.ExecuteStoreProcedure<int>($"{Constants.MainSchema}.spGroupCommentDelete",
            new Tuple<string, object>("commentId", commentId),
            new Tuple<string, object>("userId", tokenUserId));

        var resultCode = result.Length > 0 ? result[0] : -1;
        switch (resultCode)
        {
            case 1:
                return BadRequest("One of the parameters is null");
            case 2:
                return BadRequest("The comment doesn't exist or you don't have permission");
            case 0:
                return Ok("Comment deleted");
            default:
                return BadRequest("An unexpected error occured");
        }
    }
    
    [HttpDelete("Delete")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult Delete(int id)
    {
        if (!TokenHelper.CheckToken(User, userRepository, out var tokenUserId))
            return Unauthorized();

        var result = userRepository.ExecuteStoreProcedure<int>($"{Constants.MainSchema}.spGroupDelete",
            new Tuple<string, object>("groupId", id),
            new Tuple<string, object>("userId", tokenUserId));
        var resultCode = result.Length > 0 ? result[0] : -1;
        
        switch (resultCode)
        {
            case 1 :
                return BadRequest("One of the parameters is null");
            case 2 :
                return BadRequest("The group doesn't exist or you do not have permission");
            case 0 :
                return Ok("Group deleted");
            default:
                return BadRequest("An unexpected error occured");
        }
    }

    [HttpDelete("DeleteMember")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult DeleteMember(int memberId, int id)
    {
        if (!TokenHelper.CheckToken(User, userRepository, out var tokenUserId))
            return Unauthorized();

        var result = userRepository.ExecuteStoreProcedure<int>($"{Constants.MainSchema}.spGroupMemberDelete",
            new Tuple<string, object>("memberId", memberId),
            new Tuple<string, object>("groupId", id),
            new Tuple<string, object>("userId", tokenUserId));

        var resultCode = result.Length > 0 ? result[0] : -1;
        switch (resultCode)
        {
            case 1:
                return BadRequest("One of the parameters is null");
            case 2:
                return BadRequest("You do not have permission to delete this member");
            case 3:
                return BadRequest(
                    "You are the owner of the group, you can't remove yourself, please delete the group instead");
            case 4:
                return BadRequest("The member doesn't exist");
            case 0:
                return Ok("Member deleted from the group");
            default:
                return BadRequest("An unexpected error occured");
        }
    }

    #endregion
}
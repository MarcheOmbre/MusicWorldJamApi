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
    internal static IEnumerable<int> GetMembersInternal(IUserRepository userRepository, int id)
    {
        if(userRepository == null)
            throw new ArgumentNullException(nameof(userRepository));
        
        return userRepository.GetAll<UserGroupJoin>(x => x.GroupId == id).Select(x => x.UserId);
    }
    
    internal static IEnumerable<int> GetMusicsInternal(IUserRepository userRepository, int groupId)
    {
        if (userRepository == null)
            throw new ArgumentNullException(nameof(userRepository));

        return userRepository.GetAll<GroupMusicJoin>(x => x.GroupId == groupId).Select(x => x.MusicId);
    }
    
    #region Gets
    
    [HttpGet("GetUserGroups")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult GetUserGroups(int userId)
    {
        if(!TokenHelper.CheckToken(User, userRepository, out _))
            return Unauthorized();

        var groups = new List<Group>();
        foreach (var groupId in GetMembersInternal(userRepository, userId))
        {
            if (!userRepository.TryGetById<Group>(groupId, out var group))
                continue;
            
            groups.Add(group);
        }
        return Ok(groups);
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
    
    [HttpGet("GetMembers")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult GetMembers(int id)
    {
        if (!TokenHelper.CheckToken(User, userRepository, out _))
            return Unauthorized();
        
        var memberIds = userRepository.GetAll<UserGroupJoin>(x => x.GroupId == id).Select(x => x.UserId).ToList();
        if(!memberIds.Any()) 
            return NotFound("No member found");
        
        var users = new List<User>();
        foreach (var memberId in memberIds)
        {
            if(!userRepository.TryGetById<User>(memberId, out var user))
                continue;
            
            users.Add(user);
        }
        
        return Ok(users);
    }
    
    [HttpGet("GetComments")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult GetComments(int id)
    {
        if (!TokenHelper.CheckToken(User, userRepository, out _))
            return Unauthorized();
        
        if(!userRepository.TryGetById<Group>(id, out _))
            return NotFound("Group not found");
        
        return Ok(userRepository.GetAll<GroupComment>(x => x.GroupId == id));
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
        
        var result = userRepository.ExecuteStoreProcedure<int>($"{Constants.MainSchema}.spGroupSendInvitation",
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
        
        var result = userRepository.ExecuteStoreProcedure<int>($"{Constants.MainSchema}.spGroupAddComment",
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
        
        var result = userRepository.ExecuteStoreProcedure<int>($"{Constants.MainSchema}.spGroupAcceptInvitation",
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
        
        var result = userRepository.ExecuteStoreProcedure<int>($"{Constants.MainSchema}.spGroupDeleteComment",
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
        if (!TokenHelper.CheckToken(User, userRepository, out var tokenUserId) || 
            !userRepository.TryGetById<User>(tokenUserId, out var user))
            return Unauthorized();

        var result = userRepository.ExecuteStoreProcedure<int>($"{Constants.MainSchema}.spGroupDelete",
            new Tuple<string, object>("groupId", id),
            new Tuple<string, object>("userId", user.Id));
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

        var result = userRepository.ExecuteStoreProcedure<int>($"{Constants.MainSchema}.spGroupDeleteMember",
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
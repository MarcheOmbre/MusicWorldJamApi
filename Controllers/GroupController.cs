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

    internal static bool TryRemoveMemberInternal(IUserRepository userRepository, int memberId, int id, out string error)
    {
        if(userRepository == null)
            throw new ArgumentNullException(nameof(userRepository));
        
        error = string.Empty;
        
        var userGroupJoinToDelete = userRepository.Get<UserGroupJoin>(x => x.GroupId == id && x.UserId == memberId);
        if (userGroupJoinToDelete is null)
        {
            error = "User is not a member of this group";
            return false;
        }
        
        userRepository.Remove<UserGroupJoin>(userGroupJoinToDelete.Id);
        return userRepository.SaveChanges();
    }
    
    internal static bool TryDeleteInternal(IUserRepository userRepository, int id, out string error)
    {
        if(userRepository == null)
            throw new ArgumentNullException(nameof(userRepository));
        
        error = string.Empty;

        if (!userRepository.TryGetById<Group>(id, out _))
        {
            error = "Group not found";
            return false;
        }
        
        var result = userRepository.ExecuteStoreProcedure<int>($"{Constants.MainSchema}.spGroupDelete", new Tuple<string, object>("id", id));
        if (result.Length <= 0 || result[0] != 1)
        {
            error = "An error occured while deleting the music";
            return false;
        }
        
        return true;
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

        var groups = userRepository.GetAll<Group>(null);
        
        if (groups.Any(group => group.Name == groupDto.Name))
            return BadRequest("Group already exists");
        
        if(groups.Any(group => group.PictureUrl == groupDto.PictureUrl))
            return BadRequest("Cannot share the same picture url");
        
        var group = new Group
        {
            Name = groupDto.Name,
            UserId = tokenUserId,
            Description = groupDto.Description,
            PictureUrl = groupDto.PictureUrl
        };

        if (userRepository.Add(group) && userRepository.SaveChanges())
        {
            userRepository.Add(new UserGroupJoin { UserId = tokenUserId, GroupId = group.Id });
         
            if(userRepository.SaveChanges()) 
                return Ok("Group created");   
        }
        
        return NotFound("An error occured while creating the group");
    }
    
    [HttpPut("SendInvitation")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult SendInvitation(int groupId, int userId)
    {
        if (!TokenHelper.CheckToken(User, userRepository, out var tokenUserId))
            return Unauthorized();
        
        if(!userRepository.TryGetById<Group>(groupId, out var group))
            return NotFound("Group not found");
        
        if(group.UserId != tokenUserId)
            return BadRequest("You are not the owner of this group");
        
        if(group.UserId == userId)
            return BadRequest("You cannot invite yourself");
        
        if(userRepository.Get<User>(x => x.Id != userId) is null)
            return BadRequest("User not found");
        
        if(userRepository.Get<GroupInvitation>(x => x.GroupId == groupId && x.UserId == userId) is not null)
            return BadRequest("You have already invited this user");
        
        userRepository.Add(new GroupInvitation { GroupId = groupId, UserId = userId });
        if(!userRepository.SaveChanges())
            return BadRequest("An error occured while inviting the user");
        
        return Ok("Invited");
    }
    
    [HttpPut("AddComment")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult AddComment(CommentGroupDto commentGroupDto)
    {
        if (!TokenHelper.CheckToken(User, userRepository, out var tokenUserId))
            return Unauthorized();
        
        if(!userRepository.TryGetById<Group>(commentGroupDto.GroupId, out var music))
            return BadRequest("Group not found");
        
        var members = GetMembersInternal(userRepository, commentGroupDto.GroupId);
        
        if(members.Contains(tokenUserId))
            return BadRequest("You cannot comment your own group");

        if(userRepository.Get<GroupComment>(x => x.GroupId == commentGroupDto.GroupId && x.UserId == tokenUserId) is not null)
            return BadRequest("You already commented this music");
        
        if(!userRepository.Add(new GroupComment
           {
               GroupId = commentGroupDto.GroupId, 
               UserId = tokenUserId, 
               Comment = commentGroupDto.Comment
           }) || !userRepository.SaveChanges())
            return BadRequest("An error occured while adding the comment");
        
        return Ok("Comment added");
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
        
        if(!userRepository.TryGetById<Group>(groupId, out _))
            return NotFound("Group not found");

        var invitation = userRepository.Get<GroupInvitation>(x => x.GroupId == groupId && x.UserId == tokenUserId);
        if(invitation is null)
            return BadRequest("You have not been invited to this group");
        
        userRepository.Add(new UserGroupJoin { UserId = tokenUserId, GroupId = groupId });
        if(!userRepository.SaveChanges())
            return BadRequest("An error occured while adding the user to the group");
        
        userRepository.Remove<GroupInvitation>(invitation.Id);
        if(!userRepository.SaveChanges())
            return BadRequest("An error occured while removing the invitation");
        
        return Ok("Joined");
    }
    
    #endregion
    
    
    #region Deletes
    
    [HttpDelete("RemoveComment")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult RemoveComment(int commentId)
    {
        if (!TokenHelper.CheckToken(User, userRepository, out var tokenUserId) || 
            !userRepository.TryGetById<User>(tokenUserId, out var user))
            return Unauthorized();
        
        if(!userRepository.TryGetById<GroupComment>(commentId, out var comment))
            return BadRequest("Comment not found");
        
        if(user.Role != Models.User.RoleMap.Admin && comment.UserId != tokenUserId)
            return BadRequest("You are not the owner of this comment");
        
        if(!userRepository.Remove<GroupComment>(commentId) || !userRepository.SaveChanges())
            return BadRequest("An error occured while deleting the comment");
        
        return Ok("Comment deleted");
    }
    
    [HttpDelete("Delete")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult Delete(int id)
    {
        if (!TokenHelper.CheckToken(User, userRepository, out var tokenUserId) || 
            !userRepository.TryGetById<User>(tokenUserId, out var user))
            return Unauthorized();
        
        var userId = tokenUserId;
        if(!userRepository.TryGetById<Group>(id, out var group))
            return NotFound("Group not found");

        if (user.Role != Models.User.RoleMap.Admin)
        {
            if (group.UserId != userId)
            {
                return BadRequest("You are not the owner of this group");
            }
        }
        
        if(!TryDeleteInternal(userRepository, id, out var error))
            return BadRequest(error);
        
        return Ok("Group deleted");
    }
    
    [HttpDelete("DeleteMember")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult DeleteMember(int memberId, int id)
    {
        if(!TokenHelper.CheckToken(User, userRepository, out var tokenUserId))
            return Unauthorized();
        
        if(!userRepository.TryGetById<Group>(id, out var group))
            return NotFound("Group not found");
        
        if(group.UserId != tokenUserId)
            return BadRequest("You are not the owner of this group");

        if (group.UserId == memberId)
            return BadRequest("You cannot delete the owner of the group, please delete the group instead");
        
        if(!TryRemoveMemberInternal(userRepository, memberId, id, out var error))
            return BadRequest(error);
        
        return Ok("Group member deleted");
    }
    
    #endregion
}
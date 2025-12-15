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
public class MusicController(IUserRepository userRepository) : ControllerBase
{
    internal static IEnumerable<int> GetGroupMusicsIdsInternal(IUserRepository userRepository, int groupId)
    {
        if (userRepository == null)
            throw new ArgumentNullException(nameof(userRepository));

        return userRepository.GetAll<GroupMusicJoin>(x => x.GroupId == groupId).Select(x => x.MusicId);
    }

    internal static bool TryAddInternal(IUserRepository userRepository, int userId, UploadToJamDto uploadToJamDto,
        out string error, out Music music)
    {
        if (userRepository == null)
            throw new ArgumentNullException(nameof(userRepository));

        error = string.Empty;
        music = null;

        if (string.IsNullOrWhiteSpace(uploadToJamDto.Title))
        {
            error = "Title can't be null";
            return false;
        }

        if (string.IsNullOrWhiteSpace(uploadToJamDto.FileUrl))
        {
            error = "File url can't be null";
            return false;
        }

        if (!userRepository.TryGetById<Group>(uploadToJamDto.GroupId, out var group))
        {
            error = "Group not found";
            return false;
        }

        if (group.UserId != userId)
        {
            error = "You are not the owner of this group";
            return false;
        }

        var retrieveMusics = userRepository.GetAll<Music>(null);
        foreach (var retrieveMusic in retrieveMusics)
        {
            if (retrieveMusic.GroupId == uploadToJamDto.GroupId && retrieveMusic.Title == uploadToJamDto.Title)
            {
                error = "A music with this title already exists for this group";
                return false;
            }

            if (retrieveMusic.FileUrl == uploadToJamDto.FileUrl)
            {
                error = "A music with this file url already exists";
                return false;
            }
        }

        music = new Music
        {
            Title = uploadToJamDto.Title,
            Description = uploadToJamDto.Description,
            Lyrics = uploadToJamDto.Lyrics,
            FileUrl = uploadToJamDto.FileUrl,
            GroupId = uploadToJamDto.GroupId
        };

        if (!userRepository.Add(music) || !userRepository.SaveChanges())
        {
            error = "An error occured while adding the music";
            return false;
        }

        Console.WriteLine(music.Id);
        userRepository.Add(new GroupMusicJoin { GroupId = music.GroupId, MusicId = music.Id });

        if (!userRepository.TryGetById(music.Id, out music))
        {
            error = "An error occured while retrieving the music";
            return false;
        }

        return true;
    }

    private static bool TryDeleteInternal(IUserRepository userRepository, int id, out string error)
    {
        error = string.Empty;

        var result = userRepository.ExecuteStoreProcedure<int>($"{Constants.MainSchema}.spMusicDelete", new Tuple<string, object>("id", id));
        if (result.Length <= 0 || result[0] != 1)
        {
            error = "An error occured while deleting the music";
            return false;
        }

        return true;
    }

    #region Gets
    
    [HttpGet("GetGroupMusics")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult GetGroupMusics(int groupId)
    {
        if (!TokenHelper.CheckToken(User, userRepository, out _))
            return Unauthorized();

        var musics = new List<Music>();
        foreach (var musicId in GetGroupMusicsIdsInternal(userRepository, groupId))
        {
            if (!userRepository.TryGetById<Music>(musicId, out var music))
                continue;

            musics.Add(music);
        }

        return Ok(musics);
    }
    
    [HttpGet("GetAll")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult GetAll()
    {
        if (!TokenHelper.CheckToken(User, userRepository, out _))
            return Unauthorized();

        return Ok(userRepository.GetAll<Music>(null));
    }
    
    [HttpGet("GetComments")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult GetComments(int id)
    {
        if (!TokenHelper.CheckToken(User, userRepository, out _))
            return Unauthorized();
        
        if(!userRepository.TryGetById<Music>(id, out _))
            return NotFound("Music not found");
        
        return Ok(userRepository.GetAll<MusicComment>(x => x.MusicId == id));
    }
    
    #endregion
    
    
    #region Puts
    
    [HttpPut("AddComment")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult AddComment(CommentMusicDto commentMusicDto)
    {
        if (!TokenHelper.CheckToken(User, userRepository, out var tokenUserId))
            return Unauthorized();
        
        if(!userRepository.TryGetById<Music>(commentMusicDto.MusicId, out var music))
            return BadRequest("Music not found");
        
        var members = GroupController.GetMembersInternal(userRepository, music.GroupId);
        
        if(members.Contains(tokenUserId))
            return BadRequest("You cannot comment your own music");

        if(userRepository.Get<MusicComment>(x => x.MusicId == commentMusicDto.MusicId && x.UserId == tokenUserId) is not null)
            return BadRequest("You already commented this music");
        
        if(!userRepository.Add(new MusicComment
           {
               MusicId = commentMusicDto.MusicId, 
               UserId = tokenUserId, 
               Comment = commentMusicDto.Comment
           }) || !userRepository.SaveChanges())
            return BadRequest("An error occured while adding the comment");
        
        return Ok("Comment added");
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
        
        if(!userRepository.TryGetById<MusicComment>(commentId, out var comment))
            return BadRequest("Comment not found");
        
        if(user.Role != Models.User.RoleMap.Admin && comment.UserId != tokenUserId)
            return BadRequest("You are not the owner of this comment");
        
        if(!userRepository.Remove<MusicComment>(commentId) || !userRepository.SaveChanges())
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
        if (!userRepository.TryGetById<Music>(id, out var music))
            return NotFound("Music not found");
        
        if (!userRepository.TryGetById<Group>(music.GroupId, out var group))
            throw new Exception("Group not found");
        
        if (user.Role != Models.User.RoleMap.Admin)
        { 
             if(group.UserId != userId)
                return BadRequest("You are not the owner of this music");
        }

        if (!TryDeleteInternal(userRepository, id, out var deleteMusicError))
            return BadRequest(deleteMusicError);

        return Ok("Music deleted");
    }
    
    #endregion
}
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
    #region Gets
    
    [HttpGet("GetGroupMusics")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult GetGroupMusics(int groupId)
    {
        if (!TokenHelper.CheckToken(User, userRepository, out _))
            return Unauthorized();

        var musics = new List<Music>();
        foreach (var musicId in GroupController.GetMusicsInternal(userRepository, groupId))
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
        
        if(string.IsNullOrEmpty(commentMusicDto.Comment))
            return BadRequest("Comment is empty");
        
        var result = userRepository.ExecuteStoreProcedure<int>($"{Constants.MainSchema}.spMusicAddComment",
            new Tuple<string, object>("musicId", commentMusicDto.MusicId),
            new Tuple<string, object>("comment", commentMusicDto.Comment),
            new Tuple<string, object>("userId", tokenUserId));

        var resultCode = result.Length > 0 ? result[0] : -1;
        switch (resultCode)
        {
            case 1:
                return BadRequest("One of the parameters is null");
            case 2:
                return BadRequest("You do not have permission or the music doesn't exist");
            case 3:
                return BadRequest("You can't send a comment to your own music");
            case 4:
                return BadRequest("You already sent a comment to this music");
            case 0:
                return Ok("Comment sent");
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
        
        var result = userRepository.ExecuteStoreProcedure<int>($"{Constants.MainSchema}.spMusicDeleteComment",
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

        var result = userRepository.ExecuteStoreProcedure<int>($"{Constants.MainSchema}.spMusicDelete",
            new Tuple<string, object>("musicId", id),
            new Tuple<string, object>("userId", user.Id));
        var resultCode = result.Length > 0 ? result[0] : -1;

        switch (resultCode)
        {
            case 1:
                return BadRequest("One of the parameters is null");
            case 2:
                return BadRequest("The music doesn't exist or you do not have permission");
            case 0:
                return Ok("Music deleted");
            default:
                return BadRequest("An unexpected error occured");
        }
    }
    
    #endregion
}
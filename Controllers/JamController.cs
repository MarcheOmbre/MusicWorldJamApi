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
public class JamController(IUserRepository userRepository) : ControllerBase
{
    private const int MinNotation = 0;
    private const int MaxNotation = 10;
    
    #region Gets
    
    [HttpGet("Get")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult Get(int id)
    {
        if(!TokenHelper.CheckToken(User, userRepository, out _))
            return Unauthorized();
        
        if(!userRepository.TryGetById<Jam>(id, out var jam))
            return NotFound("No music found");
        
        return Ok(jam);
    }
    
    [HttpGet("GetAll")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult GetAll()
    {
        if(!TokenHelper.CheckToken(User, userRepository, out _))
            return Unauthorized();
        
        return Ok(userRepository.GetAll<Jam>(null));
    }
    
    [HttpGet("GetGroups")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult GetGroups(int id)
    {
        if (!TokenHelper.CheckToken(User, userRepository, out _))
            return Unauthorized();

        if (!userRepository.TryGetById<Jam>(id, out _))
            return NotFound("No jam found");

        var groupIds = userRepository.GetAll<JamGroupJoin>(x => x.JamId == id).Select(x => x.GroupId);
        return Ok(userRepository.GetAll<Group>(x => groupIds.Contains(x.Id)));
    }
    
    [HttpGet("GetMusics")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult GetMusics(int jamId)
    {
        if (!TokenHelper.CheckToken(User, userRepository, out _))
            return Unauthorized();

        if (!userRepository.TryGetById<Jam>(jamId, out _))
            return NotFound("No jam found");

        var musicIds = userRepository.GetAll<JamMusicJoin>(x => x.JamId == jamId).Select(x => x.MusicId);
        return Ok(userRepository.GetAll<Music>(x => musicIds.Contains(x.Id)));
    }
    
    #endregion
    
    
    #region Puts
    
    [HttpPut("Create")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult Create(CreateJamDto createJamDto)
    {
        if (!TokenHelper.CheckToken(User, userRepository, out var tokenUserId) ||
            !userRepository.TryGetById<User>(tokenUserId, out var user))
            return Unauthorized();
        
        if(user.Role != Models.User.RoleMap.Admin)
            return Unauthorized();

        if(string.IsNullOrWhiteSpace(createJamDto.Title))
            return BadRequest("Title is empty");
        if (string.IsNullOrWhiteSpace(createJamDto.Thema))
            return BadRequest("Thema is empty");
        if (createJamDto.UploadEndTime < DateTime.Now)
            return BadRequest("Upload end time is before created time");
        if(createJamDto.VoteEndTime < createJamDto.UploadEndTime)
            return BadRequest("Vote end time is before upload end time");
        
        var result = userRepository.ExecuteStoreProcedure<int>($"{Constants.MainSchema}.spJamCreate",
            new Tuple<string, object>("title", createJamDto.Title),
            new Tuple<string, object>("thema", createJamDto.Thema),
            new Tuple<string, object>("uploadEndTime", createJamDto.UploadEndTime),
            new  Tuple<string, object>("voteEndTime", createJamDto.VoteEndTime),
            new Tuple<string, object>("userId", tokenUserId));

        var resultCode = result.Length > 0 ? result[0] : -1;
        switch (resultCode)
        {
            case 1:
                return BadRequest("One of the parameters is null");
            case 2:
                return BadRequest("Upload End Time or Vote End time is incorrect : Created Time < Upload End Time < Vote End Time");
            case 3:
                return BadRequest("You don't have the permission");
            case 4:
                return BadRequest("The Jam title or thema already exists");
            case 0:
                return Ok("Jam created");
            default:
                return BadRequest("An unexpected error occured");
        }
    }
    
    [HttpPut("Subscribe")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult Subscribe(int id, int groupId)
    {
        if(!TokenHelper.CheckToken(User, userRepository, out var tokenUserId))
            return Unauthorized();
        
        var result = userRepository.ExecuteStoreProcedure<int>($"{Constants.MainSchema}.spJamSubscribe",
            new Tuple<string, object>("jamId", id),
            new Tuple<string, object>("groupId", groupId),
            new Tuple<string, object>("uploadEndTime", tokenUserId));

        var resultCode = result.Length > 0 ? result[0] : -1;
        switch (resultCode)
        {
            case 1:
                return BadRequest("One of the parameters is null");
            case 2:
                return BadRequest("The jam doesn't exist or the Upload session is already finished");
            case 3:
                return BadRequest("The group doesn't exist or you don't have the permission");
            case 4:
                return BadRequest("The group is already subscribed to this jam");
            case 0:
                return Ok("Subscribed");
            default:
                return BadRequest("An unexpected error occured");
        }
    }
    
    [HttpPut("Upload")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult Upload(UploadToJamDto uploadToJamDto)
    {
        if(!TokenHelper.CheckToken(User, userRepository, out var tokenUserId))
            return Unauthorized();
        
        var result = userRepository.ExecuteStoreProcedure<int>($"{Constants.MainSchema}.spJamUpload",
            new Tuple<string, object>("jamId", uploadToJamDto.JamId),
            new Tuple<string, object>("title", uploadToJamDto.Title),
            new Tuple<string, object>("description", uploadToJamDto.Description),
            new Tuple<string, object>("lyrics", uploadToJamDto.Lyrics),
            new Tuple<string, object>("fileUrl", uploadToJamDto.FileUrl),
            new Tuple<string, object>("groupId", uploadToJamDto.GroupId),
            new Tuple<string, object>("userId", tokenUserId));

        var resultCode = result.Length > 0 ? result[0] : -1;
        switch (resultCode)
        {
            case 1:
                return BadRequest("One of the parameters is null");
            case 2:
                return BadRequest("The group doesn't exist or you don't have the permission");
            case 3:
                return BadRequest("The jam doesn't exist or the Upload session is already finished");
            case 4:
                return BadRequest("The group is not subscribed to this jam");
            case 5:
                return BadRequest("The group already uploaded a music for the jam, you can delete the music to upload a new one");
            case 6:
                return BadRequest("A music with the same file Url already exists");
            case 7:
                return BadRequest("A music with the same title already exists for this group");
            case 0:
                return Ok("Uploaded");
            default:
                return BadRequest("An unexpected error occured");
        }
    }

    [HttpPut("Note")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult Note(NotationDto notationDto)
    {
        if(!TokenHelper.CheckToken(User, userRepository, out var tokenUserId))
            return Unauthorized();
        
        if(notationDto.Note < MinNotation)
            notationDto.Note = MinNotation;
        if(notationDto.Note > MaxNotation)
            notationDto.Note = MaxNotation;

        var result = userRepository.ExecuteStoreProcedure<int>($"{Constants.MainSchema}.spJamNote",
            new Tuple<string, object>("jamId", notationDto.JamId),
            new Tuple<string, object>("musicId", notationDto.MusicId),
            new Tuple<string, object>("note", notationDto.Note),
            new Tuple<string, object>("userId", tokenUserId));

        var resultCode = result.Length > 0 ? result[0] : -1;
        switch (resultCode)
        {
            case 1:
                return BadRequest("One of the parameters is null");
            case 2:
                return BadRequest("The note should be bounded between 0 and 10");
            case 3:
                return BadRequest("The jam doesn't exist or the Vote session is already finished");
            case 4:
                return BadRequest("The music is not uploaded for this jam");
            case 5:
                return BadRequest("You already noted this music");
            case 6:
                return BadRequest("You can't vote for your own music");
            case 0:
                return Ok("Uploaded");
            default:
                return BadRequest("An unexpected error occured");
        }
    }

    #endregion


    #region Delete

    [HttpDelete("Delete")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult Delete(int id)
    {
        if (!TokenHelper.CheckToken(User, userRepository, out var tokenUserId) || 
            !userRepository.TryGetById<User>(tokenUserId, out var user)
            || user.Role != Models.User.RoleMap.Admin)
            return Unauthorized();
        
        var result = userRepository.ExecuteStoreProcedure<int>($"{Constants.MainSchema}.spJamDelete",
            new Tuple<string, object>("jamId", id),
            new Tuple<string, object>("userId", tokenUserId));
        var resultCode = result.Length > 0 ? result[0] : -1;

        switch (resultCode)
        {
            case 1:
                return BadRequest("One of the parameters is null");
            case 2:
                return BadRequest("The jam doesn't exist or you don't have the permission to delete it");
            case 0:
                return Ok("Jam deleted");
            default:
                return BadRequest("An unexpected error occured");
        }
    }

    #endregion
}
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
        
        var jam = new Jam
        {
            Title = createJamDto.Title,
            Thema = createJamDto.Thema,
            CreatedTime = DateTime.Now,
            UploadEndTime = createJamDto.UploadEndTime,
            VoteEndTime = createJamDto.VoteEndTime,
        };

        if (userRepository.Add(jam) && userRepository.SaveChanges())
            return Ok("Jam added");
        
        return NotFound("An error occured while adding the music");
    }
    
    [HttpPut("Subscribe")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult Subscribe(int id, int groupId)
    {
        if(!TokenHelper.CheckToken(User, userRepository, out var tokenUserId))
            return Unauthorized();
        
        if(!userRepository.TryGetById<Jam>(id, out var jam))
            return BadRequest("Jam not found");
        
        if(DateTime.Now > jam.UploadEndTime)
            return BadRequest("Jam is over");
        
        if(!userRepository.TryGetById<Group>(groupId, out var group))
            return BadRequest("Group not found");

        if(group.UserId != tokenUserId)
            return BadRequest("You are not the owner of this group");
        
        if (userRepository.Get<JamGroupJoin>(x => x.JamId == id && x.GroupId == groupId) != null)
            return BadRequest("The group is already subscribed to this jam");
        
        if(!userRepository.Add(new JamGroupJoin { JamId = id, GroupId = groupId }) || 
           !userRepository.SaveChanges())
            return BadRequest("An error occured while subscribing the group to the jam");
        
        return Ok("Subscribed");
    }
    
    [HttpPut("Upload")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult Upload(UploadToJamDto uploadToJamDto)
    {
        if(!TokenHelper.CheckToken(User, userRepository, out var tokenUserId))
            return Unauthorized();
        
        if(!userRepository.TryGetById<Jam>(uploadToJamDto.JamId, out var jam))
            return BadRequest("Jam not found");
        
        if(DateTime.Now > jam.UploadEndTime)
            return BadRequest("Jam is over");

        if (userRepository.Get<JamGroupJoin>(jamGroupJoin => jamGroupJoin.GroupId != uploadToJamDto.GroupId) != null)
            return BadRequest("The group is not subscribed to this jam");
        
        if(!MusicController.TryAddInternal(userRepository, tokenUserId, uploadToJamDto, out var error, out var music))
            return BadRequest(error);
        
        if (!userRepository.Add(new JamMusicJoin { JamId = uploadToJamDto.JamId, MusicId = music.Id }) ||
            !userRepository.SaveChanges())
            return BadRequest("An error occured while adding the music to join tables");
        
        return Ok("Uploaded");
    }

    [HttpPut("Note")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult Note(NotationDto notationDto)
    {
        if(!TokenHelper.CheckToken(User, userRepository, out var tokenUserId))
            return Unauthorized();
        
        if(!userRepository.TryGetById<Jam>(notationDto.JamId, out var jam))
            return BadRequest("Jam not found");
        
        if(DateTime.Now < jam.UploadEndTime)
            return BadRequest("Jam vote is not open yet");
        
        if(DateTime.Now > jam.VoteEndTime)
            return BadRequest("Jam is over");
        
        var jamMusicJoin = userRepository.Get<JamMusicJoin>(x => x.JamId == notationDto.JamId && x.MusicId == notationDto.MusicId);
        if(jamMusicJoin is null)
            return BadRequest("Music is not uploaded to this jam");
        
        if(!userRepository.TryGetById<Music>(notationDto.MusicId, out var music))
            return BadRequest("Music not found");
        
        var members = GroupController.GetMembersInternal(userRepository, music.GroupId);
        
        if(members.Contains(tokenUserId))
            return BadRequest("You cannot vote for your own music");
        
        var userNotation = userRepository.Get<Notation>(x => x.JamId == notationDto.JamId && x.MusicId == notationDto.MusicId && x.UserId == tokenUserId);
        if(userNotation != null)
            return BadRequest("You already voted for this music");
        
        if(notationDto.Note < MinNotation)
            notationDto.Note = MinNotation;
        if(notationDto.Note > MaxNotation)
            notationDto.Note = MaxNotation;
        
        if(!userRepository.Add(new Notation
        {
            JamId = notationDto.JamId,
            MusicId = notationDto.MusicId,
            UserId = tokenUserId,
            Note = notationDto.Note
        }) || !userRepository.SaveChanges())
            return BadRequest("An error occured while adding the notation");
        
        return Ok("Notation added");
    }

    #endregion


    #region Delete

    [HttpDelete("Delete")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult Delete(int id)
    {
        if (!TokenHelper.CheckToken(User, userRepository, out var tokenUserId) || 
            !userRepository.TryGetById<User>(tokenUserId, out var user))
            return Unauthorized();
        
        if(user.Role != Models.User.RoleMap.Admin)
            return Unauthorized();
        
        var joinGroupsToDelete = userRepository.GetAll<JamGroupJoin>(x => x.JamId == id);
        foreach (var jamMusicJoin in joinGroupsToDelete) 
            userRepository.Remove<JamGroupJoin>(jamMusicJoin.Id);
        
        var joinMusicsToDelete = userRepository.GetAll<JamMusicJoin>(x => x.JamId == id);
        foreach (var jamMusicJoin in joinMusicsToDelete) 
            userRepository.Remove<JamMusicJoin>(jamMusicJoin.Id);
        
        var notationsToDelete = userRepository.GetAll<Notation>(x => x.JamId == id);
        foreach (var notation in notationsToDelete)
            userRepository.Remove<Notation>(notation.Id);
        
        if((joinMusicsToDelete.Count > 0 || 
            joinGroupsToDelete.Count > 0 ||
            notationsToDelete.Count > 0)
           && !userRepository.SaveChanges())
            return BadRequest("An error occured when deleting the jam id from jamGroupJoin, jamMusicJoin and notation tables");
        
        if(userRepository.Remove<Jam>(id) && userRepository.SaveChanges())
            return Ok("Jam deleted");
        
        return NotFound("Jam not found");
    }

    #endregion
}
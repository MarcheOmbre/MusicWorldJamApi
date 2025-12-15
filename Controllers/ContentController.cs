using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WorldMusicJam.Helpers;
using WorldMusicJam.Models;
using WorldMusicJam.Repositories;

namespace WorldMusicJam.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class ContentController(IUserRepository userRepository) : ControllerBase
{
    private const int TitleScore = 3;
    private const int DescriptionScore = 1;
    private const int LyricsScore = 2;
    
    #region Gets
    
    [HttpGet("SearchForGroups")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult SearchForGroups(string content)
    {
        if (!TokenHelper.CheckToken(User, userRepository, out _))
            return Unauthorized();


        var researchTuples = new Dictionary<Group, int>();
        var musics = userRepository.GetAll<Group>(x => x.Name.Contains(content) || x.Description.Contains(content));
        
        foreach (var music in musics)
        {
            var score = 0;
            
            if (music.Name.Contains(content))
                score += TitleScore;

            if (music.Description.Contains(content))
                score += DescriptionScore;

            if (score <= 0)
                continue;
            
            researchTuples.Add(music, score);
        }

        return Ok(researchTuples.OrderByDescending( x=> x.Value).Select(x => x.Key));
    }

    [HttpGet("SearchForMusics")]
    [ProducesResponseType(statusCode: StatusCodes.Status200OK, Type = typeof(string))]
    [ProducesResponseType(statusCode: StatusCodes.Status404NotFound, Type = typeof(string))]
    public IActionResult SearchForMusics(string content)
    {
        if (!TokenHelper.CheckToken(User, userRepository, out _))
            return Unauthorized();


        var researchTuples = new Dictionary<Music, int>();
        var musics = userRepository.GetAll<Music>(x => x.Title.Contains(content) || 
                                                       x.Description.Contains(content) || x.Lyrics.Contains(content));
        
        foreach (var music in musics)
        {
            var score = 0;
            
            if (music.Title.Contains(content))
                score += TitleScore;

            if (music.Description.Contains(content))
                score += DescriptionScore;

            if (music.Lyrics.Contains(content))
                score += LyricsScore;

            if (score <= 0)
                continue;
            
            researchTuples.Add(music, score);
        }

        return Ok(researchTuples.OrderByDescending( x=> x.Value).Select(x => x.Key));
    }

    #endregion
}
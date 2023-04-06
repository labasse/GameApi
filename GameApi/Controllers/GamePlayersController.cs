using GameApi.DTOs;
using GameApi.Models;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;

namespace GameApi.Controllers
{
    [ApiController]
    [Route("games/{gameId:guid}/players")]
    public class GamePlayersController : ControllerBase
    {
        private GameAndPlayerManager _manager;

        public GamePlayersController(GameAndPlayerManager manager)
            => _manager = manager;
        
        private bool FindGame(Guid gameId, [NotNullWhen(true)] out Game? foundGame)
            => (foundGame = _manager.GameById(gameId)) is not null; 
       
        /// <summary>
        /// Gets the list of all players actually in the game.
        /// </summary>
        /// <param name="gameId">Game Id</param>
        /// <returns>A list of the players in the game</returns>
        /// <response code="200">Player list successfully found</response>
        /// <response code="404">No game with this id</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<PlayerDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<IEnumerable<PlayerDto>> AllGamePlayers(Guid gameId)
            => FindGame(gameId, out var game) 
                ? Ok(game.Players.Select(p => PlayerDto.FromPlayer(p)))
                : NotFound("No game with this id");

        /// <summary>
        /// Join a player to the game.
        /// </summary>
        /// <param name="gameId">Game to join</param>
        /// <param name="player">Player who joins the game : Public and private Id must be correctly set.</param>
        /// <returns></returns>
        /// <response code="201">Player successfully joined the game</response>
        /// <response code="401">The given private Id does not match the player's one</response>
        /// <response code="404">No game or player with this public id</response>
        /// <response code="409">The player already joined the game</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public ActionResult<Player> JoinGame(Guid gameId, [FromBody] Player player)
        {
            var p = _manager.PlayerByPublicId(player.PublicId);
            
            if(p == null)
            {
                return NotFound("No player with this public Id");
            }
            if(p.PrivateId != player.PrivateId)
            {
                return Unauthorized("Given private Id does not match the player's one");
            }
            if(_manager.GetGameByPlayer(p) is not null)
            {
                return Conflict("Already in a game");
            }
            if(!FindGame(gameId, out var game))
            {
                return NotFound("No game with this id");
            }
            try
            {
                game.Add(p);
                return Created($"/games/{game.Id}/players/{player.PublicId}", p);
            }
            catch(ArgumentException)
            {
                return BadRequest("Player already in the game");
            }
        }
        /// <summary>
        /// Gets a player out of a game.
        /// </summary>
        /// <param name="gameId">Game to leave</param>
        /// <param name="publicId">Public Id of the player to get out</param>
        /// <param name="privateId">, the private key can be the leaving player's one or the creator's one</param>
        /// <returns>No content</returns>
        /// <response code="204">player successfully removed from the game</response>
        /// <response code="400">Private id parameter expected</response>
        /// <response code="401">Given private key does not allow to get the player out of the game</response>
        /// <response code="404">No player or game with this public id</response>
        [HttpDelete("{publicId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult LeaveGameOrKick(Guid gameId, Guid publicId, Guid privateId)
        {
            if(privateId == Guid.Empty)
            {
                return BadRequest("Private Id parameter expected");
            }
            var p = _manager.PlayerByPublicId(publicId);

            if (p == null)
            {
                return NotFound("No player with this public Id");
            }
            if (!FindGame(gameId, out var game))
            {
                return NotFound("No game with this Id");
            }
            try
            {
                game.Remove(p, privateId);
                return NoContent();
            }
            catch (ArgumentException)
            {
                return Unauthorized("Given private Id does not allow to kick the player");
            }
        }
    }
}

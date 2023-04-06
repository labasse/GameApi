using GameApi.DTOs;
using GameApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace GameApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GamesController : ControllerBase
    {
        private GameAndPlayerManager _manager;

        public GamesController(GameAndPlayerManager manager)
            => _manager = manager;

        /// <summary>
        /// Gets all active games on the server.
        /// </summary>
        /// <returns>All games</returns>
        /// <response code="200">Game list successfully returned.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<GameDto>), StatusCodes.Status200OK)]
        public IEnumerable<GameDto> GetAllGames() => _manager.AllGames.Select(g => GameDto.FromGame(g));

        /// <summary>
        /// Gets a game information.
        /// </summary>
        /// <param name="gameId">Game id</param>
        /// <returns>Game details</returns>
        /// <response code="200">Game found</response>
        /// <response code="404">No game with this id</response>
        [HttpGet("{gameId:guid}")]
        [ProducesResponseType(typeof(GameDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<GameDto> GetGame(Guid gameId)
        {
            var game = _manager.GameById(gameId);

            return game is null ? NotFound() : Ok(GameDto.FromGame(game));
        }

        /// <summary>
        /// Creates a game.
        /// </summary>
        /// <param name="game">Data of the game to create. Only title and creator id must be set.</param>
        /// <returns>Created game</returns>
        /// <response code="201">Game successfully created</response>
        /// <response code="400">Title or creator id missing</response>
        /// <response code="404">Creator not found in player list</response>
        [HttpPost]
        [ProducesResponseType(typeof(GameDto), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(GameDto), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(GameDto), StatusCodes.Status404NotFound)]
        public ActionResult<GameDto?> CreateGame([FromBody] GameDto game)
        {
            if(game.Creator is null || game.Title is null)
            {
                return BadRequest("Title or creator id missing");
            }
            try
            {
                var g = _manager.NewGame(game.Title, game.Creator.Value);

                return Created($"/games/{g.Id}", GameDto.FromGame(g));
            }
            catch(ArgumentException)
            {
                return NotFound("No player found with this private id");
            }
            catch(InvalidOperationException)
            {
                return Conflict("Creator already in a game");
            }
        }

        /// <summary>
        /// Deletes a game.
        /// </summary>
        /// <param name="gameId">Data of the game to create. Only title and creator id must be set.</param>
        /// <param name="creatorPrivateId">Private Id of the game creator</param>
        /// <returns>No content</returns>
        /// <response code="204">Game successfully deleted</response>
        /// <response code="400">Creator Private Id not set</response>
        /// <response code="401">The given id is not the creator's private id</response>
        /// <response code="404">Game not found</response>
        [HttpDelete("{gameId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<GameDto?> DeleteGame(Guid gameId, Guid creatorPrivateId)
        {
            if(creatorPrivateId == Guid.Empty)
            {
                return BadRequest("Missing the creator private Id parameter");
            }
            try
            {
                _manager.DeleteGame(gameId, creatorPrivateId);
                return NoContent();
            }
            catch(ArgumentException ex)
            {
                return ex.ParamName switch
                {
                    "gameId" => NotFound("No game with this id"),
                    "privateId" => Unauthorized("The given id is not the creator's private one"),
                    _ => BadRequest($"Unexpected error : {ex.Message}")
                };
            }
        }
    }

}
using GameApi.DTOs;
using GameApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Numerics;

namespace GameApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PlayersController : ControllerBase
    {
        private GameAndPlayerManager _manager;

        public PlayersController(GameAndPlayerManager manager) 
            => _manager = manager;
        
        /// <summary>
        /// Gets all active players on the server.
        /// </summary>
        /// <returns>All players</returns>
        /// <response code="200">Player list successfully returned.</response>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<PlayerDto>), StatusCodes.Status200OK)]
        public IEnumerable<PlayerDto> GetAllPlayers()
            => _manager.AllPlayers.Select(p => PlayerDto.FromPlayer(p));

        /// <summary>
        /// Gets a player information.
        /// </summary>
        /// <param name="publicId">Player public id</param>
        /// <returns>Player details</returns>
        /// <response code="200">Player found</response>
        /// <response code="404">No player with this public id</response>
        [HttpGet("{publicId:guid}")]
        [ProducesResponseType(typeof(PlayerDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<PlayerDto?> GetPlayer(Guid publicId)
        {
            var player = _manager.PlayerByPublicId(publicId);

            return player is null ? NotFound() : Ok(PlayerDto.FromPlayer(player));
        }

        /// <summary>
        /// Creates a player.
        /// </summary>
        /// <param name="player">Data of the player to create. Only pseudo must be set.</param>
        /// <returns>Created player</returns>
        /// <response code="201">Player successfully created</response>
        /// <response code="400">Pseudo is missing</response>
        [HttpPost]
        [ProducesResponseType(typeof(Player), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<Player?> CreatePlayer([FromBody] PlayerDto player)
        {
            if (player.Pseudo is null)
            {
                return BadRequest("Pseudo is missing");
            }
            var p = _manager.NewPlayer(player.Pseudo);
        
            return Created($"/players/{p.PublicId}", p);
        }

        /// <summary>
        /// Delete the player.
        /// </summary>
        /// <param name="publicId">Public id of the player to delete</param>
        /// <param name="privateId">Private id of the player to delete</param>
        /// <returns>No content</returns>
        /// <response code="204">Player successfully deleted</response>
        /// <response code="400">The given id is not the creator's private id</response>
        /// <response code="404">Game not found</response>
        [HttpDelete("{publicId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult DeletePlayer(Guid publicId, Guid privateId)
        {
            try
            {
                _manager.DeletePlayer(publicId, privateId);
                return NoContent();
            }
            catch(ArgumentException ex)
            {
                return ex.ParamName switch
                {
                    "publicId" => NotFound("No player with this public id"),
                    "privateId" => BadRequest("The private id is not the player's private one"),
                    _ => BadRequest($"Unexpected error : {ex.Message}")
                };
            }
        }
    }
}

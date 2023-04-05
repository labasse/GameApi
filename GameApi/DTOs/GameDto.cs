using GameApi.Models;

namespace GameApi.DTOs
{
    public record GameDto(Guid? Id = null, string? Title = null, Guid? Creator = null)
    {
        public static GameDto FromGame(Game game)
            => new GameDto(game.Id, game.Title, game.Creator.PublicId);
        
    }
}

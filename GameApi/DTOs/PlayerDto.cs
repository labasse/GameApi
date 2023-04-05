using GameApi.Models;

namespace GameApi.DTOs
{
    public record PlayerDto(Guid? PublicId, string? Pseudo)
    {
        public static PlayerDto FromPlayer(Player player)
            => new PlayerDto(player.PublicId, player.Pseudo);
    }
}

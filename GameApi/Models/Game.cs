using GameApi.Utils;

namespace GameApi.Models
{
    public class Game
    {
        private readonly List<Player> _players;
        public Game(string title, Player creator, IGuidGenerator guidgen)
        {
            Id = guidgen.NewGuid();
            Title = title;
            _players = new List<Player>{ creator };
        }
        public Guid Id { get; private set; }
        public string Title { get; init; }
        public IEnumerable<Player> Players => _players;
        public bool IsEmpty => _players.Count == 0;
        public Player Creator => _players[0];
        public void Add(Player player)
        {
            if(_players.Contains(player))
            {
                throw new ArgumentException("Player already joined");
            }
            lock(this)
            {
                _players.Add(player);
            }
        }
        public bool Remove(Player player, Guid privateId)
        {
            lock(this)
            {
                if(privateId != Creator.PrivateId && privateId != player.PrivateId)
                {
                    throw new ArgumentException(
                        "Player kick denied (only creator or player himself)", 
                        nameof(privateId)
                    );
                }
                var index = _players.IndexOf(player);
                var found = index >= 0;

                if (found)
                {
                    _players.RemoveAt(index);
                }
                return found;
            }
        }
    }
}

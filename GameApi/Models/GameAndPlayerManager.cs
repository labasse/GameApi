using GameApi.Utils;

namespace GameApi.Models
{
    public class GameAndPlayerManager
    {
        public static readonly TimeSpan PlayerTimeout = TimeSpan.FromMinutes(20);
        
        private readonly Dictionary<Guid, Player> _players = new Dictionary<Guid, Player>();
        private readonly Dictionary<Guid, Game  > _games   = new Dictionary<Guid, Game>();
        private readonly IClock _clock;
        private readonly IGuidGenerator _guidgen;

        public GameAndPlayerManager(IClock clock, IGuidGenerator guidgen)
        {
            _clock = clock;
            _guidgen = guidgen;
        }

        public IEnumerable<Player> AllPlayers
        {
            get
            {
                lock (this)
                {
                    var dead = _players
                        .Where(p => p.Value.AliveExceeds(PlayerTimeout, _clock))
                        .Select(p => p.Value)
                        .ToArray();

                    foreach (var p in dead)
                    {
                        foreach (var g in _games.Values)
                        {
                            g.Remove(p, g.Creator.PrivateId);
                        }
                        _players.Remove(p.PublicId);
                    }
                    return _players.Values;
                }
            }
        }
        public IEnumerable<Game> AllGames
        {
            get
            {
                lock(this)
                {
                    var empty = _games
                        .Where(g => g.Value.IsEmpty)
                        .Select(g => g.Key)
                        .ToArray();

                    foreach (var k in empty)
                        _games.Remove(k);
                    return _games.Values;
                }
            }
        }
        public Game NewGame(string title, Guid creatorPrivateId)
        {
            lock (this)
            {
                var creator = _players.Values.FirstOrDefault(p => p.PrivateId==creatorPrivateId);

                if (creator is null)
                {
                    throw new ArgumentException(
                        "No player with the creator id",
                        nameof(creatorPrivateId)
                    );
                }
                var game = new Game(title, creator, _guidgen);

                creator.MarkAsAlive(_clock);
                _games.Add(game.Id, game);
                return game;
            }
        }

        public Player NewPlayer(string pseudo)
        {
            lock (this)
            {
                var player = new Player(pseudo, _clock, _guidgen);

                _players.Add(player.PublicId, player);
                return player;
            }
        }

        public Player? PlayerByPublicId(Guid id)
        {
            if (!_players.ContainsKey(id))
            {
                return null;
            }
            var p = _players[id];

            p.MarkAsAlive(_clock);
            return p;
        }

        public Game? GameById(Guid id)
        {
            if(!_games.ContainsKey(id))
            {
                return null;
            }
            lock(this)
            {
                var g = _games[id];

                if(g.IsEmpty)
                {
                    _games.Remove(id);
                    return null;
                }
                return g;
            }
        }

        public void DeleteGame(Guid gameId, Guid privateId)
        {
            lock(this)
            {
                if(!_games.ContainsKey(gameId))
                {
                    throw new ArgumentException(
                        "No game with this Id",
                        nameof(gameId)
                    );
                }
                var game = _games[gameId];

                if(game.Creator.PrivateId!=privateId) 
                {
                    throw new ArgumentException(
                        "Not the creator",
                        nameof(privateId)
                    );
                }
                game.Creator.MarkAsAlive(_clock);
                _games.Remove(gameId);
            }
        }
        public void DeletePlayer(Guid publicId, Guid privateId)
        {
            lock (this)
            {
                if (!_players.ContainsKey(publicId))
                {
                    throw new ArgumentException(
                        "No player with this pubic Id",
                        nameof(publicId)
                    );
                }
                var actualPrivateId = _players[publicId].PrivateId;

                if (actualPrivateId != privateId)
                {
                    throw new ArgumentException(
                        "Cannot delete this player with this private Id",
                        nameof(privateId)
                    );
                }
                _players.Remove(publicId);
            }
        }
    }
}

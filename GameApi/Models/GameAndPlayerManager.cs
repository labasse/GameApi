using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace GameApi.Models
{
    public class GameAndPlayerManager
    {
        public static readonly TimeSpan PlayerTimeout = TimeSpan.FromMinutes(20);
        private static readonly TimeSpan PlayerListRefreshPeriod = TimeSpan.FromMinutes(1);

        private readonly Dictionary<Guid, Player> _players = new Dictionary<Guid, Player>();
        private readonly Dictionary<Guid, Game  > _games   = new Dictionary<Guid, Game>();


        public IEnumerable<Player> AllPlayers
        {
            get
            {
                lock (this)
                {
                    var dead = _players
                        .Where(p => p.Value.AliveExceeds(PlayerTimeout))
                        .Select(p => p.Key)
                        .ToArray();

                    foreach (var k in dead)
                        _games.Remove(k);
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
                    throw new ArgumentException(nameof(creatorPrivateId), "No player with the creator id");
                }
                var game = new Game(title, creator);

                creator.MarkAsAlive();
                _games.Add(game.Id, game);
                return game;
            }
        }

        public Player NewPlayer(string pseudo)
        {
            lock (this)
            {
                var player = new Player(pseudo);

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

            p.MarkAsAlive();
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
                    throw new ArgumentException(nameof(gameId), "No game with this Id");
                }
                var game = _games[gameId];

                if(game.Creator.PrivateId!=privateId) 
                {
                    throw new ArgumentException(nameof(privateId), "Not the creator");
                }
                game.Creator.MarkAsAlive();
                _games.Remove(gameId);
            }
        }
        public void DeletePlayer(Guid publicId, Guid privateId)
        {
            lock (this)
            {
                if (!_players.ContainsKey(publicId))
                {
                    throw new ArgumentException(nameof(publicId), "No player with this pubic Id");
                }
                var actualPrivateId = _players[publicId].PrivateId;

                if (actualPrivateId != privateId)
                {
                    throw new ArgumentException(nameof(privateId), "Cannot delete this player with this private Id");
                }
                _players.Remove(publicId);
            }
        }
    }
}

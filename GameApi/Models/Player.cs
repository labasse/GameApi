using GameApi.Utils;

namespace GameApi.Models
{
    public class Player
    {
        private DateTime liveSignAt;

        #region For deserialization only
        public Player() : this(Guid.NewGuid(), Guid.NewGuid(), "", DateTime.MinValue)
        {  }
        #endregion

        public Guid PrivateId { get; init; }
        public Guid PublicId { get; init; }
        public string Pseudo { get; init; }

        private Player(Guid privateId, Guid publicId, string pseudo, DateTime now)
        {
            PrivateId = privateId; 
            PublicId = publicId;
            Pseudo = pseudo;
            liveSignAt = now;
        }
        public Player(string pseudo, IClock clock, IGuidGenerator guidgen) 
            : this(guidgen.NewGuid(), guidgen.NewGuid(), pseudo, clock.UtcNow)
        {
            
        }
        public bool AliveExceeds(TimeSpan duration, IClock clock) 
            => clock.UtcNow - liveSignAt > duration;
        public void MarkAsAlive(IClock clock)
        {
            liveSignAt = clock.UtcNow;
        }
        public override bool Equals(object? obj) => 
            obj is Player p && p.PublicId == PublicId;

        public override int GetHashCode() =>
            PublicId.GetHashCode();
    }
}

using GameApi.Utils;

namespace GameApi.Models
{
    public record Player(Guid PrivateId, Guid PublicId, string Pseudo)
    {
        private DateTime liveSignAt;

        #region For deserialization only
        public Player() : this(Guid.NewGuid(), Guid.NewGuid(), "")
        { 
            liveSignAt = DateTime.MinValue;
        }
        #endregion

        public Player(string pseudo, IClock clock, IGuidGenerator guidgen) : this(guidgen.NewGuid(), guidgen.NewGuid(), pseudo)
        {
            liveSignAt = clock.UtcNow;
        }
        public bool AliveExceeds(TimeSpan duration, IClock clock) 
            => clock.UtcNow - liveSignAt > duration;
        public void MarkAsAlive(IClock clock)
        {
            liveSignAt = clock.UtcNow;
        }
    }
}

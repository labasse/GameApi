namespace GameApi.Models
{
    public record Player(Guid PrivateId, Guid PublicId, string Pseudo)
    {
        private DateTime liveSignAt = DateTime.UtcNow;

        #region For deserialization only
        public Player() : this(Guid.NewGuid(), Guid.NewGuid(), "")
        { }
        #endregion

        public Player(string pseudo) : this(Guid.NewGuid(), Guid.NewGuid(), pseudo)
        {
            
        }
        public bool AliveExceeds(TimeSpan duration) => DateTime.UtcNow - liveSignAt > duration;
        public void MarkAsAlive()
        {
            liveSignAt = DateTime.UtcNow;
        }
    }
}

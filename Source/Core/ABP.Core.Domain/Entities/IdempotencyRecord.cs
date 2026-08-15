namespace ABP.Core.Domain.Entities
{
    /// <summary>
    /// Durable claim for a financial command. The unique index prevents concurrent retries
    /// with the same key from executing the command more than once.
    /// </summary>
    public class IdempotencyRecord
    {
        public int Id { get; set; }
        public string Operation { get; set; } = string.Empty;
        public string Key { get; set; } = string.Empty;
        public string ActorUserId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}

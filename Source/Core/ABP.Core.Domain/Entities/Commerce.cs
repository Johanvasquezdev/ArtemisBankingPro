namespace ABP.Core.Domain.Entities
{
    public class Commerce
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Rnc { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string Logo { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        // ICollections
        public ICollection<CreditCardConsumption> Consumptions { get; set; } = [];
    }
}

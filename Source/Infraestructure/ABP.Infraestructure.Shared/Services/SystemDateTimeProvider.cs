using ABP.Core.Domain.Interfaces;

namespace ABP.Infraestructure.Shared.Services
{
    public class SystemDateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}

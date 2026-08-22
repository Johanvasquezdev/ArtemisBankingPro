using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Interfaces;
using ABP.Infraestructure.Persistence.Context;
using ABP.Infraestructure.Persistence.Repositories.Generic;

namespace ABP.Infraestructure.Persistence.Repositories
{
    public class VirtualCardRepository(ArtemisBankingDbContext context) : GenericRepository<VirtualCard>(context), IVirtualCardRepository
    {
    }
}

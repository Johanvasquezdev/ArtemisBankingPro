using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.ViewModels.Client;
using MediatR;

namespace ABP.Core.Application.Features.Client.Queries
{
    public record GetBeneficiariesQuery(string ClientId) : IRequest<IReadOnlyList<BeneficiaryListItemViewModel>>;

    public class GetBeneficiariesQueryHandler(IBeneficiaryService beneficiaryService)
        : IRequestHandler<GetBeneficiariesQuery, IReadOnlyList<BeneficiaryListItemViewModel>>
    {
        public async Task<IReadOnlyList<BeneficiaryListItemViewModel>> Handle(
            GetBeneficiariesQuery request,
            CancellationToken cancellationToken)
        {
            var beneficiaries = await beneficiaryService.GetByOwnerIdAsync(request.ClientId);

            return beneficiaries
                .Select(b => new BeneficiaryListItemViewModel
                {
                    Id = b.Id,
                    AccountNumber = b.AccountNumber,
                    FullName = $"{b.FirstName} {b.LastName}".Trim()
                })
                .ToList();
        }
    }
}

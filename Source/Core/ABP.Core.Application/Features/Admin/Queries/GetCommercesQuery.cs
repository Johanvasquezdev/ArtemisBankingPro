using ABP.Core.Application.DTOs.Commerce;
using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Queries
{
    public sealed record GetCommercesQuery(int Page = 1, int PageSize = 20, string Status = "activo") : IRequest<GetCommercesResult>;

    public sealed record GetCommercesResult(int Page, int PageSize, int TotalRecords, IEnumerable<CommerceDto> Data);

    public sealed class GetCommercesQueryValidator : AbstractValidator<GetCommercesQuery>
    {
        public GetCommercesQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0).WithMessage("Parámetros de paginación inválidos.");
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(20).WithMessage("Parámetros de paginación inválidos.");
            RuleFor(x => x.Status).Must(s => s is "activo" or "inactivo" or "todos")
                .WithMessage("El estado debe ser activo, inactivo o todos.");
        }
    }

    public sealed class GetCommercesQueryHandler(ICommerceService commerceService) : IRequestHandler<GetCommercesQuery, GetCommercesResult>
    {
        public async Task<GetCommercesResult> Handle(GetCommercesQuery request, CancellationToken cancellationToken)
        {
            var result = await commerceService.GetAllPagedAsync(request.Page, request.PageSize);

            var filtered = request.Status switch
            {
                "activo" => result.Items.Where(c => c.IsActive),
                "inactivo" => result.Items.Where(c => !c.IsActive),
                _ => result.Items
            };

            var data = new List<CommerceDto>();
            foreach (var commerce in filtered)
            {
                commerce.HasAssociatedUser = await commerceService.HasActiveUserAsync(commerce.Id);
                data.Add(commerce);
            }

            return new GetCommercesResult(result.Page, result.PageSize, data.Count, data);
        }
    }
}

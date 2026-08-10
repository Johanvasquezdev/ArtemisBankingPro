using ABP.Core.Application.ViewModels.Beneficiary;

namespace ABP.Core.Application.ViewModels.Client
{
    public class BeneficiariesViewModel
    {
        public IReadOnlyList<BeneficiaryListItemViewModel> Beneficiaries { get; set; } = [];
        public SaveBeneficiaryViewModel Add { get; set; } = new();
    }
}

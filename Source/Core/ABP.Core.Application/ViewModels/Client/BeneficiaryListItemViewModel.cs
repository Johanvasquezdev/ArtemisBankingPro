namespace ABP.Core.Application.ViewModels.Client
{
    public class BeneficiaryListItemViewModel
    {
        public int Id { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }
}

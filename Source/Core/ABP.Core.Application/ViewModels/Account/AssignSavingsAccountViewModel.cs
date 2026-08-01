namespace ABP.Core.Application.ViewModels.Account
{
    public class AssignSavingsAccountViewModel
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public int InitialBalance { get; set; }
        public bool HasError { get; set; }
        public string Error { get; set; } = string.Empty;
    }
}

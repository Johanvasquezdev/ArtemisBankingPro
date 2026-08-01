using System.ComponentModel.DataAnnotations;

namespace ABP.API.DTOs.Payment
{
    public class ProcessPaymentRequest
    {
        [Required(ErrorMessage = "Card number is required.")]
        [StringLength(16, MinimumLength = 16, ErrorMessage = "Card number must be 16 digits.")]
        public string CardNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Expiration month is required.")]
        [RegularExpression(@"^(0[1-9]|1[0-2])$", ErrorMessage = "Month must be between 01 and 12.")]
        public string MonthExpirationCard { get; set; } = string.Empty;

        [Required(ErrorMessage = "Expiration year is required.")]
        [RegularExpression(@"^\d{4}$", ErrorMessage = "Year must be 4 digits.")]
        public string YearExpirationCard { get; set; } = string.Empty;

        [Required(ErrorMessage = "CVC is required.")]
        [StringLength(3, MinimumLength = 3, ErrorMessage = "CVC must be 3 digits.")]
        public string CVC { get; set; } = string.Empty;

        [Required(ErrorMessage = "Transaction amount is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Transaction amount must be greater than 0.")]
        public decimal TransactionAmount { get; set; }
    }
}

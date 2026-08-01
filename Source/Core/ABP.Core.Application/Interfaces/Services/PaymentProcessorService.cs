using ABP.Core.Application.DTOs.CreditCardConsumption;
using ABP.Core.Application.DTOs.Payment;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Enums;
using AutoMapper;

namespace ABP.Core.Application.Interfaces.Services
{
    public class PaymentProcessorService : IPaymentProcessorService
    {
        private readonly ICreditCardService _creditCardService;
        private readonly ICommerceService _commerceService;
        private readonly ICreditCardConsumptionService _consumptionService;
        private readonly IMapper _mapper;

        public PaymentProcessorService(
            ICreditCardService creditCardService,
            ICommerceService commerceService,
            ICreditCardConsumptionService consumptionService,
            IMapper mapper)
        {
            _creditCardService = creditCardService;
            _commerceService = commerceService;
            _consumptionService = consumptionService;
            _mapper = mapper;
        }

        public async Task<PaymentResultDto> ProcessPaymentAsync(int commerceId, ProcessPaymentDto paymentDto)
        {
            var commerce = await _commerceService.GetByIdAsync(commerceId);
            if (commerce == null || !commerce.IsActive)
            {
                return new PaymentResultDto
                {
                    Success = false,
                    Message = "Commerce not found or inactive."
                };
            }

            var card = await _creditCardService.GetByCardNumberAsync(paymentDto.CardNumber);
            if (card == null)
            {
                return new PaymentResultDto
                {
                    Success = false,
                    Message = "Invalid card number."
                };
            }

            if (card.Status != CardStatus.Active)
            {
                return new PaymentResultDto
                {
                    Success = false,
                    Message = "Card is not active."
                };
            }

            var availableCredit = card.CreditLimit - card.AmountOwed;
            if (paymentDto.TransactionAmount > availableCredit)
            {
                return new PaymentResultDto
                {
                    Success = false,
                    Message = "Insufficient credit limit."
                };
            }

            var consumption = new CreditCardConsumptionDto
            {
                Amount = paymentDto.TransactionAmount,
                TransactionDate = DateTime.UtcNow,
                CommerceName = commerce.Name,
                Status = ConsumptionStatus.Approved,
                CreditCardId = card.Id,
                CommerceId = commerceId
            };

            await _consumptionService.AddAsync(consumption);

            var newBalance = card.CreditLimit - (card.AmountOwed + paymentDto.TransactionAmount);

            return new PaymentResultDto
            {
                Success = true,
                Message = "Payment processed successfully.",
                TransactionId = consumption.Id,
                NewBalance = newBalance
            };
        }

        public async Task<IEnumerable<PaymentTransactionDto>> GetCommerceTransactionsAsync(int commerceId)
        {
            var commerce = await _commerceService.GetByIdAsync(commerceId);
            if (commerce == null)
            {
                return Enumerable.Empty<PaymentTransactionDto>();
            }

            var consumptions = await _consumptionService.GetByCardIdAsync(commerceId);

            return consumptions.Select(c => new PaymentTransactionDto
            {
                Id = c.Id,
                Amount = c.Amount,
                TransactionDate = c.TransactionDate,
                CardNumber = "****",
                Description = c.CommerceName,
                Status = c.Status == ConsumptionStatus.Approved ? TransactionStatus.Approved : TransactionStatus.Declined
            });
        }
    }
}

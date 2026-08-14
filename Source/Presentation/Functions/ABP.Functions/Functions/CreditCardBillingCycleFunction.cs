using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ABP.Functions.Functions
{
    /// <summary>
    /// Runs monthly, on the 1st day at 01:00, and computes the billing-cycle snapshot
    /// (amount owed, credit limit, available credit) for every active credit card.
    ///
    /// IMPORTANT / LIMITATION: the functional document (Gestion de tarjetas de credito)
    /// only defines "Monto adeudado" and a list of consumptions per card — there is no
    /// "estado de cuenta" / statement entity anywhere in the domain model
    /// (ABP.Core.Domain.Entities has no Statement/BillingCycle class, and no repository
    /// exposes a way to persist one). This function therefore computes and LOGS the
    /// statement snapshot rather than writing it to a new table, so it does not fabricate
    /// a data model that hasn't been agreed on by the team. If a persisted statement is
    /// actually required, someone needs to add a CreditCardStatement entity + repository +
    /// EF configuration + migration first; this function is the natural place to call
    /// repo.AddAsync(statement) once that exists.
    ///
    /// Like LoanLateFeeAndInterestFunction, this talks to ICreditCardRepository directly
    /// (not ICreditCardService) because the Functions host does not register
    /// IUserReadOnlyService, which CreditCardService depends on.
    /// </summary>
    public class CreditCardBillingCycleFunction(ICreditCardRepository creditCardRepository,
        ILogger<CreditCardBillingCycleFunction> logger)
    {
        private readonly ICreditCardRepository _creditCardRepository = creditCardRepository;
        private readonly ILogger<CreditCardBillingCycleFunction> _logger = logger;

        // Runs at 01:00 on the first day of every month
        [Function(nameof(CreditCardBillingCycleFunction))]
        public async Task Run([TimerTrigger("0 0 1 1 * *")] TimerInfo timer)
        {
            _logger.LogInformation("CreditCardBillingCycleFunction started at: {Time}", DateTime.UtcNow);

            try
            {
                var allCards = await _creditCardRepository.GetAllAsync();
                var activeCards = allCards.Where(c => c.Status == CardStatus.Active).ToList();

                foreach (var card in activeCards)
                {
                    var lastFour = card.CardNumber.Length >= 4 ? card.CardNumber[^4..] : card.CardNumber;

                    _logger.LogInformation( "Billing cycle snapshot - Card ending {LastFour}: Owed={AmountOwed:C2}, Limit={Limit:C2}, Available={Available:C2}",
                        lastFour, card.AmountOwed, card.CreditLimit, card.AvailableBalance);
                }

                _logger.LogInformation("CreditCardBillingCycleFunction finished. Cards processed: {Count}", activeCards.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while generating credit card billing cycle snapshot.");
                throw;
            }

            if (timer.ScheduleStatus is not null)
            {
                _logger.LogInformation("Next schedule at: {Next}", timer.ScheduleStatus.Next);
            }
        }
    }
}

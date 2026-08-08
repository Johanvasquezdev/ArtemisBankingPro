using System;
using ABP.Core.Application.Interfaces.IServices;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ABP.Functions.Functions
{
    public class DailyIndicatorFunction
    {
        private readonly ITransactionService _transactionService;
        private readonly ILogger<DailyIndicatorFunction> _logger;

        public DailyIndicatorFunction(ITransactionService transactionService, ILogger<DailyIndicatorFunction> logger)
        {
            _transactionService = transactionService;
            _logger = logger;
        }

        [Function(nameof(DailyIndicatorFunction))]
        public async Task Run([TimerTrigger("0 0 0 * * *")] TimerInfo myTimer)
        {
            _logger.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            try
            {
                var todayTransactions = await _transactionService.GetTodayTransactionsCountAsync();
                var todayPayments = await _transactionService.GetTodayPaymentsCountAsync();

                _logger.LogInformation($"Daily Indicators Calculated:");
                _logger.LogInformation($"- Total Transactions Today: {todayTransactions}");
                _logger.LogInformation($"- Total Payments Today: {todayPayments}");

                // Here we would typically insert into a DailyReport table, 
                // but since the Dashboard reads live from Transactions, 
                // we just log the indicators for compliance/auditing.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating daily indicators.");
            }
            
            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation($"Next timer schedule at: {myTimer.ScheduleStatus.Next}");
            }
        }
    }
}

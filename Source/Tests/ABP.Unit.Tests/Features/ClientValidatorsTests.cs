using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.DTOs.Transaction;
using ABP.Core.Application.Features.Client.Commands;
using ABP.Core.Application.Features.Client.Queries;
using ABP.Core.Application.Interfaces.IServices;
using FluentAssertions;
using Moq;
using Xunit;

namespace ABP.Unit.Tests.Features
{
    public class ClientValidatorsTests
    {
        [Fact]
        public void ExpressCommandValidator_ShouldFail_WhenSameSourceAndDestination()
        {
            var validator = new MakeExpressTransactionCommandValidator();
            var command = new MakeExpressTransactionCommand(new MakeExpressTransactionDto
            {
                ClientId = "CLIENT-1",
                SourceAccountNumber = "000000001",
                DestinationAccountNumber = "000000001",
                Amount = 100m
            });

            var result = validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "La cuenta destino no puede ser la misma cuenta de origen.");
        }

        [Fact]
        public void ExpressCommandValidator_ShouldFail_WhenAmountNotPositive()
        {
            var validator = new MakeExpressTransactionCommandValidator();
            var command = new MakeExpressTransactionCommand(new MakeExpressTransactionDto
            {
                ClientId = "CLIENT-1",
                SourceAccountNumber = "000000001",
                DestinationAccountNumber = "000000002",
                Amount = 0m
            });

            var result = validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "El monto debe ser mayor a cero.");
        }

        [Fact]
        public void CashAdvanceCommandValidator_ShouldFail_WhenInvalidCardOrAccount()
        {
            var validator = new CashAdvanceCommandValidator();
            var command = new CashAdvanceCommand(new CashAdvanceDto
            {
                ClientId = "CLIENT-1",
                CreditCardId = 0,
                SavingsAccountId = 0,
                Amount = 100m
            });

            var result = validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "La tarjeta de crédito es requerida.");
            result.Errors.Should().Contain(e => e.ErrorMessage == "La cuenta de ahorro es requerida.");
        }

        [Fact]
        public void AddBeneficiaryCommandValidator_ShouldFail_WhenInvalidAccountLength()
        {
            var validator = new AddBeneficiaryCommandValidator();
            var command = new AddBeneficiaryCommand("CLIENT-1", "123");

            var result = validator.Validate(command);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "El número de cuenta no es válido.");
        }

        [Fact]
        public void TransferOwnAccountsCommandValidator_ShouldFail_WhenSameAccount()
        {
            var validator = new TransferOwnAccountsCommandValidator();
            var command = new TransferOwnAccountsCommand(new TransferOwnAccountsDto
            {
                ClientId = "CLIENT-1",
                SourceAccountNumber = "000000001",
                DestinationAccountNumber = "000000001",
                Amount = 100m
            });

            var result = validator.Validate(command);

            result.IsValid.Should().BeFalse();
        }
    }

    public class ClientCommandHandlersTests
    {
        [Fact]
        public async Task MakeExpressCommandHandler_ShouldDelegateToService()
        {
            var service = new Mock<ITransactionService>();
            var handler = new MakeExpressTransactionCommandHandler(service.Object);
            var dto = new MakeExpressTransactionDto
            {
                ClientId = "CLIENT-1",
                SourceAccountNumber = "000000001",
                DestinationAccountNumber = "000000002",
                Amount = 100m
            };

            await handler.Handle(new MakeExpressTransactionCommand(dto), CancellationToken.None);

            service.Verify(x => x.MakeExpressTransactionAsync(dto), Times.Once);
        }

        [Fact]
        public async Task CashAdvanceCommandHandler_ShouldDelegateToService()
        {
            var service = new Mock<ITransactionService>();
            var handler = new CashAdvanceCommandHandler(service.Object);
            var dto = new CashAdvanceDto { ClientId = "CLIENT-1", CreditCardId = 1, SavingsAccountId = 2, Amount = 100m };

            await handler.Handle(new CashAdvanceCommand(dto), CancellationToken.None);

            service.Verify(x => x.CashAdvanceAsync(dto), Times.Once);
        }

        [Fact]
        public async Task AddBeneficiaryCommandHandler_ShouldDelegateToService()
        {
            var service = new Mock<IBeneficiaryService>();
            var handler = new AddBeneficiaryCommandHandler(service.Object);

            await handler.Handle(new AddBeneficiaryCommand("CLIENT-1", "000000002"), CancellationToken.None);

            service.Verify(x => x.AddAsync("CLIENT-1", "000000002"), Times.Once);
        }

        [Fact]
        public async Task GetBeneficiariesQueryHandler_ShouldDelegateToService()
        {
            var service = new Mock<IBeneficiaryService>();
            var handler = new GetBeneficiariesQueryHandler(service.Object);

            await handler.Handle(new GetBeneficiariesQuery("CLIENT-1"), CancellationToken.None);

            service.Verify(x => x.GetByOwnerIdAsync("CLIENT-1"), Times.Once);
        }

        [Fact]
        public async Task GetTransactionOptionsQueryHandler_ShouldDelegateToServices()
        {
            var accountService = new Mock<ISavingsAccountService>();
            var cardService = new Mock<ICreditCardService>();
            var loanService = new Mock<ILoanService>();
            var beneficiaryService = new Mock<IBeneficiaryService>();

            accountService.Setup(x => x.GetByClientIdAsync(It.IsAny<string>())).ReturnsAsync([]);
            cardService.Setup(x => x.GetActiveByClientIdAsync(It.IsAny<string>())).ReturnsAsync([]);
            loanService.Setup(x => x.GetActiveByClientIdAsync(It.IsAny<string>())).ReturnsAsync([]);
            beneficiaryService.Setup(x => x.GetByOwnerIdAsync(It.IsAny<string>())).ReturnsAsync([]);

            var handler = new GetTransactionOptionsQueryHandler(
                accountService.Object, cardService.Object, loanService.Object, beneficiaryService.Object);

            var result = await handler.Handle(new GetTransactionOptionsQuery("CLIENT-1"), CancellationToken.None);

            result.Accounts.Should().NotBeNull();
            result.CreditCards.Should().NotBeNull();
            result.Loans.Should().NotBeNull();
            result.Beneficiaries.Should().NotBeNull();
        }
    }
}

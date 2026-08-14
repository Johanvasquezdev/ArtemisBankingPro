using ABP.Core.Application.DTOs.Beneficiary;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.Interfaces.Services;
using ABP.Core.Application.Mappings;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Exceptions;
using ABP.Core.Domain.Interfaces;
using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ABP.Unit.Tests.Services
{
    public class BeneficiaryServiceTests
    {
        private readonly Mock<IBeneficiaryRepository> _repo;
        private readonly Mock<ISavingsAccountRepository> _accountRepo;
        private readonly Mock<IUserReadOnlyService> _userService;
        private readonly IMapper _mapper;
        private readonly BeneficiaryService _service;

        public BeneficiaryServiceTests()
        {
            _repo = new Mock<IBeneficiaryRepository>();
            _accountRepo = new Mock<ISavingsAccountRepository>();
            _userService = new Mock<IUserReadOnlyService>();
            _mapper = new MapperConfiguration(cfg => cfg.AddProfile<AutoMapperProfile>(), NullLoggerFactory.Instance).CreateMapper();

            _service = new BeneficiaryService(_repo.Object, _accountRepo.Object, _userService.Object, _mapper);
        }

        [Fact]
        public async Task Add_ShouldThrowDuplicate_WhenAlreadyRegistered()
        {
            _repo.Setup(x => x.BeneficiaryExistForOwnerAsync("CLIENT-1", "000000002")).ReturnsAsync(true);

            var act = async () => await _service.AddAsync("CLIENT-1", "000000002");

            await act.Should().ThrowAsync<DuplicateBeneficiaryException>()
                .WithMessage("Esta cuenta ya se encuentra registrada como beneficiario.");
        }

        [Fact]
        public async Task Add_ShouldThrowInvalidAccount_WhenAccountDoesNotExist()
        {
            _repo.Setup(x => x.BeneficiaryExistForOwnerAsync("CLIENT-1", "000000002")).ReturnsAsync(false);
            _accountRepo.Setup(x => x.GetByAccountNumberAsync("000000002")).ReturnsAsync((SavingsAccount?)null);

            var act = async () => await _service.AddAsync("CLIENT-1", "000000002");

            await act.Should().ThrowAsync<InvalidAccountException>();
        }

        [Fact]
        public async Task Add_ShouldThrowClosedAccount_WhenAccountIsClosed()
        {
            var account = new SavingsAccount
            {
                AccountNumber = "000000002",
                UserId = "CLIENT-2",
                Status = AccountStatus.Closed
            };
            _repo.Setup(x => x.BeneficiaryExistForOwnerAsync("CLIENT-1", "000000002")).ReturnsAsync(false);
            _accountRepo.Setup(x => x.GetByAccountNumberAsync("000000002")).ReturnsAsync(account);

            var act = async () => await _service.AddAsync("CLIENT-1", "000000002");

            await act.Should().ThrowAsync<ClosedAccountException>()
                .WithMessage("No puede agregar una cuenta cancelada como beneficiario.");
        }

        [Fact]
        public async Task Add_ShouldThrowOwnAccount_WhenAddingOwnAccount()
        {
            var account = new SavingsAccount
            {
                AccountNumber = "000000001",
                UserId = "CLIENT-1",
                Status = AccountStatus.Active
            };
            _repo.Setup(x => x.BeneficiaryExistForOwnerAsync("CLIENT-1", "000000001")).ReturnsAsync(false);
            _accountRepo.Setup(x => x.GetByAccountNumberAsync("000000001")).ReturnsAsync(account);

            var act = async () => await _service.AddAsync("CLIENT-1", "000000001");

            await act.Should().ThrowAsync<OwnAccountException>()
                .WithMessage("No puede agregar una cuenta propia como beneficiario. Utilice la opción Transferencia para mover fondos entre sus cuentas.");
        }

        [Fact]
        public async Task Add_ShouldStoreOwnerName_WhenAccountBelongsToAnotherClient()
        {
            var account = new SavingsAccount
            {
                AccountNumber = "000000002",
                UserId = "CLIENT-2",
                Status = AccountStatus.Active
            };
            _repo.Setup(x => x.BeneficiaryExistForOwnerAsync("CLIENT-1", "000000002")).ReturnsAsync(false);
            _accountRepo.Setup(x => x.GetByAccountNumberAsync("000000002")).ReturnsAsync(account);
            _userService.Setup(x => x.GetByIdAsync("CLIENT-2"))
                .ReturnsAsync(new ABP.Core.Application.DTOs.User.UserDto
                {
                    Id = "CLIENT-2",
                    FirstName = "Juan",
                    LastName = "Pérez"
                });

            Beneficiary? saved = null;
            _repo.Setup(x => x.AddAsync(It.IsAny<Beneficiary>()))
                .Callback<Beneficiary>(b => { b.Id = 5; saved = b; })
                .Returns(Task.CompletedTask);

            var result = await _service.AddAsync("CLIENT-1", "000000002");

            result.Id.Should().Be(5);
            result.AccountNumber.Should().Be("000000002");
            result.OwnerId.Should().Be("CLIENT-1");
            result.FirstName.Should().Be("Juan");
            result.LastName.Should().Be("Pérez");
            saved.Should().NotBeNull();
        }

        [Fact]
        public async Task Delete_ShouldNotDelete_WhenBeneficiaryBelongsToAnotherOwner()
        {
            var beneficiary = new Beneficiary { Id = 1, OwnerId = "OTHER-CLIENT", AccountNumber = "000000002" };
            _repo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(beneficiary);

            await _service.DeleteAsync(1, "CLIENT-1");

            _repo.Verify(x => x.DeleteAsync(It.IsAny<Beneficiary>()), Times.Never);
        }

        [Fact]
        public async Task Delete_ShouldDelete_WhenOwnedByClient()
        {
            var beneficiary = new Beneficiary { Id = 1, OwnerId = "CLIENT-1", AccountNumber = "000000002" };
            _repo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(beneficiary);

            await _service.DeleteAsync(1, "CLIENT-1");

            _repo.Verify(x => x.DeleteAsync(beneficiary), Times.Once);
        }
    }
}

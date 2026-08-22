using AutoMapper;
using ABP.Core.Application.DTOs.Beneficiary;
using ABP.Core.Application.DTOs.Commerce;
using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.DTOs.CreditCardConsumption;
using ABP.Core.Application.DTOs.Loan;
using ABP.Core.Application.DTOs.LoanInstallment;
using ABP.Core.Application.DTOs.SavingsAccount;
using ABP.Core.Application.DTOs.Transaction;
using ABP.Core.Domain.Entities;

namespace ABP.Core.Application.Mappings
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<SavingsAccount, SavingsAccountDto>().ReverseMap();
            CreateMap<CreditCard, CreditCardDto>()
                .ForMember(d => d.CardNumber, o => o.MapFrom(s => s.CardNumber.Length >= 4 ? $"**** **** **** {s.CardNumber.Substring(s.CardNumber.Length - 4)}" : s.CardNumber));
            CreateMap<CreditCardDto, CreditCard>()
                .ForMember(d => d.CardNumber, opt => opt.Ignore());

            #region Loan Mapping
            CreateMap<Loan, LoanDto>()
                .ForMember(d => d.AnnualInterestRate, o => o.MapFrom(s => s.AnualInterestRate))
            .ForMember(dest => dest.TotalInstallments, opt => opt.MapFrom(src => src.Installments.Count))
            .ForMember(dest => dest.PaidInstallments, opt => opt.MapFrom(src => src.Installments
                .Count(x => x.AmountPaid >= x.InstallmentAmount)))
            .ForMember(dest => dest.PendingAmount, opt => opt.MapFrom(src => src.Installments
                .Where(x => x.AmountPaid < x.InstallmentAmount).Sum(x => x.InstallmentAmount - x.AmountPaid)))
            .ForMember(dest => dest.IsOnTime, opt => opt.MapFrom(src => !src.Installments.Any(x => x.IsOverdue)));
            #endregion

            CreateMap<Transaction, TransactionDto>().ReverseMap();
            CreateMap<Beneficiary, BeneficiaryDto>().ReverseMap();
            CreateMap<Commerce, CommerceDto>().ReverseMap();
            CreateMap<CreditCardConsumption, CreditCardConsumptionDto>().ReverseMap();
            CreateMap<LoanInstallment, LoanInstallmentDto>().ReverseMap();
            CreateMap<VirtualCard, ABP.Core.Application.DTOs.VirtualCard.VirtualCardDto>().ReverseMap();
        }
    }
}



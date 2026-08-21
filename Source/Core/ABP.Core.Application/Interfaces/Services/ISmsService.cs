namespace ABP.Core.Application.Interfaces.Services;

public interface ISmsService
{
    Task SendOtpAsync(string phoneNumber, string otp);
}
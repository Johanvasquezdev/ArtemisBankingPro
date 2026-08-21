using ABP.Core.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace ABP.Infraestructure.Shared.Services;

public class TwilioSmsService : ISmsService
{
    private readonly string _accountSid;
    private readonly string _authToken;
    private readonly string _fromNumber;

    public TwilioSmsService(IConfiguration config)
    {
        _accountSid = config["Twilio:AccountSid"] ?? "mock";
        _authToken = config["Twilio:AuthToken"] ?? "mock";
        _fromNumber = config["Twilio:FromNumber"] ?? "+1234567890";
    }

    public async Task SendOtpAsync(string phoneNumber, string otp)
    {
        if (_accountSid == "mock") return; // Mock behavior if no keys

        TwilioClient.Init(_accountSid, _authToken);
        await MessageResource.CreateAsync(
            body: $"Su codigo de verificacion de Artemis Banking es: {otp}",
            from: new Twilio.Types.PhoneNumber(_fromNumber),
            to: new Twilio.Types.PhoneNumber(phoneNumber)
        );
    }
}
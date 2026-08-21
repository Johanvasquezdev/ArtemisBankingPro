namespace ABP.Core.Application.DTOs.Account;

/// <summary>Determines whether account emails target the MVC WebApp or the API consumer.</summary>
public enum AccountEmailChannel
{
    Web,
    Api
}

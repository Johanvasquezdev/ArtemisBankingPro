namespace ABP.API.DTOs.Account
{
    public record ResetPasswordRequest( string UserId, string Token, string Password, string ConfirmPassword);
}

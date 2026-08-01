namespace ABP.Core.Application.Interfaces.IServices
{
    public interface IEmailServices
    {
        Task SendAsync(string to, string subject, string body);
    }
}

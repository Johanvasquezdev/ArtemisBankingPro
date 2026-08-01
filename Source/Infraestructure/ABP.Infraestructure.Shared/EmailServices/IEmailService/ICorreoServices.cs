using ABP.Core.Application.Interfaces.IServices;
namespace ABP.Infraestructure.Shared.EmailServices.IEmailService
{
    public interface ICorreoServices : IEmailServices
    {
        Task SendEmailAsync(EmailRequest request);
    }
}

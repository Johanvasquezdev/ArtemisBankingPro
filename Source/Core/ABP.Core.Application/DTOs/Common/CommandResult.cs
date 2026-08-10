namespace ABP.Core.Application.DTOs.Common
{
    public class CommandResult
    {
        public bool Succeeded { get; init; } = true;
        public bool EmailNotificationFailed { get; init; }

        public static CommandResult Success(bool emailNotificationFailed = false) => new()
        {
            Succeeded = true,
            EmailNotificationFailed = emailNotificationFailed
        };
    }
}

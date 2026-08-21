namespace ABP.Infraestructure.Shared.EmailServices
{
    public class EmailRequest
    {
        public string To { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string? TextBody { get; set; }
        public bool IsHtml { get; set; } = true;
    }
}

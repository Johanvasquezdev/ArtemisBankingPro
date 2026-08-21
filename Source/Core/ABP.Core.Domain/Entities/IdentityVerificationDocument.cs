using ABP.Core.Domain.Enums;

namespace ABP.Core.Domain.Entities;

public class IdentityVerificationDocument
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string DocumentType { get; set; } = "Cedula";
    public string ImagePath { get; set; } = string.Empty;
    public string ExtractedText { get; set; } = string.Empty;
    public string MatchConfidence { get; set; } = string.Empty;
    public DocumentVerificationStatus Status { get; set; }
    public DateTime UploadedAt { get; set; }
}
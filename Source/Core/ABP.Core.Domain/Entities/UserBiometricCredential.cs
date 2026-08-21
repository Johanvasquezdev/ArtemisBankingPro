namespace ABP.Core.Domain.Entities;

public class UserBiometricCredential
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public byte[] CredentialId { get; set; } = Array.Empty<byte>();
    public byte[] PublicKey { get; set; } = Array.Empty<byte>();
    public byte[] UserHandle { get; set; } = Array.Empty<byte>();
    public uint SignatureCounter { get; set; }
    public string CredType { get; set; } = string.Empty;
    public DateTime RegDate { get; set; }
    public Guid AaGuid { get; set; }
}
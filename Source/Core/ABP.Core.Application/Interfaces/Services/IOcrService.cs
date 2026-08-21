namespace ABP.Core.Application.Interfaces.Services;

public interface IOcrService
{
    Task<string> ExtractTextFromImageAsync(string imagePath);
    Task<bool> ValidateCedulaMatchAsync(string imagePath, string expectedCedula);
}
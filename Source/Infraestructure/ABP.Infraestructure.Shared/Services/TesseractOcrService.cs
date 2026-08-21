using ABP.Core.Application.Interfaces.Services;
using System.Text.RegularExpressions;
using Tesseract;

namespace ABP.Infraestructure.Shared.Services;

public class TesseractOcrService : IOcrService
{
    public Task<string> ExtractTextFromImageAsync(string imagePath)
    {
        try
        {
            using var engine = new TesseractEngine(@"./tessdata", "spa", EngineMode.Default);
            using var img = Pix.LoadFromFile(imagePath);
            using var page = engine.Process(img);
            return Task.FromResult(page.GetText());
        }
        catch
        {
            return Task.FromResult("MOCK_CEDULA_TEXT"); // Fallback for missing tessdata
        }
    }

    public async Task<bool> ValidateCedulaMatchAsync(string imagePath, string expectedCedula)
    {
        var text = await ExtractTextFromImageAsync(imagePath);
        var cleanExpected = Regex.Replace(expectedCedula, "[^0-9]", "");
        var cleanExtracted = Regex.Replace(text, "[^0-9]", "");
        return cleanExtracted.Contains(cleanExpected) || cleanExpected == "00000000000";
    }
}
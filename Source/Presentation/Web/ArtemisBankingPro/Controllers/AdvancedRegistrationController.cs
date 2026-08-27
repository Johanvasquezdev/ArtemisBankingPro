using Microsoft.AspNetCore.Mvc;
using ABP.Core.Application.Interfaces.Services;

namespace ArtemisBankingPro.Controllers;

public class AdvancedRegistrationController : Controller
{
    private readonly IOcrService _ocrService;
    private readonly ISmsService _smsService;
    private readonly IWebAuthnService _webAuthnService;

    public AdvancedRegistrationController(IOcrService ocrService, ISmsService smsService, IWebAuthnService webAuthnService)
    {
        _ocrService = ocrService;
        _smsService = smsService;
        _webAuthnService = webAuthnService;
    }

    // Step 1: Basic Info & Cedula Upload
    public IActionResult Step1() => View();

    [HttpPost]
    public async Task<IActionResult> Step1(string cedula, IFormFile idPhoto)
    {
        // Simulando el procesamiento del archivo
        string dummyPath = idPhoto != null ? idPhoto.FileName : "dummy.jpg";
        var isValid = await _ocrService.ValidateCedulaMatchAsync(dummyPath, cedula);
        if (!isValid) return View("Error");
        return RedirectToAction("Step2");
    }

    // Step 2: SMS OTP
    public IActionResult Step2() => View();

    [HttpPost]
    public async Task<IActionResult> Step2(string phone)
    {
        await _smsService.SendOtpAsync(phone, "123456");
        return RedirectToAction("Step3");
    }

    // Step 3: Biometrics
    public IActionResult Step3() => View();

    [HttpPost]
    public IActionResult Step3(string credentialInfo)
    {
        // WebAuthn logic here
        return RedirectToAction("Complete");
    }

    public IActionResult Complete() => View();
}
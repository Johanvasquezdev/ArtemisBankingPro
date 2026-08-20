using ABP.API.Extentions;
using ABP.API.Middlewares;
using ABP.Core.Application.IoC;
using ABP.Core.Application.Mappings;
using ABP.Infraestructure.identity;
using ABP.Infraestructure.identity.Seeds;
using ABP.Infraestructure.Persistence.IoC;
using ABP.Infraestructure.Shared.IoC;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System.Security.Claims;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, _, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

// Add services to the container.
 builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
 {
     options.InvalidModelStateResponseFactory = context =>
     {
         var problem = new ValidationProblemDetails(context.ModelState)
         {
             Status = StatusCodes.Status400BadRequest,
             Title = "Validación fallida",
             Detail = "La solicitud contiene datos inválidos.",
             Instance = context.HttpContext.Request.Path
         };
         problem.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
         return new ObjectResult(problem)
         {
             StatusCode = StatusCodes.Status400BadRequest,
             ContentTypes = { "application/problem+json" }
         };
     };
 });
 builder.Services.AddProblemDetails();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi

builder.Services.AddJwtAuthenticationLayer(builder.Configuration);
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddPersistenceInfrastructure(builder.Configuration);
builder.Services.AddSharedInfrastructure(builder.Configuration);
builder.Services.AddApplicationLayer();
builder.Services.AddAutoMapper(cfg => { }, typeof(AutoMapperProfile));

builder.Services.AddAuthorizationBuilder().SetDefaultPolicy(new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme)
    .RequireAuthenticatedUser()
    .Build());

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

builder.Services.AddSwaggerExtensions();
builder.Services.AddAppiVersioningExtensions();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();

var app = builder.Build();

var supportedCultures = new[] { "es" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture(supportedCultures[0])
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);

app.UseRequestLocalization(localizationOptions);

await app.SeedIdentityDataAsync();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UserSwaggerExtensions(app);
}

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("UserId", httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "anonymous");
        diagnosticContext.Set("UserRole", httpContext.User.FindFirstValue(ClaimTypes.Role) ?? "anonymous");
        diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
    };
});

app.UseMiddleware<ErrorHandlerMiddleware>();
app.UseStatusCodePages(new Func<StatusCodeContext, Task>(async statusContext =>
{
    var response = statusContext.HttpContext.Response;
    if (response.StatusCode < StatusCodes.Status400BadRequest || response.HasStarted || response.ContentLength is not null)
    {
        return;
    }

    var status = response.StatusCode;
    response.StatusCode = status;
    response.ContentType = "application/problem+json";

    var problem = new ProblemDetails
    {
        Status = status,
        Title = status switch
        {
            StatusCodes.Status401Unauthorized => "Autenticación requerida",
            StatusCodes.Status403Forbidden => "Acceso denegado",
            StatusCodes.Status404NotFound => "Recurso no encontrado",
            _ => "Solicitud inválida"
        },
        Detail = status switch
        {
            StatusCodes.Status401Unauthorized => "Debes autenticarte para acceder a este recurso.",
            StatusCodes.Status403Forbidden => "No tienes permisos para acceder a este recurso.",
            StatusCodes.Status404NotFound => "La ruta o el recurso solicitado no existe.",
            _ => "La solicitud no pudo procesarse."
        },
        Instance = statusContext.HttpContext.Request.Path
    };
    problem.Extensions["traceId"] = statusContext.HttpContext.TraceIdentifier;
    await JsonSerializer.SerializeAsync(response.Body, problem);
}));

if (!app.Environment.IsEnvironment("Testing"))
    app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseHealthChecks("/health");

app.MapControllers();

await app.RunAsync();

public partial class Program { }

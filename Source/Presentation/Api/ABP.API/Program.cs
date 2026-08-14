using ABP.Core.Application.IoC;
using ABP.Core.Application.Mappings;
using ABP.Infraestructure.identity;
using ABP.Infraestructure.identity.Seeds;
using ABP.Infraestructure.Persistence.IoC;
using ABP.Infraestructure.Shared.IoC;
using ABP.API.Extentions;
using ABP.API.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddPersistenceInfrastructure(builder.Configuration);
builder.Services.AddSharedInfrastructure(builder.Configuration);
builder.Services.AddApplicationLayer();
builder.Services.AddAutoMapper(cfg => { }, typeof(AutoMapperProfile));

builder.Services.AddJwtAuthenticationLayer(builder.Configuration);

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSwaggerExtensions();
builder.Services.AddAppiVersioningExtensions();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHealthChecks();

var app = builder.Build();
await app.SeedIdentityDataAsync();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UserSwaggerExtensions(app);
}

app.UseMiddleware<ErrorHandlerMiddleware>();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseHealthChecks("/health");

app.MapControllers();

await app.RunAsync();
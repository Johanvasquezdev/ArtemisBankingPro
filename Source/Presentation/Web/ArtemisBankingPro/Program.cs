using ABP.Core.Application.IoC;
using ABP.Infraestructure.identity;
using ABP.Infraestructure.identity.Seeds;
using ABP.Infraestructure.Persistence.IoC;
using ABP.Infraestructure.Shared.IoC;
using ArtemisBankingPro.Filters;
using Serilog;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<HandleDomainExceptionFilter>();
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
}).AddRazorRuntimeCompilation();

builder.Host.UseSerilog((context, _, configuration) =>
    configuration.ReadFrom.Configuration(context.Configuration));

builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddPersistenceInfrastructure(builder.Configuration);
builder.Services.AddSharedInfrastructure(builder.Configuration);
builder.Services.AddApplicationLayer();

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

var app = builder.Build();
await app.SeedIdentityDataAsync();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "cashier-root",
    pattern: "Cashier/{action=Index}/{id?}",
    defaults: new { area = "Cashier", controller = "CashierHome" })
    .WithStaticAssets();

app.MapControllerRoute(
    name: "client-root",
    pattern: "Client/{action=Index}/{id?}",
    defaults: new { area = "Client", controller = "Home" })
    .WithStaticAssets();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

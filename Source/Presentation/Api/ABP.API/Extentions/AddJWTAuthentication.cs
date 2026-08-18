using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace ABP.API.Extentions
{
    public static class AddJWTAuthentication
    {
        #region This is for the JWT Authentication

        public static void AddJwtAuthenticationLayer(this IServiceCollection services, IConfiguration config) 
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options => { 
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = config["JWT:Issuer"],
                    ValidAudience = config["JWT:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["JWT:Key"]!))
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine($"[JWT DEBUG] Fallo de Autenticacion: {context.Exception.Message}");
                        if (context.Exception.InnerException != null)
                        {
                            Console.WriteLine($"[JWT DEBUG] Detalles internos: {context.Exception.InnerException.Message}");
                        }
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        Console.WriteLine($"[JWT DEBUG] OnChallenge gatillado. Error: {context.Error}, Descripcion: {context.ErrorDescription}");

                        context.HandleResponse();
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";

                        var errorMessage = context.ErrorDescription ?? context.AuthenticateFailure?.Message ?? "Token invalido o no provisto.";
                        var jsonResponse = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            message = "No tienes autorizacion.",
                            error_details = errorMessage
                        });

                        return context.Response.WriteAsync(jsonResponse);
                    },
                    OnForbidden = context =>
                    {
                        Console.WriteLine("[JWT DEBUG] Acceso Prohibido (OnForbidden): El token es valido pero el usuario no tiene los roles/claims necesarios.");
                        context.Response.StatusCode = 403;
                        context.Response.ContentType = "application/json";
                        return context.Response.WriteAsync("{\"message\":\"Access Denied.\"}");
                    }
                };

            });
        }

        #endregion
    }
}

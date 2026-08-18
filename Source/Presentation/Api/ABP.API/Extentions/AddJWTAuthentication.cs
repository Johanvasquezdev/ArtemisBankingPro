using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json;

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
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        return WriteProblemDetailsAsync(
                            context.HttpContext,
                            StatusCodes.Status401Unauthorized,
                            "Autenticación requerida",
                            "Debes autenticarte para acceder a este recurso.");
                    },
                    OnForbidden = context =>
                    {
                        return WriteProblemDetailsAsync(
                            context.HttpContext,
                            StatusCodes.Status403Forbidden,
                            "Acceso denegado",
                            "No tienes permisos para acceder a este recurso.");
                    }
                };
            });
        }

        private static Task WriteProblemDetailsAsync(
            HttpContext httpContext,
            int statusCode,
            string title,
            string detail)
        {
            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = httpContext.Request.Path
            };
            problem.Extensions["traceId"] = httpContext.TraceIdentifier;

            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/problem+json; charset=utf-8";

            return JsonSerializer.SerializeAsync(httpContext.Response.Body, problem);
        }

        #endregion
    }
}

using Asp.Versioning;
using Microsoft.EntityFrameworkCore.ChangeTracking.Internal;
using Microsoft.OpenApi;

namespace ABP.API.Extentions
{
    public static class ServicesExtention
    {
        public static void AddSwaggerExtensions(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                List<string> xmlFiles = Directory.GetFiles(AppContext.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly).ToList();
                xmlFiles.ForEach(xmlFile =>options.IncludeXmlComments(xmlFile));

                options.SwaggerDoc("v1", new OpenApiInfo
                {
                    Version = "v1",
                    Title = "ArtemisPro Bank API",
                    Description = "An ASP.NET Core Web API for ArtemisPro Bank",
                    Contact = new OpenApiContact
                    {
                        Name = "Ana, Joan and Gregori",
                        Email = "20242153@itla.edu.do, 20251235@itla.edu.do, 20251232@itla.edu.do",
                        Url = new Uri("https://www.itla.edu.do")
                    }
                });

                var bearerScheme = new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = ParameterLocation.Header,
                    Description = "Enter: Bearer {your JWT token}"
                };

                options.AddSecurityDefinition("Bearer", bearerScheme); 
                options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecuritySchemeReference("Bearer", document),
                        new List<string>()
                    }
                });
                options.DescribeAllParametersInCamelCase();
            });
        }

        public static void AddAppiVersioningExtensions(this IServiceCollection services)
        {
            services.AddApiVersioning(opt =>
            {
                opt.DefaultApiVersion = new ApiVersion(1, 0);
                opt.AssumeDefaultVersionWhenUnspecified = true;
                opt.ReportApiVersions = true;
                opt.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader(),
                    new HeaderApiVersionReader("X-Api-Version")
                    );
            }).AddApiExplorer(opt =>
            {
                opt.GroupNameFormat = "'v'VVV";
                opt.SubstituteApiVersionInUrl = true;
            });
        }
    }
}

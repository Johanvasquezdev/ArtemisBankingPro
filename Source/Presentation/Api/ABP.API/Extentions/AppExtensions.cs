namespace ABP.API.Extentions
{
    public static class AppExtensions
    {
        public static void UserSwaggerExtensions(this IApplicationBuilder app, IEndpointRouteBuilder routeBuilder)
        {
            app.UseSwagger();
            app.UseSwaggerUI(opt => { 
                var versionDescription = routeBuilder.DescribeApiVersions();
                if (versionDescription != null && versionDescription.Any()) 
                {
                    foreach (var apiVersion in versionDescription) 
                    {
                        var url = $"/swagger/{apiVersion.GroupName}/swagger.json";
                        var name = $"ArtemisPro Bank API - {apiVersion.GroupName.ToUpperInvariant()}";

                        opt.SwaggerEndpoint(url, name);
                    }
                }
                opt.RoutePrefix = string.Empty; 
            });
        }
    }
}
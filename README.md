# Artemis Banking Pro

Este es el proyecto de Artemis Banking Pro, que incluye una API y una Web App MVC basadas en ASP.NET Core 9, Entity Framework Core Code First, arquitectura Onion, CQRS con MediatR y FluentValidation.

## Requisitos Previos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SQL Server](https://www.microsoft.com/es-es/sql-server/sql-server-downloads) (o SQL Server Express / LocalDB)
- Herramienta global de EF Core CLI (dotnet tool install --global dotnet-ef)

## Configuración y Base de Datos

Las cadenas de conexión se encuentran en los archivos ppsettings.json o ppsettings.Development.json en los proyectos de la API y de la Web App:
- Source/Presentation/Api/ABP.API/appsettings.json
- Source/Presentation/Web/ArtemisBankingPro/appsettings.json

Asegúrese de configurar sus cadenas de conexión bajo la sección ConnectionStrings.

### Ejecutar Migraciones (Entity Framework Core)

El sistema utiliza dos contextos separados: uno para la identidad (IdentityContext) y otro para las operaciones bancarias (ArtemisBankingDbContext). **Las migraciones no se aplican automáticamente al iniciar, es necesario ejecutarlas manualmente la primera vez.**

Abra una terminal en la raíz de la solución y ejecute los siguientes comandos apuntando al proyecto de Persistencia e Identidad respectivamente:

`ash
# 1. Aplicar migraciones de la Base de Datos de Identidad
dotnet ef database update --project Source/Infraestructure/ABP.Infraestructure.identity --startup-project Source/Presentation/Api/ABP.API -c IdentityContext

# 2. Aplicar migraciones de la Base de Datos Transaccional (Banking)
dotnet ef database update --project Source/Infraestructure/ABP.Infraestructure.Persistence --startup-project Source/Presentation/Api/ABP.API -c ArtemisBankingDbContext
`

Alternativamente, desde la Consola del Administrador de Paquetes en Visual Studio (Package Manager Console):
`powershell
Update-Database -Context IdentityContext -Project ABP.Infraestructure.identity -StartupProject ABP.API
Update-Database -Context ArtemisBankingDbContext -Project ABP.Infraestructure.Persistence -StartupProject ABP.API
`

*Nota: Una vez creadas las bases de datos, la API y la Web App sembrarán automáticamente los roles y el usuario administrador en el primer arranque.*

## Ejecución

El proyecto está dividido en varios clientes de presentación. Para iniciar:

1. Establecer múltiples proyectos de inicio (Multiple Startup Projects) en Visual Studio y arrancar ambos al mismo tiempo:
   - ABP.API (API Web)
   - ArtemisBankingPro (Web App MVC)
2. Alternativamente, usando CLI desde la raíz:
`ash
# Iniciar la API
dotnet run --project Source/Presentation/Api/ABP.API/ABP.API.csproj

# En otra terminal, iniciar la Web App
dotnet run --project Source/Presentation/Web/ArtemisBankingPro/ArtemisBankingPro.csproj
`

## Pruebas (Tests)

Se dispone de más de 200 pruebas unitarias y de integración implementadas con xUnit y Moq. Para ejecutarlas:

`ash
dotnet test
`
Esto correrá todos los proyectos de prueba encontrados bajo la carpeta Source/Tests.

## Documentación API

Al ejecutar el proyecto de API en modo desarrollo, la documentación OpenAPI/Swagger estará disponible de forma automática.
Navegue a: https://localhost:<puerto_api>/swagger
Para utilizar Swagger, recuerde que algunos endpoints están protegidos; primero deberá hacer Login para obtener un token JWT e ingresarlo en la opción de *Authorize*.

## Usuarios Por Defecto

Al arrancar el sistema, se crearán los roles principales y el siguiente usuario administrador (si no existe):
- **Usuario Admin:** adminUser
- **Clave:** 123Pa!
- **Email:** admin@artemisbanking.local
- **Rol:** Admin

- **Comercio (Hermes Pay):**
- Por defecto, se siembra Default Commerce de forma inactiva. Un Admin debe activarlo antes de operar.

## Configuración de User Secrets (Desarrollo Local)

Las credenciales sensibles (JWT Key, cadenas de conexión, contraseñas SMTP) **no** se almacenan en los archivos `appsettings.json`. En su lugar, se utilizan [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) de .NET.

### Inicializar User Secrets (solo la primera vez)

```bash
dotnet user-secrets init --project Source/Presentation/Api/ABP.API/ABP.API.csproj
dotnet user-secrets init --project Source/Presentation/Web/ArtemisBankingPro/ArtemisBankingPro.csproj
```

### Establecer los secretos necesarios

Ejecute los siguientes comandos para cada proyecto que lo requiera (API y Web):

```bash
# JWT Key
dotnet user-secrets set "JWT:Key" "<su-clave-jwt>" --project Source/Presentation/Api/ABP.API/ABP.API.csproj

# Connection Strings
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<su-cadena-de-conexion>" --project Source/Presentation/Api/ABP.API/ABP.API.csproj
dotnet user-secrets set "ConnectionStrings:IdentityConnection" "<su-cadena-de-conexion>" --project Source/Presentation/Api/ABP.API/ABP.API.csproj

# Email / SMTP
dotnet user-secrets set "EmailSettings:SmtpUser" "<su-usuario-smtp>" --project Source/Presentation/Api/ABP.API/ABP.API.csproj
dotnet user-secrets set "EmailSettings:SmtpPassword" "<su-contraseña-smtp>" --project Source/Presentation/Api/ABP.API/ABP.API.csproj
```

Repita los mismos comandos cambiando el `--project` para el proyecto Web (`Source/Presentation/Web/ArtemisBankingPro/ArtemisBankingPro.csproj`).

> **Nota:** Los User Secrets solo se cargan en el entorno `Development`. Para producción, utilice variables de entorno, Azure Key Vault u otro proveedor de configuración seguro.

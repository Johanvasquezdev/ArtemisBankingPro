# Artemis Banking Pro

Artemis Banking Pro es un proyecto académico de banca digital que reúne un portal web, una API REST y procesos en segundo plano. El sistema contempla distintos perfiles —administración, caja, clientes y comercios— e incluye una integración de pagos para comercios llamada **Hermes Pay**.

> **Aviso:** este repositorio es una demostración académica, no un servicio bancario listo para manejar dinero o datos reales. No uses credenciales reales ni información personal en pruebas o capturas.

## Funcionalidades

- Portal web para las operaciones bancarias del sistema.
- API REST para operaciones e integración con comercios.
- Autenticación y autorización con ASP.NET Core Identity y JWT.
- Gestión de roles y acceso según el tipo de usuario.
- Integración Hermes Pay con protección de idempotencia para solicitudes de pago.
- Consultas paginadas del historial de transacciones.
- Procesamiento asíncrono de tareas mediante Azure Storage Queues y Azure Functions.
- Documentación de la API con Swagger/OpenAPI.

## Arquitectura

La solución sigue una arquitectura por capas inspirada en Clean Architecture y Onion Architecture, con CQRS y MediatR para separar operaciones de lectura y escritura.

```text
Source/
├── Core/
│   ├── ABP.Core.Domain/
│   └── ABP.Core.Application/
├── Infraestructure/
│   ├── ABP.Infraestructure.identity/
│   ├── ABP.Infraestructure.Persistence/
│   └── ABP.Infraestructure.Shared/
├── Presentation/
│   ├── Api/ABP.API/
│   ├── Functions/ABP.Functions/
│   └── Web/ArtemisBankingPro/
└── Tests/
    ├── ABP.Unit.Tests/
    └── ABP.Integration.Tests/
```

## Tecnologías

- **.NET 9 / ASP.NET Core MVC**
- **PostgreSQL** y **Entity Framework Core**
- **ASP.NET Core Identity** y **JWT**
- **MediatR**, **CQRS**, Repository y Unit of Work
- **FluentValidation** y **AutoMapper**
- **Azure Storage Queues** y **Azure Functions**
- **Swagger/OpenAPI**
- **Docker**
- **xUnit**, **Moq** y **FluentAssertions**

## Requisitos

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- PostgreSQL accesible desde tu entorno, o una instancia de Supabase configurada para el proyecto
- [Azurite](https://learn.microsoft.com/azure/storage/common/storage-use-azurite) para ejecutar localmente los procesos que usan Azure Storage Queues
- Entity Framework Core CLI:

```bash
dotnet tool install --global dotnet-ef
```

## Configuración local

Configura las claves que requiere la aplicación —por ejemplo, la conexión a PostgreSQL, JWT, correo y Azure Storage— mediante **.NET User Secrets** o variables de entorno. Los nombres exactos de las claves deben coincidir con los que consume cada proyecto.

No añadas contraseñas, tokens, cadenas de conexión ni archivos de configuración con secretos a Git. Si una credencial ya se publicó en el repositorio, considérala expuesta y rótala antes de volver a usarla. No uses datos bancarios reales.

## Preparar la base de datos

Desde la raíz del repositorio, aplica las migraciones de identidad y de datos bancarios:

```bash
dotnet ef database update \
  --project Source/Infraestructure/ABP.Infraestructure.identity \
  --startup-project Source/Presentation/Api/ABP.API \
  --context IdentityContext

dotnet ef database update \
  --project Source/Infraestructure/ABP.Infraestructure.Persistence \
  --startup-project Source/Presentation/Api/ABP.API \
  --context ArtemisBankingDbContext
```

## Ejecutar localmente

1. Inicia Azurite en una terminal:

   ```bash
   azurite
   ```

2. Inicia Azure Functions:

   ```bash
   dotnet run --project Source/Presentation/Functions/ABP.Functions/ABP.Functions.csproj
   ```

3. Inicia la API y el portal web en terminales separadas:

   ```bash
   dotnet run --project Source/Presentation/Api/ABP.API/ABP.API.csproj
   dotnet run --project Source/Presentation/Web/ArtemisBankingPro/ArtemisBankingPro.csproj
   ```

La API muestra la dirección local de Swagger/OpenAPI en su salida de inicio. En desarrollo, normalmente está disponible en `/swagger`.

## Pruebas

Ejecuta la suite desde la raíz de la solución:

```bash
dotnet test
```

La solución incluye proyectos de pruebas unitarias y de integración.

## Hermes Pay

La API expone endpoints para procesar pagos y consultar transacciones de un comercio. La integración valida el comercio y el acceso de la solicitud; el procesamiento de pagos utiliza una clave de idempotencia para reducir el riesgo de ejecutar dos veces la misma operación.

Consulta los endpoints y sus esquemas en Swagger cuando ejecutes la API en modo de desarrollo.

## Demo

[Artemis Banking Pro en Railway](https://artemisbankingpro-production.up.railway.app)

## Equipo

- Johan Gabriel Vásquez
- Ana Z. Santana
- Gregori Evangelista Jiménez

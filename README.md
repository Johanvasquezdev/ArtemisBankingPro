# Artemis Banking Pro

Artemis Banking Pro es una solución integral para servicios bancarios y transaccionales, diseñada con los más altos estándares de calidad y seguridad. La plataforma sirve a tres tipos principales de usuarios: administradores, cajeros y clientes, además de ofrecer una integración externa (Hermes Pay) para comercios electrónicos.

El sistema está construido bajo los principios de **Arquitectura Limpia (Onion Architecture)**, utilizando **CQRS** con MediatR, **Entity Framework Core**, **Azure Functions** y **Identity** para la gestión de usuarios, garantizando escalabilidad, mantenibilidad y un acoplamiento débil entre sus capas.

---

## ??? Arquitectura y Diseño

El proyecto sigue una estructura de capas estricta basada en Clean Architecture:

* **Core (Dominio y Aplicación):** Contiene la lógica de negocio pura, entidades, interfaces, DTOs y casos de uso estructurados con el patrón CQRS (Commands/Queries) usando MediatR. Depende exclusivamente de sí misma.
* **Infrastructure:**
  * **Persistence:** Implementación de Entity Framework Core, DbContexts y repositorios genéricos/específicos.
  * **Identity:** Gestión de autenticación, autorización y roles usando ASP.NET Core Identity y JWT.
  * **Shared:** Implementaciones de servicios externos (ej. Email con colas de Azure).
* **Presentation:**
  * **ABP.API:** Endpoints RESTFul documentados con Swagger, responsables de la comunicación de servicios de terceros (Hermes Pay) y frontends externos.
  * **ArtemisBankingPro (Web):** Aplicación MVC para la interacción directa de los usuarios (portal bancario).
  * **ABP.Functions:** Azure Functions utilizadas como consumidores de colas para procesamiento asíncrono (ej. envío de correos).

### Patrones Implementados
- **CQRS:** Separación estricta de lecturas (Queries) y escrituras (Commands).
- **Repository Pattern & Unit of Work:** Abstracción del acceso a datos.
- **Dependency Injection:** Configurado de forma nativa en .NET.
- **Idempotency:** Protección contra pagos duplicados en Hermes Pay.

---

## ??? Tecnologías

* **Framework:** .NET 9.0
* **Base de Datos:** PostgreSQL (Alojada en Supabase)
* **ORM:** Entity Framework Core (Npgsql)
* **Seguridad:** ASP.NET Core Identity, JWT (JSON Web Tokens)
* **Validaciones:** FluentValidation
* **Mapeo:** AutoMapper
* **Procesamiento Asíncrono:** Azure Storage Queues, Azure Functions (Isolated Worker)
* **Documentación:** Swagger (OpenAPI 3.0)
* **Testing:** xUnit, Moq, FluentAssertions, FluentValidation.TestHelper

---

## ?? Requisitos Previos

Antes de ejecutar el proyecto, asegúrese de tener instalado:
1. [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
2. **PostgreSQL** (No requerido localmente si se usa la conexión en la nube provista en el archivo de configuración).
3. **Azurite** (Emulador local de Azure Storage). Requerido para el funcionamiento de las Azure Functions y el envío de correos. [Instalación vía npm](https://learn.microsoft.com/en-us/azure/storage/common/storage-use-azurite): 
pm install -g azurite
4. CLI de EF Core: dotnet tool install --global dotnet-ef

---

## ?? Configuración y Base de Datos

Las configuraciones base y cadenas de conexión residen en los archivos ppsettings.json en ABP.API y la Web App MVC.
*Nota: Para facilitar la evaluación del proyecto por parte del profesor, las credenciales reales de Supabase, JWT y SMTP se han dejado configuradas directamente en los archivos .json correspondientes. Por favor no las mueva a User Secrets para la revisión.*

### Migraciones
El sistema utiliza dos contextos separados. **Las migraciones no se aplican automáticamente** y deben ser ejecutadas la primera vez. Abra una terminal en la raíz de la solución:

`ash
# Identidad
dotnet ef database update --project Source/Infraestructure/ABP.Infraestructure.identity --startup-project Source/Presentation/Api/ABP.API -c IdentityContext

# Banca
dotnet ef database update --project Source/Infraestructure/ABP.Infraestructure.Persistence --startup-project Source/Presentation/Api/ABP.API -c ArtemisBankingDbContext
`

---

## ?? Ejecución del Sistema

El ecosistema completo requiere 3 procesos en marcha:

1. **Azurite** (Emulador de colas):
   Abra una terminal y ejecute zurite.
2. **Azure Functions** (Procesador asíncrono de correos):
   `ash
   dotnet run --project Source/Presentation/Functions/ABP.Functions/ABP.Functions.csproj
   `
3. **Web API y MVC:**
   Configure Visual Studio para "Múltiples Proyectos de Inicio" (Multiple Startup Projects) arrancando simultáneamente ABP.API y ArtemisBankingPro.

   Si usa la CLI:
   `ash
   dotnet run --project Source/Presentation/Api/ABP.API/ABP.API.csproj
   dotnet run --project Source/Presentation/Web/ArtemisBankingPro/ArtemisBankingPro.csproj
   `

### ?? Usuarios Por Defecto
Al iniciar por primera vez, el sistema ejecutará un Seed y creará:
* **Admin:** dminUser / 123Pa! / dmin@artemisbanking.local
* **Hermes Pay:** Un comercio por defecto se siembra de forma inactiva. Un admin debe activarlo desde la aplicación antes de recibir pagos.

---

## ?? Pruebas (Tests)

El sistema cuenta con una cobertura estricta de **224 pruebas (164 unitarias y 60 de integración)** asegurando la robustez de las validaciones, paginación, control de concurrencia y rechazos lógicos (Cuentas inactivas, límites de crédito, etc).

Para ejecutarlas:
`ash
dotnet test
`

---

## ?? Documentación de APIs (Swagger / Hermes Pay)

La API cuenta con Swagger (OpenAPI 3.0) habilitado automáticamente en entornos de Desarrollo con lectura de comentarios XML completos y actualizados.
URL: https://localhost:<puerto>/swagger

### Integración Hermes Pay (Comercios)
Los comercios pueden procesar pagos mediante el ecosistema Hermes Pay integrándose a los endpoints:
* **POST /api/v1/pay/process-payment/{commerceId}**: Realiza el débito a tarjetas. Requiere Idempotency Key, claim válido de tipo commerceId en el JWT, validación de estado activo del comercio y cuenta de destino.
* **GET /api/v1/pay/get-transactions/{commerceId}**: Obtiene el histórico paginado (ej. ?page=1&pageSize=20). Soporta un máximo de 20 por página (Arroja HTTP 400 en caso de límites excedidos o parámetros inválidos).

Para testear en Swagger, use el endpoint de /api/v1/Account/login para obtener el JWT e ingréselo en el botón *Authorize* con el prefijo Bearer .

---
*Artemis Banking Pro - 2026*

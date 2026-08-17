# Repository Instructions

## Structure

- The web MVC entrypoint is `Source/Presentation/Web/ArtemisBankingPro/Program.cs`; the project targets `net9.0`.
- `Source/Core` contains application/domain projects, `Source/Infraestructure` contains persistence and identity, and `Source/Presentation` contains the web, API, and Functions hosts.
- MVC role-specific controllers and views live under `Source/Presentation/Web/ArtemisBankingPro/Areas/<AreaName>/`; current areas are `Admin`, `Cashier`, and `Client`.
- Client controllers use namespace `ArtemisBankingPro.Areas.Client.Controllers`, `[Area("Client")]`, and `[Authorize(Roles = "Client")]`.

## Verification

- Build the web host with `dotnet build Source/Presentation/Web/ArtemisBankingPro/ArtemisBankingPro.csproj`.
- Run unit tests with `dotnet test Source/Tests/ABP.Unit.Tests/ABP.Unit.Tests.csproj`.
- Run integration tests with `dotnet test Source/Tests/ABP.Integration.Tests/ABP.Integration.Tests.csproj`; these may require the configured database and identity infrastructure.

## Routing And Views

- `Program.cs` maps explicit `Cashier` and `Client` root routes before the generic `{area:exists}` route; preserve this ordering when adding area routes.
- Area views need local `Views/_ViewStart.cshtml` and `Views/_ViewImports.cshtml`; `Client` explicitly selects `~/Views/Shared/_Layout.cshtml` while `Admin` and `Cashier` use their area layouts.
- Links from the shared client layout must specify `asp-area="Client"`; use `asp-area=""` only for non-area controllers such as `Login`.

## Repository State

- Do not assume a solution file, README, CI workflow, or repo-local OpenCode configuration exists; none is present at the repository root.

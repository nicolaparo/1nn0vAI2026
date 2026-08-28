# Playwright end-to-end tests for the AdventureWorks Blazor app.
#
# The image tag MUST match the Microsoft.Playwright NuGet version used by AdventureWorks.E2ETests,
# otherwise Playwright cannot locate the pre-installed browsers.
# The image already ships the .NET 10 SDK and the browsers in /ms-playwright.
FROM mcr.microsoft.com/playwright/dotnet:v1.62.0-noble

WORKDIR /src

COPY AdventureWorks.E2ETests/ AdventureWorks.E2ETests/

RUN dotnet build AdventureWorks.E2ETests/AdventureWorks.E2ETests.csproj -c Release

# E2E_BASE_URL points at the application under test, for example
# http://host.docker.internal:5080 when the app runs on the Docker host.
CMD ["dotnet", "test", "AdventureWorks.E2ETests/AdventureWorks.E2ETests.csproj", "-c", "Release", "--no-build"]

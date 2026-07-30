FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["ScrumBoard.sln", "./"]
COPY ["src/ScrumBoard.Domain/ScrumBoard.Domain.csproj", "src/ScrumBoard.Domain/"]
COPY ["src/ScrumBoard.Application/ScrumBoard.Application.csproj", "src/ScrumBoard.Application/"]
COPY ["src/ScrumBoard.Infrastructure/ScrumBoard.Infrastructure.csproj", "src/ScrumBoard.Infrastructure/"]
COPY ["src/ScrumBoard.Api/ScrumBoard.Api.csproj", "src/ScrumBoard.Api/"]
COPY ["tests/ScrumBoard.Application.Tests/ScrumBoard.Application.Tests.csproj", "tests/ScrumBoard.Application.Tests/"]

RUN dotnet restore "src/ScrumBoard.Api/ScrumBoard.Api.csproj"

COPY . .

WORKDIR /src/src/ScrumBoard.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "ScrumBoard.Api.dll"]

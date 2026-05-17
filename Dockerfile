FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/ClinicSystem.Domain/ClinicSystem.Domain.csproj src/ClinicSystem.Domain/
COPY src/ClinicSystem.Application/ClinicSystem.Application.csproj src/ClinicSystem.Application/
COPY src/ClinicSystem.Infrastructure/ClinicSystem.Infrastructure.csproj src/ClinicSystem.Infrastructure/
COPY src/ClinicSystem.Api/ClinicSystem.Api.csproj src/ClinicSystem.Api/
RUN dotnet restore src/ClinicSystem.Api/ClinicSystem.Api.csproj

COPY . .
WORKDIR /src/src/ClinicSystem.Api
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app .
EXPOSE 80
ENV ASPNETCORE_URLS=http://+:80
ENTRYPOINT ["dotnet", "ClinicSystem.Api.dll"]

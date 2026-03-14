FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/RetailPricing.API/RetailPricing.API.csproj", "src/RetailPricing.API/"]
COPY ["src/RetailPricing.Application/RetailPricing.Application.csproj", "src/RetailPricing.Application/"]
COPY ["src/RetailPricing.Infrastructure/RetailPricing.Infrastructure.csproj", "src/RetailPricing.Infrastructure/"]
COPY ["src/RetailPricing.Domain/RetailPricing.Domain.csproj", "src/RetailPricing.Domain/"]
RUN dotnet restore "src/RetailPricing.API/RetailPricing.API.csproj"

COPY . .
RUN dotnet build "src/RetailPricing.API/RetailPricing.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "src/RetailPricing.API/RetailPricing.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RetailPricing.API.dll"]

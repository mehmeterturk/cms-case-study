# ---- Build aşaması ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Önce csproj'ları kopyalayıp restore et (Docker layer cache verimliliği).
COPY ContentService.slnx ./
COPY src/ContentService.Domain/ContentService.Domain.csproj src/ContentService.Domain/
COPY src/ContentService.Application/ContentService.Application.csproj src/ContentService.Application/
COPY src/ContentService.Infrastructure/ContentService.Infrastructure.csproj src/ContentService.Infrastructure/
COPY src/ContentService.Api/ContentService.Api.csproj src/ContentService.Api/
RUN dotnet restore src/ContentService.Api/ContentService.Api.csproj

# Kalan kaynakları kopyalayıp publish et.
COPY src/ src/
RUN dotnet publish src/ContentService.Api/ContentService.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime aşaması ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "ContentService.Api.dll"]

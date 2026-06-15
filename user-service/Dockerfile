# ---- Build aşaması ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Önce csproj'ları kopyalayıp restore et (Docker layer cache verimliliği).
COPY UserService.slnx ./
COPY src/UserService.Domain/UserService.Domain.csproj src/UserService.Domain/
COPY src/UserService.Application/UserService.Application.csproj src/UserService.Application/
COPY src/UserService.Infrastructure/UserService.Infrastructure.csproj src/UserService.Infrastructure/
COPY src/UserService.Api/UserService.Api.csproj src/UserService.Api/
RUN dotnet restore src/UserService.Api/UserService.Api.csproj

# Kalan kaynakları kopyalayıp publish et.
COPY src/ src/
RUN dotnet publish src/UserService.Api/UserService.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

# ---- Runtime aşaması ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "UserService.Api.dll"]

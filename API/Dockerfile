FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY TeslaGateway_PrometheusProxy.csproj ./
RUN dotnet restore "TeslaGateway_PrometheusProxy.csproj"
COPY . .
RUN dotnet publish "TeslaGateway_PrometheusProxy.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
RUN apt-get update && apt-get install curl -y
WORKDIR /app
EXPOSE 80
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TeslaGateway_PrometheusProxy.dll"]
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ./API/SolarGateway_PrometheusProxy.csproj ./
RUN dotnet restore "SolarGateway_PrometheusProxy.csproj"
COPY ./API .
RUN dotnet publish "SolarGateway_PrometheusProxy.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
RUN apt-get update && apt-get install curl -y
WORKDIR /app
EXPOSE 80
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SolarGateway_PrometheusProxy.dll"]
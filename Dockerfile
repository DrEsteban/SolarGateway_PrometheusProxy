FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:8.0 AS build

ARG TARGETARCH
ARG BUILDPLATFORM

WORKDIR /src
COPY ./API/SolarGateway_PrometheusProxy.csproj ./
RUN dotnet restore -a "$TARGETARCH" "SolarGateway_PrometheusProxy.csproj"
COPY ./API .
RUN dotnet publish -a "$TARGETARCH" "SolarGateway_PrometheusProxy.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SolarGateway_PrometheusProxy.dll"]
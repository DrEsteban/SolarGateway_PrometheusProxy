FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build

ARG TARGETARCH

WORKDIR /src
COPY ./src/SolarGateway_PrometheusProxy.csproj ./
RUN dotnet restore -a "$TARGETARCH" "SolarGateway_PrometheusProxy.csproj"
COPY ./src .
RUN dotnet publish -a "$TARGETARCH" "SolarGateway_PrometheusProxy.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
RUN apk add --no-cache curl
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE $ASPNETCORE_HTTP_PORTS
HEALTHCHECK --interval=10s CMD curl --fail --head "http://localhost:$ASPNETCORE_HTTP_PORTS/health" || exit 1
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SolarGateway_PrometheusProxy.dll"]
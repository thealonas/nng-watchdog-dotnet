FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine AS base
WORKDIR /app
EXPOSE 1221

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/nightly/sdk:8.0-preview-alpine AS build
ARG TARGETARCH
WORKDIR /src
COPY ["nng-watchdog.csproj", "."]
RUN dotnet restore -a $TARGETARCH "./nng-watchdog.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "nng-watchdog.csproj" -c Release -o /app/build --no-self-contained

FROM build AS publish
RUN dotnet publish "nng-watchdog.csproj" -c Release -a $TARGETARCH -o /app/publish --no-self-contained

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "nng-watchdog.dll"]

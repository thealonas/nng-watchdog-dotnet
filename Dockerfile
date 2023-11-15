FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS base
WORKDIR /app
EXPOSE 1221

FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build
WORKDIR /src
COPY ["nng-watchdog.csproj", "./"]
RUN dotnet restore "nng-watchdog.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "nng-watchdog.csproj" -c Release -o /app/build --no-self-contained

FROM build AS publish
RUN dotnet publish "nng-watchdog.csproj" -c Release -o /app/publish --no-self-contained

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "nng-watchdog.dll"]

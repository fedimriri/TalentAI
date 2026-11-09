FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base

USER root

RUN apt-get update \
    && apt-get install -y --no-install-recommends --only-upgrade \
       apt libapt-pkg6.0t64 \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["TalentAI.csproj", "./"]
RUN dotnet restore "TalentAI.csproj"
COPY . .
RUN dotnet build "TalentAI.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "TalentAI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY --from=build /src/appsettings.container.json ./appsettings.json
RUN mkdir -p /app/wwwroot/uploads && chown "$APP_UID:$APP_UID" /app/wwwroot/uploads
USER $APP_UID
ENTRYPOINT ["dotnet", "TalentAI.dll"]

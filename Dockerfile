# Built by .github/workflows/deploy.yml and pushed to Artifact Registry.
#
# A JOB image: its default command runs the Dapper console job (create the table if
# missing, write rows, read them back), then exits 0 (non-zero on any error). It serves no HTTP.
#   - two stages, as Microsoft's container docs teach: publish on the SDK image, run on the
#     slim .NET runtime image;
#   - SQLite by default, in /app/data (SQLITE_PATH); PostgreSQL when DATABASE_URL is set;
#   - runs as the image's built-in non-root `app` user ($APP_UID).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/App/App.csproj src/App/
RUN dotnet restore src/App/App.csproj
COPY src/ src/
RUN dotnet publish src/App/App.csproj -c Release --no-restore -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
ARG BUILD_ID=""
WORKDIR /app
ENV BUILD_ID=$BUILD_ID DOTNET_CLI_TELEMETRY_OPTOUT=1
COPY --from=build /out .
RUN mkdir -p /app/data && chown $APP_UID /app/data
USER $APP_UID
CMD ["dotnet", "App.dll"]

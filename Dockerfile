# ---- build & test ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Trackpense.sln ./
COPY src/Trackpense/Trackpense.csproj src/Trackpense/
COPY src/Trackpense.Core/Trackpense.Core.csproj src/Trackpense.Core/
COPY tests/Trackpense.Tests/Trackpense.Tests.csproj tests/Trackpense.Tests/
RUN dotnet restore

COPY . .
RUN dotnet test --no-restore --configuration Release
RUN dotnet publish src/Trackpense -c Release -o /app --no-restore

# ---- runtime ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ConnectionStrings__Default="Data Source=/data/trackpense.db"
# The runtime image's non-root "app" user must own the data directory so SQLite can create its file.
RUN mkdir -p /data && chown app:app /data
VOLUME /data
EXPOSE 8080

COPY --from=build /app .
USER app
ENTRYPOINT ["dotnet", "Trackpense.dll"]

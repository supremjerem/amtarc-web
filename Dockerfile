# Build context is the repository root.
#
# Three stages, so neither Node nor the .NET SDK reaches the runtime image:
#   1. assets  — Tailwind + esbuild produce wwwroot/css/site.css and wwwroot/js/site.js
#   2. build   — dotnet publish, plus an EF migrations bundle
#   3. runtime — aspnet only, running as a non-root user

# ---------------------------------------------------------------------------
# 1. Front-end assets
# ---------------------------------------------------------------------------
FROM node:22-alpine AS assets
WORKDIR /src

# package files first: this layer is reused whenever only source changed.
COPY src/Amtarc.Web/package.json src/Amtarc.Web/package-lock.json ./
RUN npm ci --no-audit --no-fund

# Tailwind scans the Razor views and the TypeScript for class names, so both have to be here.
COPY src/Amtarc.Web/Styles ./Styles
COPY src/Amtarc.Web/Scripts ./Scripts
COPY src/Amtarc.Web/Pages ./Pages
COPY src/Amtarc.Web/tsconfig.json ./tsconfig.json

RUN npm run build

# ---------------------------------------------------------------------------
# 2. Publish
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
ENV BuildFrontendAssets=false

# Restore against the lock files alone, so a source-only change reuses the package layer.
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/Amtarc.Web/Amtarc.Web.csproj src/Amtarc.Web/
COPY src/Amtarc.Web/packages.lock.json src/Amtarc.Web/
RUN dotnet restore src/Amtarc.Web/Amtarc.Web.csproj --locked-mode

COPY src/ src/

# The assets are already built; BuildFrontendAssets=false keeps MSBuild from looking for Node,
# which does not exist in this stage.
COPY --from=assets /src/wwwroot/css/site.css src/Amtarc.Web/wwwroot/css/site.css
COPY --from=assets /src/wwwroot/js/site.js src/Amtarc.Web/wwwroot/js/site.js

RUN dotnet publish src/Amtarc.Web/Amtarc.Web.csproj -c Release -o /app --no-restore

# The migrations bundle: a standalone migrator, so the runtime image needs neither the SDK nor
# the dotnet-ef tool. Framework-dependent, because the runtime image already carries the runtime.
RUN dotnet tool install --global dotnet-ef --version 10.0.* \
 && /root/.dotnet/tools/dotnet-ef migrations bundle \
      --project src/Amtarc.Web/Amtarc.Web.csproj \
      --configuration Release \
      --force -o /app/efbundle

# ---------------------------------------------------------------------------
# 3. Runtime
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true \
    Upload__Directory=/app/uploads \
    Database__MigrateOnStartup=false \
    DataProtection__KeyRing=/app/keys

RUN apt-get update \
 && apt-get install --no-install-recommends -y curl \
 && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY --from=build /app ./
COPY docker/entrypoint.sh /usr/local/bin/entrypoint.sh

# The uploads and key-ring volumes mount here; both have to exist and be writable by `app`.
RUN chmod +x /usr/local/bin/entrypoint.sh \
 && mkdir -p /app/uploads /app/keys \
 && chown -R app:app /app

USER app
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD ["curl", "-fsS", "http://localhost:8080/healthz"]

ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]

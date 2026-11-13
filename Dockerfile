FROM node:22-alpine AS web
WORKDIR /app/src/GetYourGigs.Web
COPY src/GetYourGigs.Web/package*.json ./
RUN npm ci
COPY src/GetYourGigs.Web/ ./
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app
COPY global.json Directory.Build.props ./
COPY src/GetYourGigs.Contracts/ src/GetYourGigs.Contracts/
COPY src/GetYourGigs.Api/ src/GetYourGigs.Api/
RUN dotnet publish src/GetYourGigs.Api/GetYourGigs.Api.csproj -c Release -o /out
COPY --from=web /app/src/GetYourGigs.Api/wwwroot /out/wwwroot

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out/ ./
COPY --chmod=755 scripts/start-container.sh /app/start-container.sh
ENV ASPNETCORE_HTTP_PORTS=8080
RUN mkdir -p /app/.data/uploads && chown -R app:app /app/.data
USER app
EXPOSE 8080
ENTRYPOINT ["/app/start-container.sh"]

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY MagdyPOS.csproj .
RUN dotnet restore MagdyPOS.csproj
COPY . .
RUN dotnet publish MagdyPOS.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
RUN mkdir -p /data && chown app:app /data
USER app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ConnectionStrings__DefaultConnection="Data Source=/data/magdypos.db"
VOLUME /data
EXPOSE 8080
ENTRYPOINT ["dotnet", "MagdyPOS.dll"]

# build
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY DeviceApi.csproj ./
RUN dotnet restore DeviceApi.csproj
COPY . .
RUN dotnet publish DeviceApi.csproj -c Release -o /app/publish

# runtime
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet","DeviceApi.dll"]
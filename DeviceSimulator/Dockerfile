FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY DeviceSimulator.csproj ./
RUN dotnet restore DeviceSimulator.csproj
COPY . .
RUN dotnet publish DeviceSimulator.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:7.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet","DeviceSimulator.dll"] 
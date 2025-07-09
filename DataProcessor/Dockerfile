# build stage
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

COPY DataProcessor.csproj ./
RUN dotnet restore "DataProcessor.csproj"

COPY . ./
RUN dotnet publish "DataProcessor.csproj" -c Release -o /app/publish

# runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DataProcessor.dll"] 
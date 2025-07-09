# build stage
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

COPY ApiGateway.csproj ./
RUN dotnet restore "ApiGateway.csproj"

COPY . ./
RUN dotnet publish "ApiGateway.csproj" -c Release -o /app/publish

# runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "ApiGateway.dll"] 
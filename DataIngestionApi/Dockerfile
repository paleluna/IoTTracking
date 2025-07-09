# build stage
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src

# copy csproj and restore dependencies
COPY DataIngestionApi.csproj ./
RUN dotnet restore "DataIngestionApi.csproj"

# copy rest of the code and publish
COPY . ./
RUN dotnet publish "DataIngestionApi.csproj" -c Release -o /app/publish

# runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 80
ENTRYPOINT ["dotnet", "DataIngestionApi.dll"] 
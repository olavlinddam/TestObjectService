# Use the .NET runtime as the base image
FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

# Use the .NET SDK image to build the application
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

WORKDIR /src
COPY ["TestObjectService/TestObjectService.csproj", "TestObjectService/"]
RUN dotnet restore "TestObjectService/TestObjectService.csproj"
COPY . .
WORKDIR "/src/TestObjectService"
RUN dotnet build "TestObjectService.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "TestObjectService.csproj" -c Release -o /app/publish /p:UseAppHost=true

# Final stage/image
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS final

WORKDIR /app
COPY --from=publish /app/publish .

# Ensure the tool path is in the environment for runtime
ENV PATH="$PATH:/root/.dotnet/tools"

# Set the command to run your application
CMD ["dotnet", "TestObjectService.dll"]





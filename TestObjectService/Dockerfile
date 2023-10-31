FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["TestObjectService/TestObjectService.csproj", "TestObjectService/"]
RUN dotnet restore "TestObjectService/TestObjectService.csproj"
COPY . .
WORKDIR "/src/TestObjectService"
RUN dotnet build "TestObjectService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TestObjectService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY TestObjectService/migrations.sql /app/migrations.sql
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TestObjectService.dll"]

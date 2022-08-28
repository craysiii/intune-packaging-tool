FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "IntunePackagingTool.csproj"
RUN dotnet build "IntunePackagingTool.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "IntunePackagingTool.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "IntunePackagingTool.dll"]
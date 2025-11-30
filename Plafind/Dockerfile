FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Plafind.csproj", "./"]
RUN dotnet restore "Plafind.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "Plafind.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Plafind.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Plafind.dll"]


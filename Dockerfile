FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["reverse-proxy-example/reverse-proxy-example.csproj", "reverse-proxy-example/"]
RUN dotnet restore "reverse-proxy-example.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "reverse-proxy-example.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "reverse-proxy-example.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "reverse-proxy-example.dll"]

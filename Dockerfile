FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:80
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["GotenbergReverseProxy/GotenbergReverseProxy.csproj", "GotenbergReverseProxy/"]
RUN dotnet restore "GotenbergReverseProxy/GotenbergReverseProxy.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "GotenbergReverseProxy/GotenbergReverseProxy.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "GotenbergReverseProxy/GotenbergReverseProxy.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "GotenbergReverseProxy.dll"]

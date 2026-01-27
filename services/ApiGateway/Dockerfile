
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["services/ApiGateway/ApiGateway.csproj", "services/ApiGateway/"]
RUN dotnet restore "services/ApiGateway/ApiGateway.csproj"
COPY . .
WORKDIR "/src/services/ApiGateway"
RUN dotnet publish "ApiGateway.csproj" -c Release -o /app/publish /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "ApiGateway.dll"]
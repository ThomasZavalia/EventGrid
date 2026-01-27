
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src


COPY ["services/BookingService/src/API/API.csproj", "services/BookingService/src/API/"]
COPY ["services/BookingService/src/Application/Application.csproj", "services/BookingService/src/Application/"]
COPY ["services/BookingService/src/Domain/Domain.csproj", "services/BookingService/src/Domain/"]
COPY ["services/BookingService/src/Infrastructure/Infrastructure.csproj", "services/BookingService/src/Infrastructure/"]


RUN dotnet restore "services/BookingService/src/API/API.csproj"


COPY ["services/BookingService/", "services/BookingService/"]
COPY ["protos/", "protos/"]

WORKDIR "/src/services/BookingService/src/API"
RUN dotnet publish "API.csproj" -c Release -o /app/publish /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 5000
EXPOSE 5001


COPY --from=build /app/publish .

# Entrada
ENTRYPOINT ["dotnet", "API.dll"]
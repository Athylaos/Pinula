FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Pinula.API/Pinula.API.csproj", "Pinula.API/"]
COPY ["Pinula.Shared/Pinula.Shared.csproj", "Pinula.Shared/"]
RUN dotnet restore "Pinula.API/Pinula.API.csproj"

COPY . .
WORKDIR "/src/Pinula.API"
RUN dotnet publish "Pinula.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Pinula.API.dll"]
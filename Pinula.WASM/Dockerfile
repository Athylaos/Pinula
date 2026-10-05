FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Pinula.Web/Pinula.Web.csproj", "Pinula.Web/"]
COPY ["Pinula.Shared/Pinula.Shared.csproj", "Pinula.Shared/"]
RUN dotnet restore "Pinula.Web/Pinula.Web.csproj"

COPY . .
WORKDIR "/src/Pinula.Web"
RUN dotnet publish "Pinula.Web.csproj" -c Release -o /app/publish

FROM nginx:alpine AS final
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY Pinula.Web/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Pinula.WASM/Pinula.WASM.csproj", "Pinula.WASM/"]
COPY ["Pinula.Shared/Pinula.Shared.csproj", "Pinula.Shared/"]
RUN dotnet restore "Pinula.WASM/Pinula.WASM.csproj"

COPY . .
WORKDIR "/src/Pinula.WASM"
RUN dotnet publish "Pinula.WASM.csproj" -c Release -o /app/publish

FROM nginx:alpine AS final
COPY --from=build /app/publish/wwwroot /usr/share/nginx/html
COPY Pinula.WASM/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
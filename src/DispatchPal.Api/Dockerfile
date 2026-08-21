FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /source

COPY src/DispatchPal.Contracts/DispatchPal.Contracts.csproj \
     src/DispatchPal.Contracts/

COPY src/DispatchPal.Api/DispatchPal.Api.csproj \
     src/DispatchPal.Api/

RUN dotnet restore src/DispatchPal.Api/DispatchPal.Api.csproj

COPY . .

RUN dotnet publish \
    src/DispatchPal.Api/DispatchPal.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
       curl \
       libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "DispatchPal.Api.dll"]
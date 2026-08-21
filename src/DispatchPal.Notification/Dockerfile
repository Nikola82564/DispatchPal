FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /source

COPY src/DispatchPal.Contracts/DispatchPal.Contracts.csproj \
     src/DispatchPal.Contracts/

COPY src/DispatchPal.Notification/DispatchPal.Notification.csproj \
     src/DispatchPal.Notification/

RUN dotnet restore \
    src/DispatchPal.Notification/DispatchPal.Notification.csproj

COPY . .

RUN dotnet publish \
    src/DispatchPal.Notification/DispatchPal.Notification.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish


FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "DispatchPal.Notification.dll"]
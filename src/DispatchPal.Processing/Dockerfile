FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /source

COPY src/DispatchPal.Contracts/DispatchPal.Contracts.csproj \
     src/DispatchPal.Contracts/

COPY src/DispatchPal.Processing/DispatchPal.Processing.csproj \
     src/DispatchPal.Processing/

RUN dotnet restore \
    src/DispatchPal.Processing/DispatchPal.Processing.csproj

COPY . .

RUN dotnet publish \
    src/DispatchPal.Processing/DispatchPal.Processing.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish


FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "DispatchPal.Processing.dll"]
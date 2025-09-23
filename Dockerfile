FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore EventRegistrator.sln
RUN dotnet publish EventRegistrator/EventRegistrator.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS debug
WORKDIR /app
COPY --from=build /app/publish .

# Установить dotnet-dump
RUN dotnet tool install -g dotnet-dump
ENV PATH="${PATH}:/root/.dotnet/tools"

ENTRYPOINT ["dotnet", "EventRegistrator.dll"]
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY FileSearchService.sln .
COPY FileSearchService.Api/FileSearchService.Api.csproj FileSearchService.Api/
COPY FileSearchService.Application/FileSearchService.Application.csproj FileSearchService.Application/
COPY FileSearchService.Infrastructure/FileSearchService.Infrastructure.csproj FileSearchService.Infrastructure/
COPY FileSearchService.Domain/FileSearchService.Domain.csproj FileSearchService.Domain/

RUN dotnet restore

COPY . .

WORKDIR /src/FileSearchService.Api
RUN dotnet publish -c Release -o /app/publish 

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
COPY FileSearchService.Api/data/ /app/data/
RUN mkdir -p logs

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "FileSearchService.Api.dll"]
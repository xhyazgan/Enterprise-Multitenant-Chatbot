FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ChatBot.ServiceDefaults/ChatBot.ServiceDefaults.csproj ChatBot.ServiceDefaults/
COPY ChatBot.Gateway/ChatBot.Gateway.csproj ChatBot.Gateway/
RUN dotnet restore ChatBot.Gateway/ChatBot.Gateway.csproj
COPY ChatBot.ServiceDefaults/ ChatBot.ServiceDefaults/
COPY ChatBot.Gateway/ ChatBot.Gateway/
WORKDIR /src/ChatBot.Gateway
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ChatBot.Gateway.dll"]

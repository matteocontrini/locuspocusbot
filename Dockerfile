FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /app

# Copy sln and csproj and try to restore dependencies
COPY *.sln .
COPY LocusPocusBot/*.csproj ./LocusPocusBot/
COPY LocusPocusBot.Rooms/*.csproj ./LocusPocusBot.Rooms/
RUN dotnet restore

# Copy all srcs and compile
COPY . .
WORKDIR /app/LocusPocusBot
RUN dotnet build

FROM build AS publish
WORKDIR /app/LocusPocusBot
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS runtime
WORKDIR /app
COPY --from=publish /app/LocusPocusBot/out ./
ENTRYPOINT ["dotnet", "LocusPocusBot.dll"]

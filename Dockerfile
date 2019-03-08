FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /app

# Copy sln and csproj and try to restore dependencies
COPY *.sln .
COPY LocusPocusBot/*.csproj ./LocusPocusBot/
COPY LocusPocusBot.Rooms/*.csproj ./LocusPocusBot.Rooms/
COPY CustomConsoleLogger/*.csproj ./CustomConsoleLogger/
RUN dotnet restore

# Copy all srcs and compile
COPY . .
WORKDIR /app/LocusPocusBot
RUN dotnet build

FROM build AS publish
WORKDIR /app/LocusPocusBot
RUN dotnet publish -c Release -o out

FROM microsoft/dotnet:2.2-runtime AS runtime
WORKDIR /app
COPY --from=publish /app/LocusPocusBot/out ./
ENTRYPOINT ["dotnet", "LocusPocusBot.dll"]

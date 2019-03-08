# LocusPocusBot 2.0 [![Build Status](https://travis-ci.org/matteocontrini/locuspocusbot.svg?branch=master)](https://travis-ci.org/matteocontrini/locuspocusbot)

## Previous versions

A previous version of this bot was developed in Go and has been available for about a year and a half. That version is now available [in another branch](https://github.com/matteocontrini/locuspocusbot/tree/go).

## Requirements

MySQL is required for the bot to work. Data about users and groups will be stored in the database. The application is tested to work with **MySQL 8.0.15.**

## Configuration

Configuration of the application is done through the `appsettings.json` file read from the current working directory at startup.

Examples for [development](https://github.com/matteocontrini/locuspocusbot/blob/dotnet/LocusPocusBot/appsettings.example.development.json) and [production](https://github.com/matteocontrini/locuspocusbot/blob/dotnet/LocusPocusBot/appsettings.example.json) environments are available.

## Running for development

Make sure that you have the .NET Core 2.2 SDK and runtime installed on your system.

### Visual Studio

Make sure that the `LocusPocusBot/bin/Debug/netcoreapp2.2` directory contains the `appsettings.json` file.

Run with the nice green button.

### dotnet CLI

Make sure that the `LocusPocusBot` directory contains the `appsettings.json` file.

Run with the dotnet CLI by executing:

```sh
cd LocusPocusBot
dotnet run
```

### Docker Compose

A development Docker Compose file would look like this:

```yaml
version: '3'

services:
  locuspocusbot:
    container_name: 'locuspocusbot'
    build: .
  network_mode: 'host'
  volumes:
    - ./LocusPocusBot/appsettings.json:/app/appsettings.json
```

This time make sure that the configuration file lies at `LocusPocusBot/appsettings.json`.

Now run this command in the repository directory:

```sh
docker-compose -f docker-compose.yml up --build
```

## Running in production

A basic Docker Compose file for production looks like this:

```yaml
version: '3'

services:
  locuspocusbot:
    container_name: 'locuspocusbot'
    image: 'matteocontrini/locuspocusbot'
    restart: unless-stopped
    network_mode: 'host'
    volumes:
      - ./appsettings.json:/app/appsettings.json
```
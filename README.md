# Zcoin Tokens Manager

This is a service for provide rich API for manage tokens on Zcoin Exodus Protocol.

## Requirements

- .NET Core 2.2

## Build

```sh
git submodule init
```

```sh
git submodule update --recursive
```

```sh
dotnet build src/Ztm.sln
```

## Start Required Services

You need to install [Docker Compose](https://docs.docker.com/compose/) first then run:

```sh
docker-compose up -d
```

## Migrate Database Schemas

Change directory to `src/Ztm.Data.Entity.Postgres` then run:

```sh
ZTM_MAIN_DATABASE="Host=127.0.0.1;Database=ztm;Username=ztm" dotnet ef database update
```

## Start Web API

```sh
dotnet run -p src/Ztm.WebApi
```

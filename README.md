# Zcoin Tokens Manager
[![Total alerts](https://img.shields.io/lgtm/alerts/g/zcoinofficial/ztm.svg?logo=lgtm&logoWidth=18)](https://lgtm.com/projects/g/zcoinofficial/ztm/alerts/) [![Language grade: C#](https://img.shields.io/lgtm/grade/csharp/g/zcoinofficial/ztm.svg?logo=lgtm&logoWidth=18)](https://lgtm.com/projects/g/zcoinofficial/ztm/context:csharp)

This is a service for provide rich API for manage tokens on Zcoin Exodus Protocol.

## Requirements

- .NET Core 2.2

## Build

```sh
dotnet build src/Ztm.sln
```

## Start Required Services

You need to install [Docker Compose](https://docs.docker.com/compose/) first then run:

```sh
docker-compose up -d
```

## Create Database

Enter database shell:

```sh
docker exec -it -u postgres ztm-db-main sh
```

Create a new role to be the owner of database:

```sh
createuser ztm
```

Then create a database:

```sh
createdb -O ztm ztm
```

## Migrate Database Schemas

Change directory to `src/Ztm.Data.Entity.Postgres` then run:

```sh
ZTM_MAIN_DATABASE="Host=127.0.0.1;Database=ztm;Username=ztm" dotnet ef database update
```

## Start Zcoin Daemon

Grab latest stable Zcoin binary from [here](https://github.com/zcoinofficial/zcoin/releases), extract or install it,
then start Zcoin daemon:

```sh
./zcoind -testnet -printtoconsole -rpcuser=zcoin -rpcpassword=zcoin -zmqpubhashblock=tcp://127.0.0.1:28332
```

## Start Web API

```sh
dotnet run -p src/Ztm.WebApi
```

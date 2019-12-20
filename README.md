# Zcoin Tokens Manager
![Continuous Integration](https://github.com/zcoinofficial/ztm/workflows/Continuous%20Integration/badge.svg)
[![codecov](https://codecov.io/gh/zcoinofficial/ztm/branch/master/graph/badge.svg)](https://codecov.io/gh/zcoinofficial/ztm)
[![Total alerts](https://img.shields.io/lgtm/alerts/g/zcoinofficial/ztm.svg?logo=lgtm&logoWidth=18)](https://lgtm.com/projects/g/zcoinofficial/ztm/alerts/)
[![Language grade: C#](https://img.shields.io/lgtm/grade/csharp/g/zcoinofficial/ztm.svg?logo=lgtm&logoWidth=18)](https://lgtm.com/projects/g/zcoinofficial/ztm/context:csharp)

This is a service to provide rich API for manage tokens on Zcoin Exodus Protocol.

## Development

### Requirements

- .NET Core 2.1

### Build

```sh
dotnet build src/Ztm.sln
```

### Start Required Services

You need to install [Docker Compose](https://docs.docker.com/compose/) first then run:

```sh
docker-compose up -d
```

### Migrate Database Schemas

Change directory to `src/Ztm.Data.Entity.Postgres` then run:

```sh
ZTM_MAIN_DATABASE="Host=127.0.0.1;Database=postgres;Username=postgres" dotnet ef database update
```

### Start Web API

```sh
dotnet run -p src/Ztm.WebApi
```

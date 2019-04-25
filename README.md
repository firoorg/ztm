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

## Start Web API

```sh
dotnet run -p src/Ztm.WebApi
```

FROM mcr.microsoft.com/dotnet/core/sdk:2.2

# parent image default user is root, we don't want that

RUN useradd -m -s /bin/bash -U ztm-builder
USER ztm-builder

# This Dockerfile is designed to build inside a published directory produced for linux-musl-x64.
FROM mcr.microsoft.com/dotnet/core/runtime-deps:2.1-alpine

COPY * /opt/ztm/
RUN chmod +x /opt/ztm/Ztm.WebApi

USER 1000:1000
WORKDIR /opt/ztm
ENV ASPNETCORE_URLS=

ENTRYPOINT [ "/opt/ztm/Ztm.WebApi" ]

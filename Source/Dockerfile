FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 9001

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src

COPY . ./

FROM build AS publish

COPY ./CDR.DataRecipient.Web/. /app/CDR.DataRecipient.Web
COPY ./CDR.DataRecipient.Repository.SQLite/. /app/CDR.DataRecipient.Repository.SQLite
COPY ./CDR.DataRecipient/. /app/CDR.DataRecipient
COPY ./CDR.DataRecipient.SDK/. /app/CDR.DataRecipient.SDK

WORKDIR /app/CDR.DataRecipient.Web
RUN dotnet publish -c Release -o /app/publish/web

COPY supervisord.conf /app/publish/supervisord.conf

FROM base AS final
WORKDIR /app

COPY --from=publish /app/publish/supervisord.conf .
COPY --from=publish /app/publish/web ./web

RUN apt-get update && apt-get install -y supervisor

RUN apt-get update && apt-get install -y sudo

RUN sudo cp ./web/Certificates/ca.crt /usr/local/share/ca-certificates/ca.crt
RUN sudo update-ca-certificates

ENV ASPNETCORE_URLS=https://+:9001
ENTRYPOINT ["/usr/bin/supervisord", "-c", "/app/supervisord.conf"]
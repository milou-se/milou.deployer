FROM mcr.microsoft.com/dotnet/framework/sdk:4.8-windowsservercore-ltsc2019

COPY install-common.ps1 c:/
RUN .\install-common.ps1

COPY install-dotnetcore.ps1 c:/
RUN .\install-dotnetcore.ps1

VOLUME ["c:/config", "C:/logs"]

EXPOSE 34343

WORKDIR /app

COPY ./data/ C:/app

ENTRYPOINT ["cmd"]

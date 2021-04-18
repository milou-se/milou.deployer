FROM mcr.microsoft.com/dotnet/framework/sdk:4.8-windowsservercore-ltsc2019

COPY install-common.ps1 c:/
RUN .\install-common.ps1

COPY install-dotnetcore.ps1 c:/
RUN .\install-dotnetcore.ps1

ENV urn:arbor:app:web:settings:json-file=c:/config/settings.json
ENV urn:arbor:app:web:logging:serilog:default:rollingLogFilePath=C:/logs/deploy-agent.log
ENV urn:arbor:app:web:logging:serilog:default:rollingLogFilePathEnabled=true
ENV urn:arbor:app:web:logging:serilog:default:consoleEnabled=false
ENV urn:arbor:app:web:logging:serilog:default:seqEnabled=false
ENV urn:arbor:app:web:logging:serilog:default:seqUrl=
ENV urn:arbor:app:web:logging:startup:serilog:startup-log-file-path=C:/logs/startup.log
ENV urn:arbor:app:web:logging:startup:serilog:startup-log-file:enabled=true

VOLUME ["c:/config", "C:/logs"]

WORKDIR /app

COPY ./data/ C:/app

ENTRYPOINT ["Milou.Deployer.Web.Agent.Host.exe"]

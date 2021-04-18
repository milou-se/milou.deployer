@ECHO OFF
SET BuildTemp=%~dp0AgentTemp

IF EXIST "%BuildTemp%" (
  ECHO "Removing temp directory '%BuildTemp%'"
  RD "%BuildTemp%/" /s /q
)

ECHO "Creating directory '%BuildTemp%'"
mkdir "%BuildTemp%"

CD "%~dp0"

CD ..
CD ..

SET Artifacts=Artifacts\packages\binary\Milou.Deployer.Web.Agent.Host.*\tools\net5.0
ECHO Copying files from %Artifacts% to %BuildTemp%
xcopy "%Artifacts%" "%BuildTemp%\data\" /e /s

CD "%~dp0"

COPY build\agent\*.* "%BuildTemp%\"
COPY build\*.* "%BuildTemp%\"

cd "%BuildTemp%"

ECHO "Running docker build in '%CD%'"

CALL docker build -t %DOCKER_BASE_TAG%deployer-agent:latest .

ECHO "Getting back to script directory"

CD "%~dp0"
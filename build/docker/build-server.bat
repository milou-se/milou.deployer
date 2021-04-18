@ECHO OFF
SET BuildTemp=%~dp0ServerTemp

IF EXIST "%BuildTemp%" (
  ECHO "Removing temp directory '%BuildTemp%'"
  RD "%BuildTemp%/" /s /q
)

ECHO "Creating directory '%BuildTemp%'"
mkdir "%BuildTemp%"

CD "%~dp0"

CD ..
CD ..

SET Artifacts=Artifacts\Websites\Milou.Deployer.Web.IisHost\AnyCPU\debug
ECHO Copying files from %Artifacts% to %BuildTemp%
xcopy "%Artifacts%" "%BuildTemp%\data\" /e /s

CD "%~dp0"

COPY build\server\*.* "%BuildTemp%\"
COPY build\*.* "%BuildTemp%\"

cd "%BuildTemp%"

ECHO "Running docker build in '%CD%'"

CALL docker build -t %DOCKER_BASE_TAG%deployer-server:latest .

ECHO "Getting back to script directory"

CD "%~dp0"
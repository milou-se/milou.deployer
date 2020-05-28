@ECHO OFF

CALL %~dp0build-agent.bat

IF "%ERRORLEVEL%" NEQ "0" (
  EXIT /B %ERRORLEVEL%
)

CALL %~dp0build-server.bat

IF "%ERRORLEVEL%" NEQ "0" (
  EXIT /B %ERRORLEVEL%
)

EXIT /B 0
@ECHO OFF

CALL build-agent.bat

IF "%ERRORLEVEL%" NEQ "0" (
  EXIT /B %ERRORLEVEL%
)

CALL build-server.bat

IF "%ERRORLEVEL%" NEQ "0" (
  EXIT /B %ERRORLEVEL%
)

EXIT /B 0
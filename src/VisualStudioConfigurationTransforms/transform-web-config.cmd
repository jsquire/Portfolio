@echo off

:: Run the transform for web.config.
  set PROJECTPATH=%1%
  set CONFIGNAME=web
  set TARGETENV=%2%
  
  call %0\..\transform-config.cmd %PROJECTPATH% %CONFIGNAME% %CONFIGNAME% %CONFIGNAME% %TARGETENV%

:: Perform any final actions.
:EXIT
  exit /B 0
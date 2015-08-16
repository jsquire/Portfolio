@echo off

:: Force the current directory to be the exeutable directory.
  set CURRENTDIR=%CD%
  cd %0\..\

:: Declare script variables 
  set TARGETDIR=%1%
  set BASECONFIG=%2%
  set SOURCECONFIG=%3%
  set DESTINATIONCONFIG=%4%
  set TARGETENV=%5%
  set BASEFILENAME=%TARGETDIR%%BASECONFIG%
  set SOURCEFILENAME=%TARGETDIR%%SOURCECONFIG%
  set DESTINATIONFILENAME=%TARGETDIR%%DESTINATIONCONFIG%
  set TEMPFILENAME=%TARGETDIR%temp.%DESTINATIONCONFIG%
  set BACKUPFILENAME=%TARGETDIR%%SOURCECONFIG%.Backup.config
  
  if "%TARGETENV%" == "" (
    set TARGETENV=Release
  )   
 
  set BUILDFILE=ConfigurationTransform.build
  set MSBUILDTASKLIBRARY=Microsoft.Web.Publishing.Tasks.dll

:: Determine the correct framework directory.
  set FRAMEWORKVER=v4.0.30319
  set FRAMEWORKPATH=%windir%\Microsoft.NET\Framework64\%FRAMEWORKVER%
  set FRAMEWORKBITS=64
  
  if not exist "%FRAMEWORKPATH%" (
    set FRAMEWORKPATH=%windir%\Microsoft.NET\Framework\%FRAMEWORKVER%
    set FRAMEWORKBITS="32"
  )

:: Verify the build file.
  if not exist "%BUILDFILE%" (
    echo .
    echo Cannot find the configuration transform build file at: %MSBUILDTASKLIBRARY%.
    echo Transform aborted.
    goto EXIT
  )

:: Verify the MSBuild library.
  if not exist "%MSBUILDTASKLIBRARY%" (
    echo .
    echo Cannot find the MSBuild task library at: %MSBUILDTASKLIBRARY%.
    echo Transform aborted.
    goto EXIT
  )
    
:: Verify framework location.
  if not exist "%FRAMEWORKPATH%" (
    echo .
    echo Cannot find the .NET Framework v4.0 directory on your system.
    echo Transform aborted.
    goto EXIT
  )
  
:: Backup the config file.
  if exist "%BACKUPFILENAME%.config" (
    del "%BACKUPFILENAME%.config"
  )

  echo Backing up pre-transform config to %BACKUPFILENAME%.config.
  copy "%SOURCEFILENAME%.config" "%BACKUPFILENAME%"

:: Transform the configuration.
  echo Performing web.config transform for build configuration "%TARGETENV%" using .NET Framework %FRAMEWORKVER% (%FRAMEWORKBITS%-bit)
  %FRAMEWORKPATH%\msbuild.exe %BUILDFILE% /t:GenerateConfigs /property:SourceFilename=%SOURCEFILENAME%,BaseFileName=%BASEFILENAME%,DestinationFilename=%TEMPFILENAME%,BuildConfigurationName=%TARGETENV% /verbosity:quiet
  move "%TEMPFILENAME%.config" "%DESTINATIONFILENAME%.config"
   
:: Perform any final actions.
:EXIT
  cd %CURRENTDIR%  
  echo Transform completed.
  echo.
  exit /B 0
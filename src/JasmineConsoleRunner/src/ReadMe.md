# Source Code #

### Overview ###

Developed in early 2015, the source included herein is broken into two separate projects - the script used to bootstrap and PhantomJS and run Jasmine tests, and the C# console application used to wrap that interaction and colorize the output.  

Initially, instead of the C# console application, the intent was to use a simple PowerShell script to capture and colorize the output, however due to the model used by PowerShell for handling asynchronous events it was unable to reliably capture the stdout stream text.  The prototypes for these scripts can be found in the [PowerShell](../../scripts/powershell, "PowerShell") section of the portfolio.

### Structure ###

* **JasmineConsoleRunner**
  <br />_This project is a C# console application that is intended to be used as the front-end for running Jasmine tests from the console.  It will accept arguments focused on locating PhantomJS and the Jasmine test assets and then launch PhantomJS as a child process to capture its output.  While inspecting the child stdout stream, it will interpret any color control sequences and appropriately colorize the output when echoing to its own stdout._  
  <br />_As this project is intended for personal use, no external build scripts have been included; building for distribution is done using Visual Studio and selecting the "Distribution" build configuration._
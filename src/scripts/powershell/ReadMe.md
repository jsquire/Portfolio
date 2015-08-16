# PowerShell #

### Overview ###

Developed in 2015, these scripts were used as prototypes for the [Jasmine Console Runner](../../JasmineConsoleRunnder "Jasmine Console Runner") project's console colorizer.

### Items ###

* **phantomjs-launcher-pure.ps1**
  <br />_This script was the first attempt at capturing PhantomJS console output for the purposes of colorizing it.  Unfortunately, due to the approach used by PowerShell for asynchronous event handling, it was unable to reliably capture the stdout output._
  
* **phantomjs-launcher-hybrid.ps1**
  <br />_This script was the second attempt at capturing PhantomJS console output for the purposes of colorizing it.  The PowerShell script emits a dynamic C# class to handle the asynchronous events, in an attempt to work around PowerShell's deficiencies.  The goal was to keep the majority of functionality in this script for simplicity while only delegating event handling.  Unfortunately, because of the PowerShell interop, this approach, too, fell under PowerShell's model for asynchronous event handling and a pure C# solution was necessary._
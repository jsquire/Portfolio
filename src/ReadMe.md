# Source Code #

### Overview ###

Included in this section are code samples, organized by project/problem.  Each project or problem is intended to be self-contained and stand-alone.  Some are focused on a single language, while others include implementations in multiple languages.  For the latter, these will be organized with each implementation language in a dedicated sub-folder.  Each will contain a ReadMe to provide an overview and context.   

In some cases, the samples may be a subset of functionality, where in others they may be a full and functional application.  Please remember that each project is intended to be a representation of my coding approach, style, habits, and thought process, not to illustrate large, complex, and complete systems.  

### Structure ###

* **mAdserve**
  <br />_This project extends and enhances the open source [mAdserve](http://madserve.org "mAdserve") mobile ad platform to allow for serving of rich media ads in addition to the static content-based ads that it was originally designed to deliver._
  
* **Jasmine Console Runner**
  <br />_Many javascript unit testing libraries are able to produce color console output on Unix-based systems by using the standard escape character sequence.  These sequences do not work in the Windows console, nor is there an alternative sequence to do so. This project is a wrapper around the Jasmine unit testing library which allows test output to emit a control character sequence to the stdout stream that can influence the color that it is written with in the console output._

* **Minesweeper**
  <br />_An implementation of the classic game Minesweeper in TypeScript with a web front-end._
  
* **Scripts**
  <br />_A collection of miscellaneous small scripts in various languages.  The scripts herein are each self-contained, not part of a larger overall project or ecosystem._
  
* **Visual Studio Config Transformations**
  <br />_Visual Studio web projects have a built-in ability to utilize a base web configuration file and then apply per-build transforms to allow configuration to vary between different environments.  This functionality was initially intended to be used only as part of web deployments run from within Visual Studio.  Because many projects of different types can benefit from defining configuration this way, this project is an early attempt to isolate the built-in transform functionality of Visual Studio Web Deployments and generalize it such that it could be used as part of a build, both local and server-based._
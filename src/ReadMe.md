# Source Code #

### Overview ###

Included in this section are code samples, organized by project/problem.  Each project or problem is intended to be self-contained and stand-alone.  Some are focused on a single language, while others include implementations in multiple languages.  For the latter, these will be organized with each implementation language in a dedicated sub-folder.  Each will contain a ReadMe to provide an overview and context.   

In some cases, the samples may be a subset of functionality, where in others they may be a full and functional application.  Please remember that each project is intended to be a representation of my coding approach, style, habits, and thought process, not to illustrate large, complex, and complete systems.  

### Structure ###

* **ASP.NET Core Authentication Custom Nonce Protection**
  <br />_Authored in late 2018 to illustrate a work-around using a custom formatter for the ASP.NET Core authentication library's nonce cookie, this project extends the open source [Azure Active Directory B2C sample application for ASP.NET Core](https://github.com/Azure-Samples/active-directory-b2c-dotnetcore-webapp).  The extensions have been organized in their own classes in order to help segregate the changes from the baseline sample and include an accompanying suite of unit tests._
 
* **Certificate Store**
  <br />_Authored in mid-2016 to assist with managing certificates stored in a cloud vault, this project serves as a repository for the metadata about certificates, as well as the tools to help secure and manage them.  The goal of this project is to offer a centralized reference for certificates using standardized information, and a consistent approach to managing them._
 
* **Jasmine Console Runner**
  <br />_Many javascript unit testing libraries are able to produce color console output on Unix-based systems by using the standard escape character sequence.  These sequences do not work in the Windows console, nor is there an alternative sequence to do so. This project is a wrapper around the Jasmine unit testing library which allows test output to emit a control character sequence to the stdout stream that can influence the color that it is written with in the console output._
  
* **mAdserve**
  <br />_This project extends and enhances the open source [mAdserve](http://madserve.org "mAdserve") mobile ad platform to allow for serving of rich media ads in addition to the static content-based ads that it was originally designed to deliver._

* **Order Fulfillment**
  <br />_An example of an asynchronous order processing workflow built on Microsoft Azure.  The workflow is triggered by a web hook API, which then performs background processing using WebJobs and passing messages via Service Bus.  One of the design goals was to offer an elevated level of resiliency by employing a multi-level back off, allowing for fast retries to combat transient failures and slow retries to deal with potential downstream system failures._
    
* **Particle Button**
  <br />_A collection of miscellaneous projects for the [Particle Internet Button](https://docs.particle.io/guide/tools-and-features/button/core/) IOT device.  Each project is contained in its own folder with a dedicated ReadMe._

* **Scripts**
  <br />_A collection of miscellaneous small scripts in various languages.  The scripts herein are each self-contained, not part of a larger overall project or ecosystem._
  
* **Visual Studio Config Transformations**
  <br />_Visual Studio web projects have a built-in ability to utilize a base web configuration file and then apply per-build transforms to allow configuration to vary between different environments.  This functionality was initially intended to be used only as part of web deployments run from within Visual Studio.  Because many projects of different types can benefit from defining configuration this way, this project is an early attempt to isolate the built-in transform functionality of Visual Studio Web Deployments and generalize it such that it could be used as part of a build, both local and server-based._
  

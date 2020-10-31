# Visual Studio Configuration Transforms

### Summary

In a typical environment, applications must support multiple environments, such as local, development, integration, and production.  In order to ensure that the proper configuration values are present for an application to be functional in each scenario, a new build configuration is commonly created in Visual Studio for each target environment.  Rather than building using the built-in Debug or Release configurations, developers would instead choose the configuration specific to the environment that the application was to be run in.  The use of a dedicated build configuration also makes it easier for an automated build server to have a target that is consistent with what the development team uses locally.

One of the challenges in allowing projects to target dynamic environment is managing the needed configuration changes in the way that results in the least possible duplication of common values.  In order to help manage the complexity and ensure that the settings common to all targets appear only once, configuration transforms are often employed.  Their use allows a common set of configuration values to be maintained in a single place while those values that need to be specific to a given target can be injected as the build configuration is selected within Visual Studio.

This project was developed in 2012 for use with Visual Studio 2012 and the .NET framework v4.0.  Since the time that I wrote this, many true Visual Studio add-ons and build server extensions have been developed to perform the same tasks.  While many of them are more robust and offer better integration/functionality, I still find value in this approach as it allows a single, stand-alone set of assets that can be used for local development and automated builds and can be included with the project in source control so that no coordination is needed for common Visual Studio add-ins. 

### Base Configuration and Transforms

For each application, service, or job project, there exists a base configuration file which contains the settings that are required for the application to run properly.  Some of these settings will apply across business units and environments, while some need to be specific to a unique combination.  In the base configuration, the common settings should be fully defined with the appropriate values.  Those settings that are target-specific should also be defined but are initially given empty values, which allows them to be defined where they make the most sense for ensuring that the configuration is organized and readable.  The actual values will be filled in during the build.

When a build configuration is created in Visual Studio, a target-specific configuration file is automatically generated to match.  This configuration file is intended to contain the target-specific transformations which are applied to the base configuration to fill in the placeholder values.  These transform configurations appear in Visual Studio organized underneath the base configuration, named in the pattern: Web.BuildConfiguration.config or App.BuildConfiguration.config, where "BuildConfiguration" is the name of the build configuration for a unique business unit and environment.

The transform configuration files contain only those parts of the configuration which vary for the unique target.  These values are marked using transform attributes (see syntax resource below) which instruct the build how the settings should be applied to the base configuration.  These values may be organized in whatever manner best allows for readability and maintenance of the transform configuration; they do not need to appear in the same order as the base configuration.  They transformation will ensure that they are applied to the proper items regardless of any organizational differences.

### Opting Into Transformations

By default, Visual Studio performs the transformations only when a web project is published.  In order for transformations to be applied during normal builds, both within the IDE and by the build server, the project file must be configured with a post build event that triggers the transform.  For more information on assigning build events in Visual Studio please refer to the resources below.

The handler for the post build event that enables transformations on every build is defined as a batch file under the Build folder at the root of the repository.  Projects should reference this file directly as part of the post build command line; there is no need to make a local copy of it.  In order to ensure that the post build events work for all developers, it is highly recommended that the commands be referenced by relative path.  This allows developers the freedom to use the physical working path of their choice, so long as the repository is checked out from the root and its structure unchanged.  A relative command line for the post build event of a web project would be similar to:

    call "$(ProjectDir)..\..\..\Build\Actions\tansform-web-config.cmd" $(ProjectDir) $(ConfigurationName)

In the preceding command line, pathing starts at the project directory and each "..\" token signifies moving up a level in the directory hierarchy; the project for this case lives in a directory that is three levels above the project root.  The tokens $(ProjectDir) and $(ConfigurationName) are macros that exist within the Visual Studio project build environment specifying the physical directory of the current project and its active build configuration, respectively.  These macros are available for use in build event command lines only; they are not part of the operating system or normal command-line environment.

There are two build actions available for triggering transforms - one batch file for web-based projects such as web applications and web services and one batch file for non-web applications, such as command line jobs and WPF applications.  Each of these actions takes a set of parameters which differ slightly due to the way that Visual Studio names configuration files.  The batch file names and arguments are:

    For web-based applications : transform-web-config.cmd [Project Directory] [Build Configuration Name]
    For non-web applications   : transform-config.cmd [Project Directory] [Base File Name] [Source File Name] [Destination File Name] [Build Configuration Name] 

    [Project Directory]        : The fully qualified path to the physical directory of the project.  Typically the macro $(ProjectDir).
    [Base File Name]           : The name (without extension) of the base configuration file to use as the target of the transform.  Typically, "App", indicating that the base configuration values are sourced from "App.config".
    [Source File Name]         : The name (without extension) of the configuration file to use as the transform source; the build configuration name is appended to identify the phsysical file.  Typically "App", indicating the transform source is "App.[Build Configuration Name].config".
    [Destination File Name]    : The name (without extension) of the resulting file.  Typically "$(ProjectName).exe", indicating the resulting file is "SomeProject.exe.config".
    [Build Configuration Name] : The name of the active build configuration.  Typically the macro $(ConfigurationName).

    Sample web-based project post build command line:
      call "$(ProjectDir)..\..\..\Build\Actions\transform-web-config.cmd" $(ProjectDir) $(ConfigurationName)

    Sample application project post build command line:
      call "$(ProjectDir)..\..\..\Build\Actions\transform-config.cmd" $(ProjectDir) App App $(ProjectName).exe $(ConfigurationName)

### Items

* **ConfigurationTransform.build**  
 _An MSBuild targets file extracted from the built-in web publishing targets which allows the configuration transform functionality to be included as part of a project file._
  
* **Microsoft.Web.Publishing.Tasks.dll**  
  _The library used by the built-int web publishing functionality of Visual Studio in which the MSBuild tasks for configuration transform logic are defined._
  
* **transform-config.cmd**  
  _A batch file intended to be used as part of a post-build command for a non-web project, such as a desktop application,  performs the transformation against an app.config file._
  
* **transform-web-config.cmd**  
  _A batch file intended to be used as part of a post-build command for a web project, such as a desktop application,  performs the transformation against an web.config file._
  
  
### Resources

* [Configuration Transform Syntax](http://msdn.microsoft.com/en-us/library/dd465326.aspx "Configuration Transform Syntax")
* [How to: Specify Build Events](http://msdn.microsoft.com/en-us/library/ke5z92ks(v=VS.100).aspx "How to: Specify Build Events")

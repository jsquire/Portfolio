using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Fclp;

namespace GoofyFoot.PhantomLauncher
{
  /// <summary>
  ///   The application's context for command line-related information.
  /// </summary>
  /// 
  internal class CommandLineContext
  {
    /// <summary>The configured parser to be used when evaluating the command line arguments.</summary>
    private Lazy<FluentCommandLineParser<Arguments>> parser = new Lazy<FluentCommandLineParser<Arguments>>(CommandLineContext.CreateParser, LazyThreadSafetyMode.PublicationOnly);

    /// <summary>
    ///   Gets the command line arguments that were evaluated by the <see cref="P:GoofyFoot.PhantomLauncher.CommandLineContext.ArgumentParser"/>.
    /// </summary>
    /// 
    public Arguments Args
    {
      get
      {
        return this.parser.Value.Object;
      }
    }

    /// <summary>
    ///   Gets a value indicating whether there are command line argument errors in this context.
    /// </summary>
    /// 
    /// <value><c>true</c> if this context has argument errors; otherwise, <c>false</c>.</value>
    /// 
    public bool HasArgumentErrors
    {
      get;
      private set;
    }
    
    /// <summary>
    ///   Initializes a new instance of the <see cref="T:GoofyFoot.PhantomLauncher.CommandLineContext"/> class.
    /// </summary>
    /// 
    /// <param name="args">The command line arguments passed to the application.</param>
    /// 
    public CommandLineContext(string[] args)
    {
      if (args == null)
      {
        throw new ArgumentNullException("args");
      }

      
      this.HasArgumentErrors = this.parser.Value.Parse(args).HasErrors;
    } 
    
    /// <summary>
    ///   Creates and configures the parser responsible for evaluating the command line arguments.
    /// </summary>
    /// 
    /// <returns>The <see cref="T:Fclp.FluentCommandLineParser"/> instance configured for the applications arguments.</returns>
    ///
    private static FluentCommandLineParser<CommandLineContext.Arguments> CreateParser()
    {
      var parser = new FluentCommandLineParser<Arguments>();
           
      parser.IsCaseSensitive = false;
      
      parser.Setup(arg => arg.Help)
            .As('?', "Help")
            .SetDefault(false);

      parser.Setup(arg => arg.TestSuitePath)
            .As("TestSuitePath")
            .Required();

      parser.Setup(arg => arg.TestScriptContainer)
            .As("TestScriptContainer")
            .SetDefault(Environment.CurrentDirectory);
            
      parser.Setup(arg => arg.TestScriptPath)
            .As("TestScriptPath")
            .SetDefault(Path.Combine(parser.Object.TestScriptContainer ?? Environment.CurrentDirectory, "test-runner.js"));

      parser.Setup(arg => arg.PhantomPath)
            .As("PhantomPath")
            .SetDefault(Path.Combine(parser.Object.TestScriptContainer ?? Environment.CurrentDirectory, "phantomjs.exe"));

      return parser;      

    } 

    /// <summary>
    ///   Defines the set of arguments that can be passed via the command line to
    ///   influence application behavior.
    /// </summary>
    /// 
    public class Arguments
    {
      /// <summary>A default instance of the command line parser, allowing default arguments to be exposed.</summary>
      private static Arguments defaultArguments = new CommandLineContext(new string[0]).Args;

      /// <summary>The set of default values for the arguments.</summary>
      private static Lazy<Dictionary<string, object>> defaultValues = new Lazy<Dictionary<string,object>>(Arguments.CreateDefaults, LazyThreadSafetyMode.PublicationOnly);

      /// <summary>
      ///   Gets the set of name/value pairs for the argument default values.
      /// </summary>
      /// 
      public static Dictionary<string, object> Defaults
      {
        get
        {
          return Arguments.defaultValues.Value;
        }
      }
      
      /// <summary>
      ///   Gets or sets a value indicating whether or not to display the help message.
      /// </summary>
      /// 
      /// <value><c>true</c> to display the help message; otherwise, <c>false</c>.</value>
      /// 
      [Description("Displays this help message.")]
      public bool Help { get; set; }

      /// <summary>
      ///   Gets or sets the path, including filename, to the suite of tests to be run.
      /// </summary>
      /// 
      [Description("Required.  The path, including filename, to the suite of tests to be run.  This will typically be an .html file.")]      
      public string TestSuitePath { get; set; }

      /// <summary>
      ///   Gets or sets the path to the directory that contains the test runner script to be passed to PhantomJS.
      /// </summary>
      /// 
      [Description("The path to the directory that contains the test runner script to be passed to PhantomJS. This parameter is most often used to specify the root where PhantomJS and the test runner script reside without overriding their default filenames.")]
      public string TestScriptContainer { get; set; }

      /// <summary>
      ///   Gets or sets the path, including filename, to the test runner script that should be passed to PhantomJS.
      /// </summary>
      /// 
      [Description("The path, including filename, to the test runner script that should be passed to PhantomJS.  This will typically be a .js file.")]
      public string TestScriptPath { get; set; }

      /// <summary>
      ///   Gets or sets the path, including filename, to the PhantomJS executable that should be used to invoke the test runner script.
      /// </summary>
      /// 
      [Description("The path, including filename, to the PhantomJS executable that should be used to invoke the test runner script.  This will typically be an .exe file.")]
      public string PhantomPath { get; set; }

      /// <summary>
      ///   Creates the set of default values for the arguments.
      /// </summary>
      /// 
      /// <returns>The set of default values for the arguments, keyed by the argument name.</returns>
      /// 
      private static Dictionary<string, object> CreateDefaults()
      {
        // Build a dictionary of default values for any members that have a non-null value, other than booleans which are treated 
        // as a command line switch (true if present, false if missing)

        return typeof(Arguments).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty)
                                .Where(property => property.PropertyType != typeof(bool))
                                .Select(item => Tuple.Create(item.Name, item.GetValue(Arguments.defaultArguments)))
                                .Where(tuple => tuple.Item2 != null)
                                .ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);
      }
    
    } // End class Arguments

  } // End class CommandLineContext
} // End namespace GoofyFoot.PhantomLauncher

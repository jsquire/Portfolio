using System;
using System.Collections.Generic;

namespace GoofyFoot.PhantomLauncher
{
  /// <summary>
  ///   Serves as the main entry point for the application.
  /// </summary>
  /// 
  public static class EntryPoint
  {
    /// <summary>
    ///   The method called as the main entry point for the application.
    /// </summary>
    /// 
    /// <param name="args">The arguments passed to the application on the command line.</param>
    /// 
    public static void Main(string[] args)
    {      
      var context = new CommandLineContext(args ?? new string[0]);
        
      // If the command line arguments were not passed correctly or the help switch
      // was set, then display the help text to the console.

      if ((context.HasArgumentErrors) || (context.Args.Help))
      {
        EntryPoint.DisplayHelp<CommandLineContext.Arguments>(CommandLineContext.Arguments.Defaults);
      }

      ProcessManager.Launch(context.Args.PhantomPath, context.Args.TestSuitePath, System.IO.Path.GetTempPath(), EntryPoint.SegmentProcessor);
    }

    /// <summary>
    ///   Displays help text to the console, describing command line usage.
    /// </summary>
    /// 
    /// <typeparam name="TArgs">The type of command line arguments expected by the application.</typeparam>
    /// 
    /// <param name="indent">The string to use as the default indent for each line of text.</param>
    /// <param name="defaults">The set of default values applied to the command line arguments.</param>
    /// <param name="lineLength">The maximum length, in characters, of the lines of text to write.</param>
    /// 
    private static void DisplayHelp<TArgs>(IDictionary<string, object> defaults,
                                           string                      indent = "    ", 
                                           int                         lineLength = 80)
    {       
      Console.WriteLine();
      Console.WriteLine("PhantomJS Console Runner Help");
      Console.WriteLine();
      
      var messageLines = "This script launches a suite of PhantonJS-based tests from the console capturing the output and enabling colored output in the Windows command prompt window.".SplitIntoLines(lineLength);

      foreach (var line in messageLines)
      {
        Console.WriteLine("{0}{1}", indent, line);
      }

      Console.WriteLine();
      Console.WriteLine("{0}Available Parameters:", indent);
      Console.WriteLine();
      
      // Get the descriptions associated with each member of the command line arguments type, displaying
      // it in lines of the appropriate length. 

      var descriptions = typeof(TArgs).GetMemberDescriptions();

      foreach (var item in descriptions)
      {
        Console.WriteLine("{0}--{1}", indent, item.Key);

        foreach (var line in item.Value.SplitIntoLines(lineLength))
        {
          Console.WriteLine("{0}{0}{1}", indent, line);
          
        }

        // If there is a default value for the argument, display it.

        if (defaults.Keys.Contains(item.Key))
        {
          Console.WriteLine("{0}{1}{1}Default: {2}", Environment.NewLine, indent, defaults[item.Key]);
        }

        Console.WriteLine();
      }
    }

    /// <summary>
    ///   Processes a set of display segments, echoing them to the console using the identified color, if present,
    ///   or default color.
    /// </summary>
    /// 
    /// <param name="segments">The set of display segments to consider.</param>
    /// 
    private static void SegmentProcessor(IEnumerable<DisplaySegment> segments)
    {
      var defaultColor = Console.ForegroundColor;

      foreach (var segment in segments)
      {
        Console.ForegroundColor = segment.Color ?? defaultColor;
        Console.Write(segment.Text);
      }

      Console.ForegroundColor = defaultColor;
    }

  } // End class EntryPoint
} // End namespace GoofyFoot.PhantomLauncher

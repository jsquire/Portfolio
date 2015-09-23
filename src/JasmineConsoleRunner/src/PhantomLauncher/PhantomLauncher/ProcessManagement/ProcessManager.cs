using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;


namespace GoofyFoot.PhantomLauncher
{
  /// <summary>
  /// Manages the child process in which PhantomJS is performing test runs.
  /// </summary>
  /// 
  public static class ProcessManager
  {
    /// <summary>The name to assign the regular expression match group that contains the color name, when extracting a color directive.</summary>
    private const string colorGroupName = "Color";

    /// <summary>The regular expression pattern to be used for parsing output text.</summary>
    private static readonly Regex colorDirectiveExpression = new Regex(String.Format(@"(?<DirectiveStart>\[\[\|)(?<{0}>[^\]]+)(?<DirectiveEnd>\|\]\])", ProcessManager.colorGroupName), RegexOptions.IgnorePatternWhitespace | RegexOptions.Compiled);

    /// <summary>
    ///   Defines the signature of the output parsing function used for interpreting the captured
    ///   stdout from the child process.
    /// </summary>
    /// 
    /// <param name="target">The target text to parse.</param>
    /// 
    /// <returns>The set of display segments discovered when parsing the <paramref name="target"/> text.</returns>
    /// 
    public delegate IEnumerable<DisplaySegment> OutputParser(string target);

    /// <summary>
    ///   Defines the signature of the function used to process display segments parsed from
    ///   the stdout of the child process.
    /// </summary>
    /// 
    /// <param name="displaySegments">The set of display segments parsed from child process output.</param>
    /// 
    public delegate void DisplaySegmentProcessor(IEnumerable<DisplaySegment> displaySegments);
    
    /// <summary>
    ///   Defines the signature of the output parsing function used for interpreting the captured
    ///   stdout from the child process.
    /// </summary>
    /// 
    /// <param name="target">The target text to parse.</param>
    /// 
    /// <returns>The set of display segments discovered when parsing the <paramref name="target"/> text.</returns>
    /// 
    private static IEnumerable<DisplaySegment> DefaultOutputParser(string target)
    {
      // If the text was null, then return an empty set.
      
      if (target == null)
      {        
        yield break;
      }
      
      // If there is no text to parse, then return an empty segment.
      
      if (target.Length < 1)
      {
        yield return new DisplaySegment(null, String.Empty);
        yield break;
      }

      // Parse the text and decompose it into segments.

      var colorDirectiveMatches = ProcessManager.colorDirectiveExpression.Matches(target);
      var currentMatch          = default(Match);
      var currentColor          = default(ConsoleColor?);
      var parsedColor           = default(ConsoleColor);
      var isLastMatch           = false;
      var start                 = 0;
      var end                   = 0;

      // If there were no matches, then no color directive was present; the target text 
      // should be returned with no associated color.

      if (colorDirectiveMatches.Count < 1)
      {
        yield return new DisplaySegment(null, target);
        yield break;
      }

      // Prepare to slice the target string into a segment by removing the color directive for 
      // parsing and retaining the non-directive text.  If the current match is the last, then
      // ensure that the entire remaining string is included in the slice.  Otherwise, only 
      // include the string segment before the next color directive was found.

      for (var index = 0; index < colorDirectiveMatches.Count; ++index)
      {
        currentColor = null;
        currentMatch = colorDirectiveMatches[index];
        isLastMatch  = ((index + 1) >= colorDirectiveMatches.Count);
        start        = (currentMatch.Index + currentMatch.Length);

        if (isLastMatch)
        {
          end = (target.Length - start);
        }
        else
        {
          end = ((colorDirectiveMatches[index + 1].Index) - start);
        }

        // Parse the color directive by assuming that it matches a ConsoleColor enumeration member.  If
        // not, then leave the default null value to indicate that there is no understood color to 
        // associate with the text.

        currentColor = (Enum.TryParse(currentMatch.Groups[ProcessManager.colorGroupName].Value, out parsedColor)) ? (ConsoleColor?)parsedColor : null;
        yield return new DisplaySegment(currentColor, target.Substring(start, end));
      }
            
      yield break;

    }

    /// <summary>
    ///   Formats the arguments needed to for launching the PhantomJS process.
    /// </summary>
    /// 
    /// <param name="jasmineRunnerPath">The full path, including filename, to the script that serves as a shim between PhantomJS and the Jasmine library allowing tests to be run.</param>
    /// <param name="testSuitePath">The full path, including filename, to the test suite that should be run.</param>
    /// 
    /// <returns>The argument string to be passed to the PhantomJS process to run the specified suite of Jasmine tests.</returns>
    /// 
    public static string FormatPhantomArguments(string jasmineRunnerPath,
                                                string testSuitePath)
    {
      if (String.IsNullOrWhiteSpace(jasmineRunnerPath))
      {
        throw new ArgumentNullException("jasmineRunnerPath");
      }

      if (String.IsNullOrWhiteSpace(testSuitePath))
      {
        throw new ArgumentNullException("testSuitePath");
      }

      if (!File.Exists(jasmineRunnerPath))
      {
        throw new ArgumentException("The Jasmine Runner path does not exist.", "jasmineRunnerPath");
      }

      if (!File.Exists(testSuitePath))
      {
        throw new ArgumentException("The test suite path does not exist.", "testSuitePath");
      }

      return String.Format("{0} file:///{1}", jasmineRunnerPath, testSuitePath);
    } 

    /// <summary>
    ///   Launches the specified executable as a child process, capturing its stdout and parsing its output, using a 
    ///   default processor, into a display segment set.
    /// </summary>
    /// 
    /// <param name="executablePath">The path, including filename, to the executable to launch.</param>
    /// <param name="arguments">The arguments to pass to the child process.</param>
    /// <param name="workingPath">The working path to specify for the child process.</param>
    /// <param name="displaySegmentProcessor">The callback function responsible for processing any display segments parsed from the child process' stdout captures.</param>
    /// 
    /// <remarks>
    ///   This call will block until the child process exits.
    /// </remarks>
    /// 
    public static void Launch(string                  executablePath,
                              string                  arguments,
                              string                  workingPath,
                              DisplaySegmentProcessor displaySegmentProcessor)
    {
      ProcessManager.Launch(executablePath, arguments, workingPath, displaySegmentProcessor, ProcessManager.DefaultOutputParser);
    }

    /// <summary>
    ///   Launches the specified executable as a child process, capturing its stdout and parsing its output into
    ///   a display segment set.
    /// </summary>
    /// 
    /// <param name="executablePath">The path, including filename, to the executable to launch.</param>
    /// <param name="arguments">The arguments to pass to the child process.</param>
    /// <param name="workingPath">The working path to specify for the child process.</param>
    /// <param name="displaySegmentProcessor">The callback function responsible for processing any display segments parsed from the child process' stdout captures.</param>
    /// <param name="outputParser">The callback function responsible for parsing the output captured from the child process' stdout stream.</param>
    /// 
    /// <remarks>
    ///   This call will block until the child process exits.
    /// </remarks>
    /// 
    public static void Launch(string                  executablePath,
                              string                  arguments,
                              string                  workingPath,
                              DisplaySegmentProcessor displaySegmentProcessor,
                              OutputParser            outputParser)
    {
      if (executablePath == null) 
      {
        throw new ArgumentNullException("executablePath");
      }

      if (displaySegmentProcessor == null)
      {
        throw new ArgumentNullException("displaySegmentProcessor");
      }

      if (outputParser == null)
      {
        throw new ArgumentNullException("outputParser");
      }

      if (!File.Exists(executablePath))
      {
        throw new ArgumentException("The path does not exist", "executablePath");
      }

      var dataRecieveCompleted = new ManualResetEvent(false);

      var processStartInfo = new ProcessStartInfo
      {
        CreateNoWindow         = true,
        RedirectStandardOutput = true,
        UseShellExecute        = false,
        ErrorDialog            = false,
        Arguments              = arguments,
        FileName               = executablePath,
        WorkingDirectory       = workingPath
      };

      DataReceivedEventHandler dataHandler = (sender, args) =>
      {        
        if ((args == null ) || (args.Data == null))
        {
          dataRecieveCompleted.Set();
          return;
        }

        var result = outputParser(args.Data);
        displaySegmentProcessor(result);
      };
      
      using (var process = new Process())
      {
        try
        {
          process.StartInfo = processStartInfo;                  
          process.OutputDataReceived += dataHandler;  

          if (process.Start())
          {
            process.BeginOutputReadLine();
            process.WaitForExit();

            // Because the data recieved can be sent after the process exited, wait
            // until a null data event happens or a second has passed to return.

            dataRecieveCompleted.WaitOne(TimeSpan.FromSeconds(1));     
          }
        }

        finally
        {
          if (process != null)
          {
            process.OutputDataReceived -= dataHandler;
          }
        }
      }
    }
        
  } // End class ProcessManager
} // End namespace GoofyFoot.PhantomLauncher

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using FluentAssertions;
using Xunit;

using UnderTest = GoofyFoot.PhantomLauncher;

namespace GoofyFoot.PhantomLauncher.UnitTests
{
  /// <summary>
  ///   The set of unit tests for the <see cref="T: GoofyFoot.PhantomLauncher.ProcessManager"/> class.
  /// </summary>
  /// 
  public class ProcessManager : IDisposable
  {
    /// <summary>Serves as a String.Format mask for creating color directives used in output text.</summary>
    private const string colorDirectiveMask = "[[|{0}|]]";

    /// <summary>A reference to the ProcessManager's private default output parser method, to allow easier testing of it.</summary>
    private UnderTest.ProcessManager.OutputParser defaultOutputParser;

    /// <summary>The path to the test process executable; the first invocation will trigger the generation attempt.</summary>
    private Lazy<string> testProcessPath = new Lazy<string>( () => SimulationProcessGenerator.Generate(Path.GetTempPath()), LazyThreadSafetyMode.PublicationOnly);

    /// <summary>Indicates whether or not Dispose has been called, to prevent redundant disposing.</summary>
    private bool disposed = false;
    
    /// <summary>
    ///   Initializes a new instance of the <see cref="T:GoofyFoot.PhantomLauncher.UnitTests.ProcessManager"/> class.
    /// </summary>
    /// 
    public ProcessManager()
    {
      // Capture the private default output parser method for testing.  

      var methodInfo = typeof(UnderTest.ProcessManager).GetMethod("DefaultOutputParser", BindingFlags.NonPublic | BindingFlags.Static);
      this.defaultOutputParser = (UnderTest.ProcessManager.OutputParser)Delegate.CreateDelegate(typeof(UnderTest.ProcessManager.OutputParser), methodInfo);
    }

    /// <summary>
    ///   Performs the tasks needed to clean up the ambient environment after tests from this
    ///   class have completed.
    /// </summary>
    /// 
    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
    /// 
    protected virtual void Dispose(bool disposing)
    {
      if (this.disposed)
      {
        return;
      }

      if (disposing)
      {
        // If the test process was not generated, avoid doing so now, as it will just be removed.

        var path = (this.testProcessPath.IsValueCreated) ? this.testProcessPath.Value : null;

        try
        {
          if ((!String.IsNullOrEmpty(path)) && (File.Exists(path)))
          {            
            File.Delete(path);            
          }
        }

        catch (Exception ex)
        {
          Debug.WriteLine("Unable to delete the test executable at: [{0}].   Reason: [{1}]", path, ex.Message);
        }
      }
       
      this.disposed = true;
    }

    /// <summary>
    ///   Validates that parsing a null target.
    /// </summary>
    /// 
    [Fact()]
    public void OutputParsingNullProducesAnEmptySet()
    {
      this.defaultOutputParser(null).ToList().Should().BeEmpty("because performing output parsing on null should result in an empty set");
    }

    /// <summary>
    ///   Validates parsing an empty target.
    /// </summary>
    /// 
    [Fact()]
    public void OutputParsingEmptyProducesASingleItemSet()
    {
      var set = this.defaultOutputParser(String.Empty).ToList();
      
      set.Should().HaveCount(1, "because parsing an empty string should produce a single item set");
      set.First().Color.Should().BeNull("because an empty string has no color directive");
    }

    /// <summary>
    ///   Validates parsing a target without color directive.
    /// </summary>
    /// 
    [Fact()]
    public void OutputParsingNoDirectiveProducesSingleItemSet()
    {
      var expectedText = "This has no directive in here.";
      var set          = this.defaultOutputParser(expectedText).ToList();
      
      set.Should().HaveCount(1, "because parsing a target without a color directive should produce a single item set");
      set.First().Color.Should().BeNull("because a target with no color directive should not specify a color");
      set.First().Text.Should().Be(expectedText, "because a target without color directive should be returned unaltered");
    }

    /// <summary>
    ///   Validates that parsing a string with a single color directive.
    /// </summary>
    /// 
    [Fact()]
    public void OutputParsingASingleDirectiveTargetProducesASingleItemSet()
    {
      var expectedColor = ConsoleColor.Yellow;
      var expectedText  = "This is a test";
      var parseTarget   = String.Format(ProcessManager.colorDirectiveMask, expectedColor) + expectedText;
      var set           = this.defaultOutputParser(parseTarget).ToList();
      
      set.Should().HaveCount(1, "because there was a single directive present single item set");
      set.First().Color.Should().Be(expectedColor, "because the directive should match the segment color");
      set.First().Text.Should().Be(expectedText, "because the output text should match the parse target, without the directive");
    }

    /// <summary>
    ///   Validates that parsing a string with two color directives.
    /// </summary>
    /// 
    [Fact()]
    public void OutputParsingWithTwoDirectivesProducesATwoItemSet()
    {
      var expected = new [] 
      { 
        Tuple.Create(ConsoleColor.Yellow, "This is a test"),
        Tuple.Create(ConsoleColor.Red, "More Text")
      };
            
      var parseTarget = String.Empty;
      
      foreach (var item in expected)
      {
        parseTarget += String.Format(ProcessManager.colorDirectiveMask, item.Item1);
        parseTarget += item.Item2;
      }

      var set = this.defaultOutputParser(parseTarget).ToList();
      
      set.Should().HaveCount(expected.Length, "because the set length should equal the number of directives in the target text.");

      for (var index = 0; index < expected.Length; ++index)
      {      
        set[index].Color.Should().Be(expected[index].Item1, "because the directive should match the segment color and order should be retained.  Index: {0}", index);
        set[index].Text.Should().Be(expected[index].Item2, "because the output text should match the segment text and order should be maintained.  Index: {0}", index);
      }
    }

    /// <summary>
    ///   Validates that parsing a string with multiple random color directives.
    /// </summary>
    /// 
    [Fact()]
    public void OutputParsingWithMultipleRandomDirectives()
    {
      var rng    = new Random();
      var colors = (ConsoleColor[])Enum.GetValues(typeof(ConsoleColor));
      var count  = rng.Next(3, 100);

      var expected = new Tuple<ConsoleColor, string>[count];

      for (var index = 0; index < count; ++index)
      {         
        expected[index] = Tuple.Create(colors[rng.Next(0, colors.Length)], new string(index.ToString().ToCharArray()[0], rng.Next(3, 100)));
      };
            
      var parseTarget = String.Empty;
      
      foreach (var item in expected)
      {
        parseTarget += String.Format(ProcessManager.colorDirectiveMask, item.Item1);
        parseTarget += item.Item2;
      }

      var set = this.defaultOutputParser(parseTarget).ToList();
      
      set.Should().HaveCount(expected.Length, "because the set length should equal the number of directives in the target text.");

      for (var index = 0; index < expected.Length; ++index)
      {      
        set[index].Color.Should().Be(expected[index].Item1, "because the directive should match the segment color and order should be retained.  Index: {0}", index);
        set[index].Text.Should().Be(expected[index].Item2, "because the output text should match the segment text and order should be maintained.  Index: {0}", index);
      }
    }

    /// <summary>
    ///   Validates that attempting to launch the process manager with a null executable path is not allowed.
    /// </summary>
    /// 
    [Fact()]
    public void LaunchDoesNotAllowNullExecutablePath()
    {
      Action actionUnderTest = () => UnderTest.ProcessManager.Launch(null, "test test", Environment.CurrentDirectory, set => {} );

      actionUnderTest.ShouldThrow<ArgumentNullException>()
                     .And.ParamName.Should().Be("executablePath", "because a null executable path is not allowed");
    }

    /// <summary>
    ///   Validates that attempting to launch the process manager with an invalid executable path is not allowed.
    /// </summary>
    /// 
    [Fact()]
    public void LaunchDoesNotAllowNonExistingExecutablePath()
    {
      Action actionUnderTest = () => UnderTest.ProcessManager.Launch(Path.Combine(Environment.CurrentDirectory, Guid.NewGuid().ToString() + ".exe"), "test test", Environment.CurrentDirectory, set => {} );

      actionUnderTest.ShouldThrow<ArgumentException>()
                     .And.ParamName.Should().Be("executablePath", "because an invalid executable path is not allowed");
    }

    /// <summary>
    ///   Validates that attempting to launch the process manager with a null output parser is not allowed.
    /// </summary>
    /// 
    [Fact()]
    public void LaunchDoesNotAllowNullOutputParser()
    {
      Action actionUnderTest = () => UnderTest.ProcessManager.Launch(Environment.CommandLine, "test test", Environment.CurrentDirectory, set => {}, null);

      actionUnderTest.ShouldThrow<ArgumentNullException>()
                     .And.ParamName.Should().Be("outputParser", "because a null output processor is not allowed");
    }

    /// <summary>
    ///   Validates that attempting to launch the process manager with a null display segment is not allowed.
    /// </summary>
    /// 
    [Fact()]
    public void LaunchDoesNotAllowNullDisplaySegmentProcesser()
    {
      Action actionUnderTest = () => UnderTest.ProcessManager.Launch(Environment.CommandLine, "test test", Environment.CurrentDirectory, null);

      actionUnderTest.ShouldThrow<ArgumentNullException>()
                     .And.ParamName.Should().Be("displaySegmentProcessor", "because a null displaySegmentProcessor is not allowed");
    }

    /// <summary>
    ///   Verifies that the output parser and segment processor are only called when the child process produces output.
    /// </summary>
    /// 
    [Fact()]
    public void LaunchWithNoOutputDoesNotInvokeTheParserAndProcessor()
    {
      var path            = this.testProcessPath.Value;
      var parserCalled    = false;
      var processorCalled = false;

      path.Should().NotBeNullOrEmpty("because the test executable is required to verify launching the process.");
            
      UnderTest.ProcessManager.OutputParser outputParser = output =>
      {
        parserCalled = true;
        return Enumerable.Empty<UnderTest.DisplaySegment>();
      };

      UnderTest.ProcessManager.DisplaySegmentProcessor segmentProcessor = segments =>
      {
        processorCalled = true;
      };
      
      UnderTest.ProcessManager.Launch(path, "0", Path.GetDirectoryName(path), segmentProcessor, outputParser);

      parserCalled.Should().BeFalse("because no output was generated.");
      processorCalled.Should().BeFalse("because no output was generated.");
    }

    /// <summary>
    ///   Verifies that the output parser and segment processor are called when the child process produces output.
    /// </summary>
    /// 
    [Fact()]
    public void LaunchWithOutputInvokesTheParserAndProcessor()
    {
      var path            = this.testProcessPath.Value;
      var parserCalled    = false;
      var processorCalled = false;

      path.Should().NotBeNullOrEmpty("because the test executable is required to verify launching the process.");
            
      UnderTest.ProcessManager.OutputParser outputParser = output =>
      {
        parserCalled = true;
        return Enumerable.Empty<UnderTest.DisplaySegment>();
      };

      UnderTest.ProcessManager.DisplaySegmentProcessor segmentProcessor = segments =>
      {
        processorCalled = true;
      };
      
      UnderTest.ProcessManager.Launch(path, "1", Path.GetDirectoryName(path), segmentProcessor, outputParser);

      parserCalled.Should().BeTrue("because output was generated.");
      processorCalled.Should().BeTrue("because output was generated.");
    }

    /// <summary>
    ///   Verifies that the output parser and segment processor are called appropriately when the child process produces output.
    /// </summary>
    /// 
    /// <param name="outputCount">The number of times that the child process should write to the stdout stream.</param>
    /// <param name="delayMilliseconds">The delay, in milliseconds, that the child process should wait between writes to the output stream.</param>
    /// 
    [Theory()]
    [InlineData(Int32.MinValue, 250)]
    [InlineData(-1, 25)]
    [InlineData(0, 25)]
    [InlineData(1, 25)]
    [InlineData(2, 25)]
    [InlineData(3, 25)]
    [InlineData(10, 5)]
    [InlineData(15, 5)]
    [InlineData(25, 5)]
    [InlineData(50, 5)]
    [InlineData(99, 5)]
    [InlineData(100, 5)]
    [InlineData(200, 1)]
    [InlineData(250, 1)]
    public void Launch(int outputCount,
                       int delayMilliseconds)
    {
      var path               = this.testProcessPath.Value;
      var expected           = Math.Max(outputCount, 0);
      var parserCallCount    = 0;
      var processorCallCount = 0;

      path.Should().NotBeNullOrEmpty("because the test executable is required to verify launching the process.");
            
      UnderTest.ProcessManager.OutputParser outputParser = output =>
      {
        ++parserCallCount;
        return Enumerable.Empty<UnderTest.DisplaySegment>();
      };

      UnderTest.ProcessManager.DisplaySegmentProcessor segmentProcessor = segments =>
      {
        ++processorCallCount;
      };
      
      UnderTest.ProcessManager.Launch(path, String.Format("{0} {1}", outputCount, delayMilliseconds), Path.GetDirectoryName(path), segmentProcessor, outputParser);
            
      parserCallCount.Should().Be(expected, "because the parser should be called once per write to stdout.");
      processorCallCount.Should().Be(expected, "because the parser should be called once per write to stdout.");
    }


    /// <summary>
    ///   Performs the tasks needed to clean up the ambient environment after tests from this
    ///   class have completed.
    /// </summary>
    /// 
    /// <remarks>
    ///   Dispose will be called by the XUnit framework after tests from this class have completed, similar
    ///   to other frameworks that allow test class/fixture setup and teardown methods.
    /// </remarks>
    ///
    public void Dispose()
    {
      this.Dispose(true);
    }    
    
  } // End class ProcessManager
} // End namespace GoofyFoot.PhantomLauncher.UnitTests
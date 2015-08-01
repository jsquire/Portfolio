using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;

using UnderTest = GoofyFoot.PhantomLauncher;

namespace GoofyFoot.PhantomLauncher.UnitTests
{
  /// <summary>
  ///   The suite of tests that verifies the functionality of the CommandLineContext object
  /// </summary>
  /// 
  public class CommandLineContext
  {
    /// <summary>
    ///   The set of data for use with the testing of whether the "Help" command line argument is
    ///   treated as a switch.
    /// </summary>
    /// 
    public static IEnumerable<object[]> HelpSwitchData
    {
      get
      {
        return new []
        {
          new object[] { new string[0],                                                     false, "because no arguments were passed" },
          new object[] { new string[] { "--Help"                                         }, true,  "because the help switch was passed alone" },
          new object[] { new string[] { "--Help", "--TestSuitePath ./"                   }, true,  "because the help switch was specified" },
          new object[] { new string[] { "--TestSuitePath", "../tests"                    }, false, "because the help switch was not specified" },
          new object[] { new string[] { "--Help", "true"                                 }, true,  "because the help switch was explicitly set to true" },
          new object[] { new string[] { "--Help", "false"                                }, false, "because the help switch was explicitly set to false" },
          new object[] { new string[] { "--Help", "false", "--TestSuitePath", "../tests" }, false, "because the help switch was explicitly set to false" },
        };
      }
    }

    /// <summary>
    ///   Validates that the command line context does not allow a null
    ///   argument set.
    /// </summary>
    /// 
    [Fact()]
    public void ContextDoesNotAllowNullArgSet()
    {
       Action actionUnderTest = () => new UnderTest.CommandLineContext(null);

       actionUnderTest.ShouldThrow<ArgumentException>()
                      .And.ParamName.Should().Be("args", "because an argument set must be passed");
    }

    /// <summary>
    ///   Validates that the command line context allows an empty
    ///   argument set.
    /// </summary>
    /// 
    [Fact()]
    public void ContextAllowsEmptyArgSet()
    {
       Action actionUnderTest = () => new UnderTest.CommandLineContext(new string[0]);

       actionUnderTest.ShouldNotThrow("because an empty argument set is valid");
    }

    /// <summary>
    ///   Validates that the default values returned by the command line context match
    ///   those returned as the default set for the Arguments.
    /// </summary>
    /// 
    [Fact()]
    public void ArgumentDefaultsMatchContextDefaults()
    {
      var argumentDefaults = UnderTest.CommandLineContext.Arguments.Defaults;
      var context          = new UnderTest.CommandLineContext(new string[0]);
      var argsType         = context.Args.GetType();
            
      foreach (var key in argumentDefaults.Keys)
      {
        var prop = argsType.GetProperty(key);

        prop.Should().NotBeNull("because the arguments type should have a property named {0}", key);
        prop.GetValue(context.Args).Should().Be(argumentDefaults[key], "because the  argument value should match the defaults for {0}", key);        
      }
    }

    /// <summary>
    ///   Validates that a context created with an empty argument set
    ///   has errors, since there are required members.
    /// </summary>
    /// 
    [Fact()]
    public void ContextWithEmptySetHasErrors()
    {      
      new UnderTest.CommandLineContext(new string[0]).HasArgumentErrors.Should().BeTrue("because there are required context arguments that were not present");
    }

    /// <summary>
    ///   Validates that a context created with an empty argument set
    ///   has errors, since there are required members.
    /// </summary>
    /// 
    /// <param name="arguments">The command line arguments to use for testing</param>
    /// <param name="expected">The expectation of whether the "Help" flat is set on the context</param>
    /// <param name="reasonMessage">The reason message to use for assertions when the expectation is not met</param>
    /// 
    [Theory()]
    [MemberData("HelpSwitchData")]
    public void HelpArgumentIsTreatedAsASwitch(string[] arguments,
                                               bool     expected,
                                               string   reasonMessage)
    {      
      new UnderTest.CommandLineContext(arguments).Args.Help.Should().Be(expected, reasonMessage);
    }

    /// <summary>
    ///   Validates that a context created with the required members provided
    ///   does not have errors.
    /// </summary>
    /// 
    /// <param name="parameterName">The name of the command line parameter to use for testing.</param>
    /// 
    [Theory()]
    [InlineData("--TestSuitePath")]
    [InlineData("/TestSuitePath")]
    public void ContextWithTestSuitePathSetDoesNotHaveErrors(string parameterName)
    {      
      new UnderTest.CommandLineContext(new string[] { parameterName, "SomeValue" }).HasArgumentErrors.Should().BeFalse("because all required arguments were present");
    }

  } // End ClassCommandLineContext
} // End namespace GoofyFoot.PhantomLauncher.UnitTests

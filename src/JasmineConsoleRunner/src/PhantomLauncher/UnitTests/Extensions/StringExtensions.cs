using System;
using System.Linq;
using System.ComponentModel;
using FluentAssertions;
using Xunit;

namespace GoofyFoot.PhantomLauncher.UnitTests
{
  /// <summary>
  ///   The set of unit tests for the extension methods defined for the <see cref="T:System.String"/> class.
  /// </summary>
  /// 
  public class StringExtensions
  {
    /// <summary>
    ///   Verifies that the provided line length is validated against 0.
    /// </summary>
    /// 
    [Fact()]    
    public void LineLengthCannotBeZero()
    { 
      Action actionUnderTest = () => "Test".SplitIntoLines(0).ToList();
       
      actionUnderTest.ShouldThrow<ArgumentException>()
                     .And.ParamName.Should().Be("lineLength", "because the line length must not be zero");
    }
    
    /// <summary>
    ///   Verifies that the provided line length is validated against negative values.
    /// </summary>
    /// 
    [Theory()]
    [InlineData(-1)]  
    [InlineData(-2)]
    [InlineData(-7)]
    [InlineData(int.MinValue)]
    public void LineLengthCannotBeNegative(int lineLimit)
    {     
      Action actionUnderTest = () => "Test".SplitIntoLines(lineLimit).ToList();
            
      actionUnderTest.ShouldThrow<ArgumentException>()
                     .And.ParamName.Should().Be("lineLength", "because the line length must be positive");
    }

    /// <summary>
    ///   Verifies that splitting a null string returns an empty enumerable.
    /// </summary>
    /// 
    [Fact()]    
    public void NullStringReturnsNoLines()
    { 
      ((string)null).SplitIntoLines(80).Any().Should().BeFalse("because a null string has no content to split");
    } 

    /// <summary>
    ///   Verifies that splitting an empty string returns an empty enumerable.
    /// </summary>
    /// 
    [Fact()]    
    public void EmptyStringReturnsNoLines()
    { 
      String.Empty.SplitIntoLines(80).Any().Should().BeFalse("because an empty string has no content to split");
    }

    /// <summary>
    ///   Verifies that a string containing a line break at the length limit is split
    ///   at that length limit.
    /// </summary>
    /// 
    [Theory()]
    [InlineData(" ")]
    [InlineData("\r\n")]
    [InlineData("\r")]
    [InlineData("\n")]
    public void StringWithLinebreakAtLimitIsSplitAtLimit(string lineBreak)
    {
      var lineLength = 80;
      var secondLine = "This should be line two.";
      var target     = String.Format("{0}{1}{2}", new String('x', (lineLength - lineBreak.Length)), lineBreak, secondLine);
      var result     = target.SplitIntoLines(lineLength);

      result.Should().NotBeNull("because there should be multiple lines after the split.");

      var expanded = result.ToList();
      
      expanded.Any().Should().BeTrue("because there should be multiple lines after the split.");
      expanded.Count.Should().Be(2, "becaue there should have been two lines after the split.");
      expanded[1].Should().Be(secondLine, "because that text appeared after the character limit.");
    }

    /// <summary>
    ///   Verifies that lines do not start with break characters after being split.
    /// </summary>
    /// 
    [Theory()]
    [InlineData("0123456789 0123456789")]
    [InlineData("0123456789\r0123456789")]
    [InlineData("0123456789\n0123456789")]
    [InlineData("012345678\r\n0123456789")]
    [InlineData("012345678\r 0123456789")]
    [InlineData("012345678\n 0123456789")]
    [InlineData("012345678 \r0123456789")]
    [InlineData("012345678 \n0123456789")]
    public void SplitLinesDoNotStartWithBreakCharacters(string source)
    {      
      var result = source.SplitIntoLines(11);

      result.Should().NotBeNull("because there should be multiple lines after the split.");

      foreach (var line in result)
      {
        line.StartsWith(" ").Should().BeFalse("because \" \" is a line break character");
        line.StartsWith("\r").Should().BeFalse("because \"\\r\" is a line break character");
        line.StartsWith("\n").Should().BeFalse("because \"\\n\" is a line break character");
        line.StartsWith("\r\n").Should().BeFalse("because \"\r\n\" is a line break character");
      }
    }

  } // End class StringExtensions
} // End namespace GoofyFoot.PhantomLauncher.UnitTests


using System;
using System.Linq;
using FluentAssertions;
using Xunit;

using UnderTest = GoofyFoot.PhantomLauncher;

namespace GoofyFoot.PhantomLauncher.UnitTests
{
  /// <summary>
  ///   The set of unit tests for the <see cref="T: GoofyFoot.PhantomLauncher.DisplaySegment"/> class.
  /// </summary>
  /// 
  public class DisplaySegment
  {
    /// <summary>
    ///   Verifies that the provided text cannot be null when creating the structure.
    /// </summary>
    /// 
    [Fact()]    
    public void TextCannotBeNull()
    { 
      Action actionUnderTest = () => new UnderTest.DisplaySegment(ConsoleColor.White, null);
       
      actionUnderTest.ShouldThrow<ArgumentNullException>()
                     .And.ParamName.Should().Be("text", "because the text may not be null");
    }

    /// <summary>
    ///   Verifies that the provided text may be empty when creating the structure.
    /// </summary>
    /// 
    [Fact()]    
    public void EmptyTextIsAllowed()
    { 
      Action actionUnderTest = () => new UnderTest.DisplaySegment(ConsoleColor.White, String.Empty);
       
      actionUnderTest.ShouldNotThrow("because empty text is allowed");
    }

    /// <summary>
    ///   Verifies that the provided text may be empty when creating the structure.
    /// </summary>
    /// 
    [Fact()]    
    public void NullColorIsAllowed()
    { 
      Action actionUnderTest = () => new UnderTest.DisplaySegment(null, String.Empty);
       
      actionUnderTest.ShouldNotThrow("because a null color is allowed");
    }

  } // End class DisplaySegment  
} // End namespace GoofyFoot.PhantomLauncher.UnitTests


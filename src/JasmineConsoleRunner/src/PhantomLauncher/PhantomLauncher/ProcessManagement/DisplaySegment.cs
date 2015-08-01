using System;

namespace GoofyFoot.PhantomLauncher
{
  /// <summary>
  ///   Represents a segment of text to be displayed and the corresponding color in which the
  ///   text should appear.
  /// </summary>
  /// 
  public class DisplaySegment
  {
    /// <summary>The color that the text is intended to be displayed in.</summary>
    public readonly ConsoleColor? Color;

    /// <summary>The text to be displayed.</summary>
    public readonly string Text;

    /// <summary>
    ///   Prevents a default instance of the <see cref="T:GoofyFoot.PhantomLauncherDisplaySegment"/> class from being created.
    /// </summary>
    /// 
    private DisplaySegment()
    {
    }

    /// <summary>
    ///   Initializes a new instance of the <see cref="T:GoofyFoot.PhantomLauncherDisplaySegment"/> class.
    /// </summary>
    /// 
    /// <param name="color">The color that the text should be displayed in.</param>
    /// <param name="text">The text to be displayed.</param>
    ///     
    public DisplaySegment(ConsoleColor? color,
                          string        text)
    {
      if (text == null)
      {
        throw new ArgumentNullException("text");
      }

      this.Color = color;
      this.Text  = text;
    }

  } // End struct DisplaySegment
} // End namespace GoofyFoot.PhantomLauncher

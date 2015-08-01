using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace GoofyFoot.PhantomLauncher
{
  /// <summary>
  ///   The set of extension methods for the <see cref="T:System.StringExtensions"/> class
  /// </summary>
  /// 
  internal static class StringExtensions
  {
    /// <summary>
    ///   Splits a string into lines of the requested length, attempting to respect word breaks when possible.
    /// </summary>
    /// 
    /// <param name="instance">The instance that this method was invoked on.</param>
    /// <param name="lineLength">The desired lenght of the line;  lines will not exceed this value but may be shorter.</param>
    /// 
    /// <returns>The set of lines that the string was split into.</returns>
    /// 
    public static IEnumerable<string> SplitIntoLines(this string instance,
                                                          int    lineLength)
    {
      if (lineLength < 1)
      {
        throw new ArgumentException("The line length must be at least 1 character long.", "lineLength");
      }
      
      // If there is no content, then there is nothing to split.

      if (String.IsNullOrEmpty(instance))
      {
        yield break;
      }

      // If the content is already within the line length limit, then there is nothing to split.  

      if (instance.Length < lineLength)
      {
        yield return instance;
        yield break;
      }
      
      // The string needs to be split into lines, but ensure that we only split on spaces and new line characters to ensure that we
      // don't break a word.  If it is not possible to split without a break, then it will be forced.
                  
      var startAt = 0;
      var length  = Math.Min(instance.Length, lineLength);
      var current = default(string);

      while (startAt < instance.Length)
      {
        // If there remainder of the string will fit on a line, just return it and stop
        // splitting.

        if ((instance.Length - startAt) < length)
        {
          yield return instance.Substring(startAt);
          yield break;
        }
                
        current = instance.Substring(startAt, length);

        while ((length > 0) && (!current.EndsWith(" ")) && (!current.EndsWith("\r")) && (!current.EndsWith("\n")))
        {
          --length;          
          current = instance.Substring(startAt, length);
        }

        // If the length reached 0, then there wasn't a space or linebreak to use; force the 
        // split in the aboslute position.

        if (length == 0)
        {
          length  = Math.Min((instance.Length - startAt), lineLength);
          current = instance.Substring(startAt, length);
        }

        // Reset for the next iteration and return the current line.  Adjust the starting point by the number of 
        // characters that the line break had to back off, as a new line shouldn't include the break characters at the
        // start of it.

        startAt = (startAt + length);        
        length  = lineLength;

        yield return current;
      }
    }


  } // End class StringExtensions
} // End namespace GoofyFoot.PhantomLauncher

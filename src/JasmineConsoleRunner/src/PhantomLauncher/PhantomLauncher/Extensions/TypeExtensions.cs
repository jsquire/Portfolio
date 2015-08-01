using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace GoofyFoot.PhantomLauncher
{
  /// <summary>
  ///   The set of extension methods for the <see cref="T:System.Type"/> class
  /// </summary>
  /// 
  internal static class TypeExtensions
  {
    /// <summary>
    ///   Gets the set of descriptions for members of the provided <see cref="T:System.Type"/> decorated
    ///   with a <see cref="T:System.ComponentModel.DescriptionAttribute"/>.
    /// </summary>
    /// 
    /// <param name="instance">The instance that this method was invoked on.</param>
    /// 
    /// <returns>The set of descriptions for the members of the type which have them; if no members have descriptions, an empty set is returned.</returns>
    /// 
    public static IDictionary<string, string> GetMemberDescriptions(this Type instance)
    {
      if (instance == null)
      {
        return new Dictionary<string, string>(0);
      }

      // Description attributes from anywhere in the hierarchy will be returned, allowing a virtual member to set a description for all inherited members.  In
      // the case that multiple descriptions are set for the same member, no guarantee is made for which will be selected; it is dependent upon the order they 
      // are returned in the reflection call.

      return instance.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy)                            
                     .Select(member => new KeyValuePair<string, DescriptionAttribute>(member.Name, member.GetCustomAttributes<DescriptionAttribute>(true).FirstOrDefault()))
                     .Where(pair => pair.Value != null)  
                     .ToDictionary(pair => pair.Key, pair => pair.Value.Description);
    }
  } // End class TypeExtensions
} // End namespace GoofyFoot.PhantomLauncher

using System;
using System.ComponentModel;
using FluentAssertions;
using Xunit;

namespace GoofyFoot.PhantomLauncher.UnitTests
{
  /// <summary>
  ///   The set of unit tests for the extension methods defined for the <see cref="T:Systme.Type"/> class.
  /// </summary>
  /// 
  public class TypeExtensions
  {
    /// <summary>
    ///   Verifies the behavior of <see cref="M:GoofyFoot.PhantomLauncher.TypeExtensions.GetMemberDescriptions"/> when the <see cref="T:System.Type"/>
    ///   instance is null.
    /// </summary>
    /// 
    [Fact()]
    public void GetMemberDescriptionsWithNullType()
    {
     ((Type)null).GetMemberDescriptions().Should().BeEmpty("because a null instance has no members.");
    }

    /// <summary>
    ///   Verifies the behavior of <see cref="M:GoofyFoot.PhantomLauncher.TypeExtensions.GetMemberDescriptions"/> when the <see cref="T:System.Type"/>
    ///   instance contains property descriptions
    /// </summary>
    /// 
    [Fact()]
    public void GetMemberDescriptionsShouldIncludePropertyDescriptions()
    {
      var set = typeof(PropertyDescriptionType).GetMemberDescriptions();

      set.Should().NotBeNull("because there are properties with descriptions");
      set.Should().HaveCount(2, "because two properties have descriptions");
      
      foreach (var key in set.Keys)
      {
        set[key].Should().Be(String.Format(PropertyDescriptionType.DescriptionMask, key), "because the description should match the mask pattern");
      }
    }

    /// <summary>
    ///   Verifies the behavior of <see cref="M:GoofyFoot.PhantomLauncher.TypeExtensions.GetMemberDescriptions"/> when the <see cref="T:System.Type"/>
    ///   instance contains method descriptions
    /// </summary>
    /// 
    [Fact()]
    public void GetMemberDescriptionsShouldIncludeMethodDescriptions()
    {
      var set = typeof(MethodDescriptionType).GetMemberDescriptions();

      set.Should().NotBeNull("because there are methods with descriptions");
      set.Should().HaveCount(2, "because two methods have descriptions");
      
      foreach (var key in set.Keys)
      {
        set[key].Should().Be(String.Format(MethodDescriptionType.DescriptionMask, key), "because the description should match the mask pattern");
      }
    }

    /// <summary>
    ///   Verifies the behavior of <see cref="M:GoofyFoot.PhantomLauncher.TypeExtensions.GetMemberDescriptions"/> when the <see cref="T:System.Type"/>
    ///   instance contains field descriptions
    /// </summary>
    /// 
    [Fact()]
    public void GetMemberDescriptionsShouldIncludeFieldDescriptions()
    {
      var set = typeof(FieldDescriptionType).GetMemberDescriptions();

      set.Should().NotBeNull("because there are fields with descriptions");
      set.Should().HaveCount(3, "because three fields have descriptions");
      
      foreach (var key in set.Keys)
      {
        set[key].Should().Be(String.Format(FieldDescriptionType.DescriptionMask, key), "because the description should match the mask pattern");
      }
    }

    /// <summary>
    ///   Verifies the behavior of <see cref="M:GoofyFoot.PhantomLauncher.TypeExtensions.GetMemberDescriptions"/> when the <see cref="T:System.Type"/>
    ///   instance contains mixed member descriptions
    /// </summary>
    /// 
    [Fact()]
    public void GetMemberDescriptionsShouldIncludeAllDescriptions()
    {
      var set = typeof(MixedMembersDescriptionType).GetMemberDescriptions();

      set.Should().NotBeNull("because there are members with descriptions");
      set.Should().HaveCount(3, "because three members have descriptions");
      
      foreach (var key in set.Keys)
      {
        set[key].Should().Be(String.Format(MixedMembersDescriptionType.DescriptionMask, key), "because the description should match the mask pattern");
      }
    }

    /// <summary>
    ///   Verifies the behavior of <see cref="M:GoofyFoot.PhantomLauncher.TypeExtensions.GetMemberDescriptions"/> when the <see cref="T:System.Type"/>
    ///   instance contains inherited/overridden mixed member descriptions
    /// </summary>
    /// 
    [Fact()]    
    public void GetMemberDescriptionsShouldIncludeInheritedDescriptions()
    {
      var set = typeof(MixedMembersChildType).GetMemberDescriptions();

      set.Should().NotBeNull("because there are members with descriptions");
      set.Should().HaveCount(4, "because four members have descriptions");
      
      foreach (var key in set.Keys)
      {
        set[key].Should().Be(String.Format(MixedMembersChildType.DescriptionMask, key), "because the description should match the mask pattern");
      }
    }

    // ================================================
    // Disable the "never assigned to" warning for the test class definintions
    //
    #pragma warning disable 649
    // ================================================

    private class PropertyDescriptionType
    {      
      public const string DescriptionMask = "Description for {0}";

      [Description("Description for HasDescription")]      
      public string HasDescription { get { return "foo"; } }

      [Description("Description for AnotherDescription")]      
      public string AnotherDescription { set { Console.WriteLine(value); } }

      public string NoDescription { get; set; }      
    }

    private class MethodDescriptionType
    {      
      public const string DescriptionMask = "Description for {0}";

      [Description("Description for HasDescription")]      
      public string HasDescription() { return String.Empty; }

      [Description("Description for AnotherDescription")]      
      public int AnotherDescription(int param) { return param; }

      public int NoDescription(int param) { return param; }      
    }

    private class FieldDescriptionType
    {      
      public const string DescriptionMask = "Description for {0}";

      [Description("Description for HasDescription")]      
      public string HasDescription;

      [Description("Description for AnotherDescription")]      
      public int AnotherDescription = 42;

      [Description("Description for AThirdDescription")]      
      public Type AThirdDescription = typeof(String);

      public int NoDescription;      
    }

    private class MixedMembersDescriptionType
    {      
      public const string DescriptionMask = "Description for {0}";

      [Description("Description for HasDescription")]      
      public string HasDescription;

      [Description("Description for AnotherDescription")]      
      public virtual int AnotherDescription { get; set; }

      [Description("Description for AThirdDescription")]      
      public Type AThirdDescription(Type param) { return param; }

      public virtual int NoDescription { get; set; }
    }

    private class MixedMembersChildType : MixedMembersDescriptionType
    {
      public override int AnotherDescription { get; set; }

      [Description("Description for NoDescription")]
      public override int NoDescription { get; set; }

    }


    // ================================================
    // Restore the "never assigned to" warning 
    //
    #pragma warning restore 649
    // ================================================

  } // End class TypeExtensions
} // End namespace GoofyFoot.PhantomLauncher.UnitTests

namespace OrderFulfillment.Core.Infrastructure
{
    /// <summary>
    ///   Represents a generic priority level.
    /// </summary>
    /// 
    public enum Priority
    {
        /// <summary>Indicates that the target is one of the least important in the set.</summary>
        Lowest = -3,

        /// <summary>Indicates that the target is not important, but not quite completely unimportant.</summary>
        Low = -2,

        /// <summary>Indicates that the target is below normal importantce, but is not unimportant.</summary>
        BelowNormal = -1,
        
        /// <summary>The default priority, indicating the target is neither unimportant or important.</summary>
        Normal = 0,        
        
        /// <summary>Indicates that the target above normal importance, but not quite highly important.</summary>
        AboveNormal = 3,

        /// <summary>Indicates that the target is high priority, but not quite urgent level.</summary>
        High = 4,

        /// <summary>Indicates that the target is one of the most important in the set.</summary>
        Highest = 5
    }
}

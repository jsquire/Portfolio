using System;

namespace OrderFulfillment.Api.RouteConstraints
{
    /// <summary>
    ///   Allows a route constraint to identify itself and the constraint name that it should be
    ///   registered to handle.
    /// </summary>
    /// 
    /// <seealso cref="System.Attribute" />
    /// 
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class RouteConstraintAttribute : Attribute
    {
        /// <summary>
        ///   Gets the name that the constraint should be registered to handle within the Web API
        ///   configuration. 
        /// </summary>
        /// 
        public string ConstraintName { get; }

        /// <summary>
        ///   Initializes a new instance of the <see cref="RouteConstraintAttribute"/> class.
        /// </summary>
        /// 
        /// <param name="constraintName">The name that the constraint should be registered to handle.</param>
        /// 
        public RouteConstraintAttribute(string constraintName)
        {
            if (String.IsNullOrEmpty(constraintName))
            {
                throw new ArgumentNullException(nameof(constraintName));
            }

            this.ConstraintName = constraintName;
        }
    }
}
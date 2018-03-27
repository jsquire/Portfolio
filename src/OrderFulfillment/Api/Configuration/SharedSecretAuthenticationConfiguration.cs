using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using OrderFulfillment.Core.Configuration;

namespace OrderFulfillment.Api.Configuration
{
    /// <summary>
    ///   The set of configuration needed for authentication of a user/entity
    ///   using the shared secret scheme.
    /// </summary>
    /// 
    public class SharedSecretAuthenticationConfiguration : IConfiguration
    {        
        /// <summary>
        ///   Indicates whether or not the handler is enabled.
        /// </summary>
        /// 
        /// <value><c>true</c> if the handler is enabled; otherwise, <c>false</c>.</value>
        /// 
        public bool Enabled { get;  set; }

        /// <summary>
        ///   The primary value for the app key.
        /// </summary>
        /// 
        /// <value>
        ///   This is the preferred key when performing authentication and should be given precedence over any
        ///   secondary values defined.
        /// </value>
        /// 
        public string PrimaryKey { get;  set; }

        /// <summary>
        ///   The secondary value for the app key.
        /// </summary>
        /// 
        /// <value>
        ///   This value should only be used when rolling the Key.  At other times, it should
        ///   be left empty or set to the same value as the <see cref="PrimaryKey" />.
        /// </value>
        /// 
        public string SecondaryKey { get;  set; }

        /// <summary>
        ///   The primary value for the app secret.
        /// </summary>
        /// 
        /// <value>
        ///   This is the preferred secret when performing authentication and should be given precedence over any
        ///   secondary values defined.
        /// </value>
        /// 
        public string PrimarySecret { get;  set; }

        /// <summary>
        ///   The secondary value for the app secret.
        /// </summary>
        /// 
        /// <value>
        ///   This value should only be used when rolling the secret.  At other times, it should
        ///   be left empty or set to the same value as the <see cref="PrimarySecret" />.
        /// </value>
        /// 
        public string SecondarySecret { get;  set; }
    }
}
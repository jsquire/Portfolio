using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;

namespace WebApp_OpenIDConnect_DotNet
{
    /// <summary>
    ///   Acts as a formatter for protecting data in a string format that can be used as a 
    ///   cookie value.
    /// </summary>
    /// 
    /// <seealso cref="Microsoft.AspNetCore.Authentication.ISecureDataFormat{System.String}" />
    /// <seealso cref="Microsoft.AspNetCore.Authentication.SecureDataFormat{System.String}"/>    
    /// <seeaksi cref="Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions"/>
    /// <seealso cref="Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectPostConfigureOptions"/>
    /// 
    /// <remarks>
    ///     <para>
    ///       This class is intended to ensure that the nonce cookie does not trigger OWASP CRS v3.0 warnings in the web application 
    ///       firewall due to the potential for URL-focused Base64 encoding to include a sequence of characters with double hyphen (--) 
    ///       that is flagged as potential SQL injection attack.   
    ///     </para  
    ///     <para>
    ///         The content of this class is heavily based on the SecureDataFormat class that is used
    ///         by default in the OpenId Connect authentication provider.  The original source can be viwed here: 
    ///         <see cref="l:https://github.com/aspnet/AspNetCore/blob/02ca469ea1ee06be2769ebbb0252bc88847d6378/src/Security/src/Microsoft.AspNetCore.Authentication/Data/SecureDataFormat.cs."/>
    ///     </para>
    ///     <para>
    ///         The encoding difference in the Base64 scheme between the standard and file/url versions are the substitution of two characters, "-" for "+" and
    ///         "_" for "/".   The character sets are described in RFC 4648, which can be seen here: <see cref="l:https://tools.ietf.org/html/rfc4648"/>.  The SecureDataFormat class makes use of the
    ///         internal WebEncoders class which performs encoding based on the Base64 web scheme, calling the Base64UrlEncode method (lines 253 - 314) in the source, which can be seen here: 
    ///         <see cref="l:https://github.com/aspnet/Extensions/blob/master/src/Shared/src/WebEncoders/WebEncoders.cs"/>   This class makes use of the System.Convert.ToBase64String method, which uses 
    ///         the character set defined on line 140 of the source, which can be seen here: <see cref="l:https://github.com/Microsoft/referencesource/blob/master/mscorlib/system/convert.cs"/>         
    ///     </para>
    ///     <para>
    ///         While the majority of implementations encode cookie values with URL safety in mind, the use of the standard Base64 alphabet for encoding is permissible the cookie specification.  
    ///         According to RFC 6265, a cookie value may contain US-ASCII characters, excluding control characters, and that only three types of characters must be encoded: semicolon, comma, and white space. 
    ///         <see cref="l:https://www.ietf.org/rfc/rfc6265.txt" /> (page 8).      
    ///     </para>
    ///     <para>
    ///         It is worth noting that the default StringDataFormat instance is not injected by the DI container; if not provided with the
    ///         options used for OpenId Connect, it is created within the OpenIdConnectPostConfigurationOptions on line 47, as seen in the source:
    ///         <see cref="l:https://github.com/aspnet/Security/blob/7e14b052ea9cb935ec4f5cb0485b4edb5d41297a/src/Microsoft.AspNetCore.Authentication.OpenIdConnect/OpenIdConnectPostConfigureOptions.cs"/>
    ///     </para>
    /// </remarks>
    /// 
    public class OpenIdConnectNonceStringDataFormat : ISecureDataFormat<string>
    {
        /// <summary>The serializer used for transforming between a sequence of bytes and a string.</summary>
        private readonly IDataSerializer<string> serializer;

        /// <summary>The protector responsible for ensuring that a sequence of bytes is secure and can safely be written to the cookie.</summary>
        private readonly IDataProtector protector;

        /// <summary>
        ///   Initializes a new instance of the <see cref="OpenIdConnectNonceStringDataFormat"/> class.
        /// </summary>
        /// 
        /// <param name="serializer">The serializer used for transforming between a sequence of bytes and a string.</param>
        /// <param name="protector">The protector responsible for ensuring that a sequence of bytes is secure and can safely be written to the cookie.</param>
        /// 
        public OpenIdConnectNonceStringDataFormat(IDataSerializer<string> serializer, 
                                                  IDataProtector          protector)
        {
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this.protector  = protector  ?? throw new ArgumentNullException(nameof(protector));
        }

        /// <summary>
        ///   Performs the actions needed to protect the provided data, so that it can be written to a non-secure transport,
        ///   such as a cookie.
        /// </summary>
        /// 
        /// <param name="data">The data to protect.</param>
        /// 
        /// <returns>An encoded string which represents the protected data.</returns>
        /// 
        public string Protect(string data) => this.Protect(data, null);


        /// <summary>
        ///   Performs the actions needed to protect the provided data, so that it can be written to a non-secure transport,
        ///   such as a cookie.
        /// </summary>
        /// 
        /// <param name="data">The data to protect.</param>
        /// <param name="purpose">A description of the purpose for which the data is being protected.</param>
        /// 
        /// <returns>An encoded string which represents the protected data.</returns>
        /// 
        public string Protect(string data, 
                              string purpose) 
        {   
            var activeProtector = (String.IsNullOrEmpty(purpose)) ? this.protector : this.protector.CreateProtector(purpose);
            var serializedData  = this.serializer.Serialize(data);
            var protectedBytes  = activeProtector.Protect(serializedData);

            return this.EncodeProtectedData(protectedBytes);
        }

        /// <summary>
        ///   Performs the actions needed to unprotect the provided string, which was previously protected via the the <see cref="OpenIdConnectNonceStringDataFormat.Protect"/>
        ///   method.
        /// </summary>
        /// 
        /// <param name="data">The data to protect.</param>
        /// 
        /// <returns>The decoded byte sequence that had been protected.</returns>
        /// 
        public string Unprotect(string protectedText) => this.Unprotect(protectedText, null);
        
        
        /// <summary>
        ///   Performs the actions needed to unprotect the provided string, which was previously protected via the the <see cref="OpenIdConnectNonceStringDataFormat.Protect"/>
        ///   method.
        /// </summary>
        /// 
        /// <param name="data">The data to protect.</param>
        /// <param name="purpose">A description of the purpose for which the data is being unprotected.</param>
        /// 
        /// <returns>The decoded byte sequence that had been protected.</returns>
        /// 
        public string Unprotect(string protectedText, 
                                string purpose)
        {
            // If there was no value, there is nothing to unprotect.

            if (String.IsNullOrEmpty(protectedText))
            {
                return protectedText;
            }

            // If the data cannot be decoded, there is nothing to unprotect.

            var decodedData = default(byte[]);

            try
            {
                decodedData = this.DecodeProtectedData(protectedText);
            }

            catch
            {
            }

            if (decodedData == null)
            {
                return null;
            }

            // If the data could not be unprotected, then return a default value.

            var activeProtector = (String.IsNullOrEmpty(purpose)) ? this.protector : this.protector.CreateProtector(purpose);
            var unprotectedData = activeProtector.Unprotect(decodedData);

            if (unprotectedData == null)
            {
                return null;
            }

            return this.serializer.Deserialize(unprotectedData);
        }

        /// <summary>
        ///   Performs the actions needed to encode a protected sequence of bytes into string format.
        /// </summary>
        /// 
        /// <param name="protectedData">The protected data to encode.</param>
        /// 
        /// <returns>An encoded version of the <paramref name="protectedData"/>, in string form.</returns>
        /// 
        protected virtual string EncodeProtectedData(byte[] protectedData) => Convert.ToBase64String(protectedData);

        /// <summary>
        ///   Performs the actions needed to decode an encoded string into its corresponding sequence of bytes.
        /// </summary>
        /// 
        /// <param name="encodedData">The string format of the encoded data.</param>
        /// 
        /// <returns>The sequence of bytes which had previously been encoded.</returns>
        /// 
        protected virtual byte[] DecodeProtectedData(string encodedData) => Convert.FromBase64String(encodedData);
    }
}

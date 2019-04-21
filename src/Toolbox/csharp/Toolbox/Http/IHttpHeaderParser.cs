using System.Collections.Generic;

namespace Squire.Toolbox
{
    /// <summary>
    ///   Offers functionality for the parsing of HTTP headers.
    /// </summary>
    ///
    public interface IHttpHeaderParser
    {
        /// <summary>
        ///   Performs the tasks needed to parse an HTTP Authorization header.
        /// </summary>
        ///
        /// <param name="headerValue">The value of the HTTP authorization header sent with the request.</param>
        /// <param name="headerName">The name of the HTTP authorization header being parsed, if non-standard.</param>
        ///
        /// <returns>The set of authorization members parsed from the header, including the operation received, keyed by the header name.</param>
        ///
        /// <remarks>
        ///   Parsing attempts to adhere to RFCs 7235 (https://tools.ietf.org/html/rfc7235), 7616 (https://tools.ietf.org/html/rfc7616), and
        ///   7617 (https://tools.ietf.org/html/rfc7617) as closely as possible.  Some portions of the RFCs, notably 7616, would require dedicated
        ///   parsing that cannot be expressed generally for the entire header.  Because these portions should not be impactful, they are being
        ///   parsed more loosely than the RFC outlines.  This approach will correctly handle a valid Authorization header, but may produce erroneous
        ///   results if the header is malformed.
        ///
        ///   Parsing Notes:
        ///     <list type="bullet">
        ///       <item>The header name, key names, and key values may contain a dash "-".</item>
        ///       <item>Keys and values must be joined by an equal sign with no preceding or trailing spaces. (e.g. key="value")</item>
        ///       <item>Values may be quoted or unquoted.</item>
        ///       <item>No item may contain a comma "," as part of the key or value, this character is used to delimit entries.</item>
        ///       <item>No item may contain a space " " as part of the key or value, this character is used to delimit entries.</item>
        ///     </list>
        /// </remarks>
        ///
        IReadOnlyDictionary<string, string> ParseAuthorization(string headerValue,
                                                               string headerName);
    }
}

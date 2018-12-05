using System.Text;
using Microsoft.AspNetCore.Authentication;

namespace WebApp_OpenIDConnect_DotNet
{
    /// <summary>
    ///   Serves as a serializer for string-based data into a UTF-8 byte format.
    /// </summary>
    /// 
    /// <seealso cref="Microsoft.AspNetCore.Authentication.IDataSerializer{System.String}" />
    /// 
    public class Utf8ByteSerializer: IDataSerializer<string>
    {
        /// <summary>
        ///   Performs the actions needed to deserialize a UTF-8 byte sequence into its corresponding
        ///   string representation. 
        /// </summary>
        /// 
        /// <param name="data">The data to operate on.</param>
        /// 
        /// <returns>The string represented by the specified <paramref name="data"/></returns>
        /// 
        public string Deserialize(byte[] data) => Encoding.UTF8.GetString(data);

        /// <summary>
        ///   Performs the actions needed to serialize a string into its corresponding UTF-8
        ///   byte sequence.
        /// </summary>
        /// 
        /// <param name="data">The string to operate on.</param>
        /// 
        /// <returns>The UTF-8 byte sequence that represents the specified <paramref name="data"/></returns>
        /// 
        public byte[] Serialize(string data) => Encoding.UTF8.GetBytes(data);
    }
}

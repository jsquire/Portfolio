using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace OrderFulfillment.Api.Security
{
    /// <summary>
    ///   Provides a mapping of identity claims to a certificate thumbprint.
    /// </summary>
    /// 
    public class ClientCertificateClaimsMap
    {
        /// <summary>The set of known thumbprints and the claims to which they are mapped.</summary>        
        private readonly IDictionary<string, IDictionary<string, string>> claimMappings;

        /// <summary>
        ///   Gets the set of claims mappings associated with the specified <paramref name="thumbprint"/>, if it exists.
        /// </summary>
        ///         
        /// <param name="thumbprint">The thumbprint of the certificate to retrieve the mappings for.</param>
        /// 
        /// <returns>If the <paramref name="thumbprint"/> is known, the <see cref="IDictionary{System.String, System.String}"/> containing the claims and associated values; otherwise, <c>null</c>.</returns>
        /// 
        public IDictionary<string, string> this[string thumbprint]
        {
            get 
            { 
                if (String.IsNullOrEmpty(thumbprint))
                {
                    throw new ArgumentNullException(nameof(thumbprint));
                }
                
                if (!this.claimMappings.ContainsKey(thumbprint))
                {
                    return null;
                }

                return this.claimMappings[thumbprint]; 
            }
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="ClientCertificateClaimsMap"/> class.
        /// </summary>
        /// 
        /// <param name="mappings">The set of mappings to use; if <c>null</c>, the set will be initialized as empty.</param>
        /// 
        private ClientCertificateClaimsMap(IDictionary<string, IDictionary<string, string>> mappings)
        {
            this.claimMappings = mappings ?? new Dictionary<string, IDictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="ClientCertificateClaimsMap"/> class.
        /// </summary>
        /// 
        public ClientCertificateClaimsMap() : this(null)
        {
        }

        /// <summary>
        ///   Determines whether the map contains the specified thumbprint.
        /// </summary>
        /// 
        /// <param name="thumbprint">The thumbprint of the certificate to consider.</param>
        /// 
        /// <returns><c>true</c> if the map contains the specified thumbprint; otherwise, <c>false</c>.</returns>
        /// 
        public bool ContainsThumbprint(string thumbprint)
        {
            if (String.IsNullOrEmpty(thumbprint))
            {
                throw new ArgumentNullException(nameof(thumbprint));
            }

            return this.claimMappings.ContainsKey(thumbprint);
        }

        /// <summary>
        ///   Gets the set of known certificate thumbprints that were added.
        /// </summary>
        /// 
        /// <returns>The set of thumprints that are contained by the map; no guarantee is made on whether they are associated with a valid set of mappings.</returns>
        /// 
        public IEnumerable<string> GetCertificateThumbprints()
        {
            return this.claimMappings.Keys;
        }

        /// <summary>
        ///   Adds the specified certificate to the map.
        /// </summary>
        /// 
        /// <param name="thumbprint">The thumbprint of the certificate to add.</param>
        /// <param name="claimsMapping">The set of claim mappings to associate with the certificate.  If <c>null</c>, an empty set will be used.</param>
        /// 
        public void AddCertificate(string                     thumbprint,
                                   Dictionary<string, string> claimsMapping = null)
        {
            if (String.IsNullOrEmpty(thumbprint))
            {
                throw new ArgumentNullException(nameof(thumbprint));
            }

            if (this.claimMappings.ContainsKey(thumbprint))
            {
                throw new ArgumentException("The specified thumbprint already exists in the map", nameof(thumbprint));
            }

            this.claimMappings.Add(thumbprint, claimsMapping ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        }

        /// <summary>
        ///   Serializes this client certificate claims map.
        /// </summary>
        /// 
        /// <returns>The serialized map, in JSON format.</returns>
        /// 
        public string Serialize()
        {
            return JsonConvert.SerializeObject(this.claimMappings);
        }

        /// <summary>
        ///   Deserializes a serialized client certificate claims map.
        /// </summary>
        /// 
        /// <param name="serializedClientCertificateClaimsMap">The serialized client certificate claims map to deserialize.</param>
        /// 
        /// <returns></returns>
        /// 
        public static ClientCertificateClaimsMap Deserialize(string serializedClientCertificateClaimsMap)
        {
            if (String.IsNullOrEmpty(serializedClientCertificateClaimsMap))
            {
                throw new ArgumentNullException(nameof(serializedClientCertificateClaimsMap));
            }

           var deserializedMap = JsonConvert.DeserializeObject<Dictionary<string, IDictionary<string, string>>>(serializedClientCertificateClaimsMap);
           var claimsMap       = new Dictionary<string, IDictionary<string, string>>(deserializedMap, StringComparer.OrdinalIgnoreCase);       
               
           return new ClientCertificateClaimsMap(claimsMap);
        }
    }
}
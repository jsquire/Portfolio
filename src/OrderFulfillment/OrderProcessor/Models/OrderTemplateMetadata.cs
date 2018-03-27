using System;

namespace OrderFulfillment.OrderProcessor.Models
{
    /// <summary>
    ///   Serves as a model for rendering an order line item fragment from a metadata
    ///   template.
    /// </summary>
    /// 
    public class OrderTemplateMetadata
    {
        /// <summary>
        ///   The SKU of the item that the template represents.
        /// </summary>
        /// 
        public string Sku { get;  set; }

        /// <summary>
        ///   The quantity of the line item ordered, across all recipients.
        /// </summary>
        /// 
        public int LineItemCount { get;  set; }

        /// <summary>
        ///   The number of unique recipients that have ordered the line item.
        /// </summary>
        /// 
        public int NumberOfRecipients { get;  set; }

        /// <summary>
        ///   The total number of sheets for the item being rendered.
        /// </summary>
        /// 
        public int TotalSheets { get; set; }

        /// <summary>
        ///   The number of sheets beyond the base sheet count for the item being
        ///   rendered.
        /// </summary>
        /// 
        public int AdditionalSheets { get; set; }

        /// <summary>
        ///   The url that corresponds to the asset associated with the item.
        /// </summary>
        /// 
        public string AssetUrl { get;  set; }
    }
}

using Sitecore.XConnect;
using System;

namespace SUGCON2019Cortex.XConnect.Extension.Model
{
    public class ProductPurchasedOutcome : Outcome
    {
        public int ProductId { get; set; }

        public ProductPurchasedOutcome(Guid definitionId, DateTime timestamp, string currencyCode, decimal monetaryValue)
            : base(definitionId, timestamp, currencyCode, monetaryValue)
        {
        }

        public static Guid CustomPurchaseOutcomeDefinitionId { get; } = new Guid("{19457001-52AF-48DF-ADD1-CD63E156D496}");
    }
}

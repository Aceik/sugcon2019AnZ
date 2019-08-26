using System;
using Sitecore.XConnect;
using System.Collections.Generic;

namespace SUGCON2019Cortex.XConnect.Extension.Model
{
    [FacetKey(DefaultFacetKey)]
    [Serializable]
    public class ProductRecommendationFacet : Facet
    {
        public const string DefaultFacetKey = "ProductRecommendationFacet";

        public List<ProductRecommend> ProductRecommendations { get; set; } = new List<ProductRecommend>();
    }

    public class ProductRecommend
    {
        public long ProductId { get; set; }
        public double Score { get; set; }
    }
}

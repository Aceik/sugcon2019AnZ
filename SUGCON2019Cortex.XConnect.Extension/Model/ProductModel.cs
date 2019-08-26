using Sitecore.XConnect;
using Sitecore.XConnect.Collection.Model;
using Sitecore.XConnect.Schema;

namespace SUGCON2019Cortex.XConnect.Extension.Model
{
    public class ProductModel
    {
        private static XdbModel _model;

        public static XdbModel Model => _model ?? (_model = BuildModel());

        private static XdbModel BuildModel()
        {
            var builder = new XdbModelBuilder("SUGCON2019Cortex.XConnect.Extension.Model", new XdbModelVersion(1, 5));

            builder.ReferenceModel(CollectionModel.Model);
            builder.DefineEventType<ProductPurchasedOutcome>(true);
            builder.DefineFacet<Contact, ProductRecommendationFacet>(ProductRecommendationFacet.DefaultFacetKey);

            return builder.BuildModel();
        }
    }
}

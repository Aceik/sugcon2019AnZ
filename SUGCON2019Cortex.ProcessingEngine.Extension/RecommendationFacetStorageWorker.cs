using Sitecore.Processing.Engine.Abstractions;
using Sitecore.Processing.Engine.Storage.Abstractions;
using Sitecore.XConnect;
using SUGCON2019Cortex.XConnect.Extension.Model;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SUGCON2019Cortex.ProcessingEngine.Extension
{
    public class RecommendationFacetStorageWorker : IDeferredWorker
    {
        public const string OptionTableName = "tableName";
        public const string OptionSchemaName = "schemaName";

        private readonly string _tableName;
        private readonly ITableStore _tableStore;
        private readonly IXdbContext _xdbContext;

        public RecommendationFacetStorageWorker(ITableStoreFactory tableStoreFactory, IXdbContext xdbContext, IReadOnlyDictionary<string, string> options)
        {
            _tableName = options[OptionTableName];
            var schemaName = options[OptionSchemaName];

            _tableStore = tableStoreFactory.Create(schemaName);

            _xdbContext = xdbContext;
        }

        public void Dispose()
        {
            _tableStore.Dispose();
        }

        public async Task RunAsync(CancellationToken token)
        {
            var rows = await _tableStore.GetRowsAsync(_tableName, CancellationToken.None);

            while (await rows.MoveNext())
            {
                foreach (var row in rows.Current)
                {
                    var contactId = row.GetGuid(0);
                    var productId = row.GetInt64(1);
                    var score = row.GetDouble(2);

                    var contact = await _xdbContext.GetContactAsync(contactId,
                        new ContactExpandOptions(ProductRecommendationFacet.DefaultFacetKey));

                    var facet = contact.GetFacet<ProductRecommendationFacet>(ProductRecommendationFacet.DefaultFacetKey) ??
                                new ProductRecommendationFacet();

                    if (facet.ProductRecommendations.All(x => x.ProductId != productId))
                    {
                        facet.ProductRecommendations.Add(new ProductRecommend
                        {
                            ProductId = productId,
                            Score = score
                        });

                        _xdbContext.SetFacet(contact, ProductRecommendationFacet.DefaultFacetKey, facet);
                        await _xdbContext.SubmitAsync(CancellationToken.None);
                    }
                }
            }

            await _tableStore.RemoveAsync(_tableName, CancellationToken.None);
        }
    }
}

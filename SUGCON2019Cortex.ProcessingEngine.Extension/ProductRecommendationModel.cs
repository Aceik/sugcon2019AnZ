using Sitecore.Processing.Engine.ML.Abstractions;
using Sitecore.Processing.Engine.Projection;
using Sitecore.XConnect;
using SUGCON2019Cortex.XConnect.Extension.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SUGCON2019Cortex.ProcessingEngine.Extension
{
    public class ProductRecommendationModel : IModel<Contact>
    {
        public const string OptionTableName = "tableName";
        private readonly string _tableName;

        public ProductRecommendationModel(IReadOnlyDictionary<string, string> options)
        {
            _tableName = options[OptionTableName];
        }

        public IProjection<Contact> Projection =>
            Sitecore.Processing.Engine.Projection.Projection.Of<Contact>().CreateTabular(
                _tableName,
                contact =>
                    contact.Interactions.Select(interaction =>
                        new
                        {
                            Contact = contact,
                            Product = interaction.Events.OfType<ProductPurchasedOutcome>().LastOrDefault()?.ProductId
                        }
                    ).LastOrDefault(),
                cfg => cfg
                    .Key("ContactId", x => x.Contact.Id)
                    .Attribute("ProductId", x => x.Product)
            );

        public Task<IReadOnlyList<object>> EvaluateAsync(string schemaName, CancellationToken cancellationToken, params TableDefinition[] tables)
        {
            throw new NotImplementedException();
        }

        public Task<ModelStatistics> TrainAsync(string schemaName, CancellationToken cancellationToken, params TableDefinition[] tables)
        {
            throw new NotImplementedException();
        }
    }
}

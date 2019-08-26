using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RestSharp;
using Sitecore.Processing.Engine.Abstractions;
using Sitecore.Processing.Engine.Projection;
using Sitecore.Processing.Engine.Storage.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SUGCON2019Cortex.ProcessingEngine.Extension
{
    public class ProductRecommendationWorker : IDeferredWorker
    {
        public const string OptionSourceTableName = "sourceTableName";
        public const string OptionTargetTableName = "targetTableName";
        public const string OptionSchemaName = "schemaName";
        public const string OptionLimit = "limit";

        private readonly ITableStore _tableStore;
        private readonly string _sourceTableName;
        private readonly string _targetTableName;
        private readonly int _limit = 1;

        private ILogger _logger;

        public ProductRecommendationWorker(ITableStoreFactory tableStoreFactory, ILogger logger, IReadOnlyDictionary<string, string> options)
        {
            _sourceTableName = options[OptionSourceTableName];
            _targetTableName = options[OptionTargetTableName];
            _limit = int.Parse(options[OptionLimit]);

            var schemaName = options[OptionSchemaName];
            _tableStore = tableStoreFactory.Create(schemaName);

            _logger = logger;
        }

        public void Dispose()
        {
            _tableStore.Dispose();
        }
        

        public async Task RunAsync(CancellationToken token)
        {
            var sourceRows = await _tableStore.GetRowsAsync(_sourceTableName, CancellationToken.None);
            var targetRows = new List<DataRow>();
            var targetSchema = new RowSchema(
                new FieldDefinition("ContactId", FieldKind.Key, FieldDataType.Guid),
                new FieldDefinition("ProductId", FieldKind.Key, FieldDataType.Int64),
                new FieldDefinition("Score", FieldKind.Attribute, FieldDataType.Double)
            );

            while (await sourceRows.MoveNext())
            {
                foreach (var row in sourceRows.Current)
                {
                    var purchasedProductId = uint.Parse(row["ProductId"].ToString());

                    var recommendations = GetRecommendList(purchasedProductId, _limit);

                    if (recommendations.Any())
                    {
                        for (var i = 0; i < _limit; i++)
                        {
                            var targetRow = new DataRow(targetSchema);
                            targetRow.SetGuid(0, row.GetGuid(0));
                            targetRow.SetInt64(1, recommendations[i].RecommendedItemId);
                            targetRow.SetDouble(2, recommendations[i].Score);

                            targetRows.Add(targetRow);
                        }
                    }
                }
            }

            var tableDefinition = new TableDefinition(_targetTableName, targetSchema);
            var targetTable = new InMemoryTableData(tableDefinition, targetRows);
            await _tableStore.PutTableAsync(targetTable, TimeSpan.FromMinutes(30), CancellationToken.None);
        }

        private List<ProductRecommendResult> GetRecommendList(long productId, int limit)
        {
            var itemList = new List<ProductRecommendResult>();

            try
            {
                var purchasedProductId = productId;
                var client_rs = new RestClient("https://aceiksugcon2019ws.azurewebsites.net/api/models/12125d85-d384-4b50-8d84-fe41211ae578/recommend");
                client_rs.FollowRedirects = false;
                var request = new RestRequest(Method.GET);

                request.AddParameter("itemId", purchasedProductId);
                request.AddParameter("recommendationCount", limit);
                request.AddHeader("cache-control", "no-cache");
                request.AddHeader("x-api-key", "bTN3djc3NXpkc29law==");
                request.AddHeader("Content-Type", "application/json");

                IRestResponse response = client_rs.Execute(request);
                _logger.LogInformation("*****Recommendation Result for product: " + purchasedProductId);
                _logger.LogInformation(response.Content);
                List<ProductRecommendResult> temp_list = JsonConvert.DeserializeObject<List<ProductRecommendResult>>(response.Content);
                itemList = temp_list;
            }
            catch (HttpRequestException exception)
            {
                _logger.LogError("THere was an error", exception);
            }
            return itemList;
        }
    }

    public class ProductRecommendResult
    {
        public long RecommendedItemId { get; set; }
        public double Score { get; set; }
    }
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Sitecore.Processing.Engine.Abstractions;
using Sitecore.Processing.Engine.Agents;
using Sitecore.Processing.Tasks.Options.DataSources.Search;
using Sitecore.Processing.Tasks.Options.Workers.ML;
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
    public class RecommendatioinAgent : RecurringAgent
    {
        private readonly ILogger<IAgent> _logger;
        private readonly ITaskManager _taskManager;
        private readonly IServiceProvider _serviceProvider;

        public RecommendatioinAgent(IConfiguration options, ILogger<IAgent> logger, ITaskManager taskManager, IServiceProvider serviceProvider) : base(options, logger)
        {
            _logger = logger;
            _taskManager = taskManager;
            _serviceProvider = serviceProvider;
        }

        protected override async Task RecurringExecuteAsync(CancellationToken token)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                using (var xdbContext = scope.ServiceProvider.GetService<IXdbContext>())

                {
                    // Register the project, merge, predict, storage tasks here
                    var query = xdbContext.Contacts.Where(contact =>
                        contact.Interactions.Any(interaction =>
                            interaction.Events.OfType<ProductPurchasedOutcome>().Any() &&
                            interaction.StartDateTime > DateTime.UtcNow.AddHours(-1)
                        )
                    );

                    var expandOptions = new ContactExpandOptions
                    {
                        Interactions = new RelatedInteractionsExpandOptions()
                    };

                    query = query.WithExpandOptions(expandOptions);

                    var searchRequest = query.GetSearchRequest();

                    // Task for projection
                    var dataSourceOptions = new ContactSearchDataSourceOptionsDictionary(
                        searchRequest, // searchRequest
                        30, // maxBatchSize
                        50 // defaultSplitItemCount
                    );

                    var projectionOptions = new ContactProjectionWorkerOptionsDictionary(
                        typeof(ProductRecommendationModel).AssemblyQualifiedName, // modelTypeString
                        TimeSpan.FromMinutes(10), // timeToLive
                        "recommendation", // schemaName
                        new Dictionary<string, string> // modelOptions
                        {
                        { ProductRecommendationModel.OptionTableName, "contactProducts" }
                        }
                    );

                    var projectionTaskId = await _taskManager.RegisterDistributedTaskAsync(
                        dataSourceOptions, // datasourceOptions
                        projectionOptions, // workerOptions
                        null, // prerequisiteTaskIds
                        TimeSpan.FromMinutes(10) // expiresAfter
                    );

                    // Task for merge
                    var mergeOptions = new MergeWorkerOptionsDictionary(
                        "contactProductsFinal", // tableName
                        "contactProducts", // prefix
                        TimeSpan.FromMinutes(10), // timeToLive
                        "recommendation" // schemaName
                    );

                    var mergeTaskId = await _taskManager.RegisterDeferredTaskAsync(
                        mergeOptions, // workerOptions
                        new[] // prerequisiteTaskIds
                        {
                    projectionTaskId
                        },
                        TimeSpan.FromMinutes(10) // expiresAfter
                    );

                    // Task for predict
                    var workerOptions = new DeferredWorkerOptionsDictionary(
                    typeof(ProductRecommendationWorker).AssemblyQualifiedName, // workerType
                    new Dictionary<string, string> // options
                    {
                    { ProductRecommendationWorker.OptionSourceTableName, "contactProductsFinal" },
                    { ProductRecommendationWorker.OptionTargetTableName, "contactRecommendations" },
                    { ProductRecommendationWorker.OptionSchemaName, "recommendation" },
                    { ProductRecommendationWorker.OptionLimit, "5" }
                    });

                    var recommendationTaskId = await _taskManager.RegisterDeferredTaskAsync(
                        workerOptions, // workerOptions
                        new[] // prerequisiteTaskIds
                        {
                        mergeTaskId
                        },
                        TimeSpan.FromMinutes(10) // expiresAfter
                    );

                    // Task for storage
                    var storageOptions = new DeferredWorkerOptionsDictionary(
                    typeof(RecommendationFacetStorageWorker).AssemblyQualifiedName, // workerType
                    new Dictionary<string, string> // options
                    {
                    { RecommendationFacetStorageWorker.OptionTableName, "contactRecommendations" },
                    { RecommendationFacetStorageWorker.OptionSchemaName, "recommendation" }
                    });

                    var storeFacetTaskId = await _taskManager.RegisterDeferredTaskAsync(
                        storageOptions, // workerOptions
                        new[] // prerequisiteTaskIds
                        {
                        recommendationTaskId
                        },
                        TimeSpan.FromMinutes(10) // expiresAfter
                    );

                    _logger.LogInformation("***projection task ID: " + projectionTaskId);
                    _logger.LogInformation("***merge task ID: " + mergeTaskId);
                    _logger.LogInformation("***predict task ID: " + recommendationTaskId);
                    _logger.LogInformation("***storage task ID: " + storeFacetTaskId);
                }
            }
        }
    }
}

using Sitecore.DependencyInjection;
using Sitecore.Processing.Engine.Abstractions;
using Sitecore.XConnect;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Extensions.DependencyInjection;
using SUGCON2019Cortex.XConnect.Extension.Model;
using Sitecore.Processing.Tasks.Options.DataSources.Search;
using Sitecore.Processing.Tasks.Options.Workers.ML;
using SUGCON2019Cortex.ProcessingEngine.Extension;
using System.Collections.Generic;
using Sitecore.XConnect.Collection.Model;
using SUGCON2019Cortex.Website.Generators.Contacts;
using Sitecore.XConnect.Client;

namespace SUGCON2019Cortex.Website.Controllers
{
    public class ContactApiController : ApiController
    {
        private IContactGenerator _contactGenerator;

        private static string _contactsource = Sitecore.Configuration.Settings.GetSetting("xConnect.ContactSource");
        private static Guid _channelId = new Guid(Sitecore.Configuration.Settings.GetSetting("xConnect.ChannelId"));
        private static string _userAgent = Sitecore.Configuration.Settings.GetSetting("xConnect.InteractionUserAgent");

        public ContactApiController(IContactGenerator contactGenerator)
        {
            _contactGenerator = contactGenerator;
        }

        [HttpPost]
        public async Task<object> RegisterTasks()
        {
            //In a core role like Content Management, we can retrive Task Manager like below
            var taskManager = ServiceLocator.ServiceProvider.GetService<ITaskManager>();


            using (XConnectClient client
                = Sitecore.XConnect.Client.Configuration.SitecoreXConnectClientConfiguration.GetClient())
            {
                var query = client.Contacts.Where(contact =>
                    contact.Interactions.Any(interaction =>
                        interaction.Events.OfType<ProductPurchasedOutcome>().Any() &&
                        interaction.StartDateTime > DateTime.UtcNow.AddDays(-1)         //Only for outcome happened in past 24 hours
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

                var projectionTaskId = await taskManager.RegisterDistributedTaskAsync(
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

                var mergeTaskId = await taskManager.RegisterDeferredTaskAsync(
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

                var recommendationTaskId = await taskManager.RegisterDeferredTaskAsync(
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

                var storeFacetTaskId = await taskManager.RegisterDeferredTaskAsync(
                    storageOptions, // workerOptions
                    new[] // prerequisiteTaskIds
                    {
                        recommendationTaskId
                    },
                    TimeSpan.FromMinutes(10) // expiresAfter
                );

                return new List<Guid>() { projectionTaskId, mergeTaskId, recommendationTaskId, storeFacetTaskId };
            }
        }

        [HttpGet]
        public async Task<List<RecoResult>> Recommendtions()
        {
            var result = new List<RecoResult>();
            using (XConnectClient client
                = Sitecore.XConnect.Client.Configuration.SitecoreXConnectClientConfiguration.GetClient())
            {
                var query = client.Contacts.Where(contact =>
                    contact.Interactions.Any(interaction =>
                        interaction.Events.OfType<ProductPurchasedOutcome>().Any() &&
                        interaction.StartDateTime > DateTime.UtcNow.AddDays(-1)         //Only for outcome happened in past 24 hours
                    )
                );

                var expandOptions = new ContactExpandOptions (CollectionModel.FacetKeys.PersonalInformation, ProductRecommendationFacet.DefaultFacetKey)
                {
                    Interactions = new RelatedInteractionsExpandOptions()
                };

                query = query.WithExpandOptions(expandOptions);

                var contacts = await query.ToList();

                if (contacts.Any())
                {
                    foreach (var c in contacts)
                    {
                        var purchaseOutcome = c.Interactions.Select(i => i.Events.OfType<ProductPurchasedOutcome>()).FirstOrDefault();
                        var purchasedId = purchaseOutcome.FirstOrDefault().ProductId;
                        var recommendationFacet = c.GetFacet<ProductRecommendationFacet>();

                        var r = new RecoResult()
                        {
                            FirstName = c.Personal()?.FirstName,
                            LastName = c.Personal()?.LastName,
                            PurchasedProductId = purchasedId
                        };

                        if (recommendationFacet != null)
                        {
                            r.Recommends = recommendationFacet.ProductRecommendations;
                        }

                        result.Add(r);
                    }
                }
            }
            return result;
        }

        [HttpGet]
        public async Task<RecoResult> Recommendtion(string id)
        {
            var result = new RecoResult();
            using (XConnectClient client
                = Sitecore.XConnect.Client.Configuration.SitecoreXConnectClientConfiguration.GetClient())
            {
                var reference = new ContactReference(Guid.Parse(id));

                var expandOptions = new ContactExpandOptions(CollectionModel.FacetKeys.PersonalInformation, ProductRecommendationFacet.DefaultFacetKey)
                {
                    Interactions = new RelatedInteractionsExpandOptions()
                };

                Task<Contact> contactTask = client.GetAsync<Contact>(reference, expandOptions);

                Contact contact = await contactTask;

                if(contact != null)
                {
                    result.FirstName = contact.Personal()?.FirstName;
                    result.LastName = contact.Personal()?.LastName;
                    var purchaseOutcome = contact.Interactions.Select(i => i.Events.OfType<ProductPurchasedOutcome>()).FirstOrDefault();
                    result.PurchasedProductId = purchaseOutcome.FirstOrDefault().ProductId;
                    var recommendationFacet = contact.GetFacet<ProductRecommendationFacet>();
                    if (recommendationFacet != null)
                    {
                        result.Recommends = recommendationFacet.ProductRecommendations;
                    }
                }
            }
            return result;
        }

        [HttpPost]
        public RecoResult Contact()
        {
            var result = new RecoResult();

            using (XConnectClient client
                = Sitecore.XConnect.Client.Configuration.SitecoreXConnectClientConfiguration.GetClient())
            {
                var person = _contactGenerator.CreateContact();
                var contactId = new ContactIdentifier(_contactsource, person.Identifier.ToString(), ContactIdentifierType.Known);
                var contact = new Contact(contactId);

                var infoFacet = new PersonalInformation()
                {
                    FirstName = person.FirstName,
                    LastName = person.LastName
                };
                client.SetFacet(contact, PersonalInformation.DefaultFacetKey, infoFacet);

                var emailFacet = new EmailAddressList(new EmailAddress(person.EmailAddress, true), _contactsource);
                client.SetFacet(contact, EmailAddressList.DefaultFacetKey, emailFacet);

                var purchaseOutcome = new ProductPurchasedOutcome(ProductPurchasedOutcome.CustomPurchaseOutcomeDefinitionId, DateTime.UtcNow, "AUD", 0)
                { ProductId = _contactGenerator.GetProductId() };

                var productPurchasedinteraction = new Interaction(contact, InteractionInitiator.Contact, _channelId, _userAgent);
                productPurchasedinteraction.Events.Add(purchaseOutcome);

                client.AddContact(contact);
                client.AddInteraction(productPurchasedinteraction);

                result.FirstName = infoFacet.FirstName;
                result.LastName = infoFacet.LastName;
                result.PurchasedProductId = purchaseOutcome.ProductId;

                client.Submit();
            }

            return result;
        }
    }

    [Serializable]
    public class RecoResult
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public int PurchasedProductId { get; set; }
        public List<ProductRecommend> Recommends { get; set; }
    }
}
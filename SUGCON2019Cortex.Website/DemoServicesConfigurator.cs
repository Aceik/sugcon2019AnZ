using Microsoft.Extensions.DependencyInjection;
using Sitecore.DependencyInjection;
using SUGCON2019Cortex.Website.Controllers;

namespace SUGCON2019Cortex.Website
{
    public class DemoServicesConfigurator : IServicesConfigurator
    {
        public void Configure(IServiceCollection serviceCollection)
        {
            serviceCollection.AddTransient(typeof(ContactApiController));
        }
    }
}
using SUGCON2019Cortex.Website.Model;

namespace SUGCON2019Cortex.Website.Generators.Contacts
{
    public interface IContactGenerator
    {
        Person CreateContact();
        int GetProductId();
    }
}

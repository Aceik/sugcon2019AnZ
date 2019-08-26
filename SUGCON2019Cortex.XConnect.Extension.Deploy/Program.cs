using Sitecore.XConnect.Serialization;
using SUGCON2019Cortex.XConnect.Extension.Model;

namespace SUGCON2019Cortex.XConnect.Extension.Deploy
{
    class Program
    {
        static void Main(string[] args)
        {
            var fileName = ProductModel.Model.FullName + ".json";
            var model = XdbModelWriter.Serialize(ProductModel.Model);
            System.IO.File.WriteAllText(fileName, model);
        }
    }
}

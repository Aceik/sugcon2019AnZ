# SUGCON 2019 Presentation Demo - Using Sitecore Cortext #

The current solution is to get Cortex working with the ML model we found so far. The workflow is like below:

* User purchased a product, a custom outcome triggered via code and the purchased product id is saved with the contact in xDB. This is done by creating contact and outcome using script.
* The processing engine registered with Projection, Merge, Predict and Storage, 4 tasks
 * Projection task is to retrieve data from xDB (which is very easy as xDB support is Cortex's OOTB), create tabular datasource for next step.
 * We can use default merge task as Sitecore doc recommended.
 * In the Predict task, we use a trained ML model to predict product recommendations with each product id in datasource
 * In Storage task, we save the recommendation back to the contact's custom facet.
* We display the recommendation on a webpage or in the application console.

## Deployment Instructions ##

* Checkout the code
* Make sure you opened the Visual Studio using administrator permission
* In [Git Root], copy "publishsettings.targets.user.example" and rename it to "publishsettings.targets.user", edit the "publishUrl" value with your own local Sitecore URL
* Build the solution

### Deploy the Custom Model for XConnect ###

* Go to [Git Root]\SUGCON2019Cortex.XConnect.Extension.Deploy\bin\Debug, double click "SUGCON2019Cortex.XConnect.Extension.Deploy.exe" to generate the model JSON file
* Copy the generated model JSON file to [XConnect WebRoot]/App_Data/Models
* Copy the generated model JSON file to [XConnect WebRoot]/App_Data/jobs/continuous/ProcessingEngine/App_Data/Models
* Copy the generated model JSON file to [XConnect WebRoot]/App_Data/jobs/continuous/IndexWorker/App_data/Models
* Copy SUGCON2019Cortex.XConnect.Extension.dll to [XConnect WebRoot]/App_Data/jobs/continuous/AutomationEngine
* Copy [Git Root]\SUGCON2019Cortex.XConnect.Extension.Deploy\sc.XConnect.SUGCON2019Cortex.XConnect.ProductModel.xml to [XConnect WebRoot]/App_data/jobs/continuous/AutomationEngine/App_Data/config/sitecore/XConnect
* Copy [Git Root]\SUGCON2019Cortex.XConnect.Extension.Deploy\sc.XConnect.SUGCON2019Cortex.ProcessingEngine.ProductModel.xml to [XConnect WebRoot]/App_data/jobs/continuous/ProcessingEngine/App_Data/config/sitecore/XConnect


### Deploy Custom Tasks for Cortex Processing Engine ###

* Go to [Git Root]\SUGCON2019Cortex.ProcessingEngine.Extension
* copy "\App_Data\Config\Sitecore\Processing\sc.SUGCON2019Cortex.ProcessingEngine.Extension.xml" to ProcessingEngine\App_Data\Config\Sitecore\Processing
* copy "SUGCON2019Cortex.ProcessingEngine.Extension.dll" and "SUGCON2019Cortex.XConnect.Extension.dll" from SUGCON2019Cortex.ProcessingEngine.Extension project Debug folder to "ProcessingEngine"
* copy "RestSharp.dll" to "ProcessingEngine" root folder

### Deploy Website ###

* Publish the "SUGCON2019Cortex.Website" project to your local Sitecore instance
* Login to Sitecore, install the [Git Root]\custom-outcome.zip 
* After installation, go to Control Panel in Sitecore, choose "deploy marketing definition" and only choose "outcomes" to deploy
* Check your windows services: Sitecore Marketing Automation Engine, Sitecore Processing Engine, Sitecore XConnect Search Indexer, they are all running

## Run the Demo ##

* Go to Https://[web host]/demopage.html
* Use "Create Contact" button to create dummy contact with purchased product id in the custom purchase outcome.
* Use "Register Task" button to register Cortex tasks(Projection, Merge, Predict, Storage).
* Use contact ID with "Show Recommendation" button to retrieve recommendations for one specific contact.
* Use "Show All Recommendations" sections to display all the recently created contact's product recommendations.

## Tips ##

* if run into "The HTTP response was not successful: Forbidden", see https://stackoverflow.com/a/35001970
* do not use comment out in "sc.blahblah.xml" config files, this would cause the windows service not able to start
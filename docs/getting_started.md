# Documentation

## Getting Started and Pre-requesites 
---
* Before being able to use this library you will have to set up cloud accounts with the service providers you would like to work with
    * [Amazon Signup](https://portal.aws.amazon.com/billing/signup#/start)
    * [Google Signup](https://console.cloud.google.com/freetrial?_ga=2.206345818.-877186005.1527773985&page=0)
    * [IBM Signup](https://console.bluemix.net/registration/)
    * [Azure Signup](https://azure.microsoft.com/en-us/free/search/?&OCID=AID719825_SEM_Rp5NDjdF&lnkd=Google_Azure_Brand&gclid=CjwKCAjwj4zaBRABEiwA0xwsP8Qjmx-Uprcaaj8ac3MUixgQ5HCHzYKzx8AOTwaVRNHGPiEx7FTnRBoCKMwQAvD_BwE&dclid=CL_6ku-1ktwCFZBENwodoTgJHw)

* After creating your account(s) you must get access to credentials for whatever library-supported cloud service you wish to use
    
    * List of services from each Provider
        * Speech to Text
            * Amazon : [Amazon Transcribe](https://aws.amazon.com/transcribe/?nc2=h_m1)
            * Google : [Google Speech API](https://cloud.google.com/speech-to-text/)
            * IBM : [Watson Speech to Text API](https://www.ibm.com/watson/services/speech-to-text/) 
            * Azure Speech : [Microsoft Cognitive Services Speech API](https://azure.microsoft.com/en-us/services/cognitive-services/speech-to-text/) 
            * Azure Video Indexer : [Microsoft Cognitive Services Speech API](https://azure.microsoft.com/en-us/services/cognitive-services/video-indexer/)
        * Video Analysis
            * Coming soon...
---
## Obtaining Authentication Credentials
---
### Speech to Text Instructions
---

* **Amazon**
    * For AWS you will need to obtain an Access Key, Secret Access Key, and know what region your aws account is located in
        1. To obtain this information first sign into your AWS account, this should place you at the Amazon Web Service Console
        2. From here click your account name in the top right hand corner and navigate to the My Security Credentials tab
        3. Here you will see an Access Keys section, under this section create a new pair of keys and store the information somewhere secure.  
        ( ***You will only be able to see your access key NOT your secret access key from this view, you must generate another set of keys to view it*** )
        4. To figure out the possible regions you can use for the transcribe service navigate to amazon transcribe under the services tab at the top of your console, in the top right next to your account you may choose any highlighted region.

* **Google**
    * For google you will need to obtain and store a credential file on your computer and know the path to its location along with the project name .
        1. After logging into your cloud account go to the following [documentation.](https://cloud.google.com/speech-to-text/docs/quickstart-client-libraries)
        2. Click "Set up a project" ( ***Remember what you named it*** ) and follow the steps listed underneath the button to obtain a json file with your credentials
        4. Save this file in a known location on your computer and use the absolute path to this file along with the project name to authenticate with the library.

* **IBM**
    * For IBM you will need to obtain a username and password given to you when you create an instance of the watson service.
        1. Sign into your newly created IBM cloud account and navigate to the dashboard.
        2. From here click the dropdown in the top left and click on watson, browse services, and find the speech to text api.
        3. Click create and navigate to the service from your dashboard to view your credentials

* **Azure Speech Service - Azure Basic Offering**
    * For Azure Speech you will need to obtain a subscription key and the region endpoint of the service instance there are two ways to do this
        * Free Trial way
            1. Click this [link](https://azure.microsoft.com/en-us/try/cognitive-services/) and click the Get API Key button underneath the Speech APIs tab for the Speech services preview
        * Pairing with Azure subscription
            1. Login to your Azure portal/dashboard and click create a resource in the top left
            2. In the search bar of the new tab type "speech" and press enter
            3. Choose the Speech (Preview) resource from the given list of resources
            4. Fill in the required information to generate a speech service instance in your azure cloud enviornment
            5. If you view all your resources you should now be able to see the service instance and the region name next to it, by clicking on this and selecting the keys section you should be able to see your API keys.

* **Azure Video Indexer Service - Azure Advanced Offering (recommended)** 
    * For Azure Indexer you will need to obtain an API subscription key ( you will be given two both work individually ), a video indexer portal account id (create an account), and know the location of your API/service.
        1. Click [here](https://api-portal.videoindexer.ai/) to navigate to the video indexer API interface
        2. Login using your azure credentials and click on the PRODUCTS tab on the top taskbar.
        3. Then click on authorization and click the Subscribe button, after choosing your subscription you can view your two API subscription keys
        4. After obtaining your subcription keys navigate to [this page](https://www.videoindexer.ai/) and login using your azure account credentials again
        5. After logging in you will be able to view your account ID and location in the taskbar at the top of the page.

---
## Using your newly Obtained Credentials to authenticate within the library
---
In "app.config" change the "value" field on the given config properties ( ***using the credentials obtained from the above instructions.*** )

For example, the location for azureAdvanced is filled in below, while the other values have yet to be filled in...

```  
<awsConfig>
    <add key="accessKey" value="" />
    <add key="secretAccessKey" value="" />
    <add key="region" value="" />
  </awsConfig>

  <googleConfig>
    <add key="pathToCredentialsFile" value="" />
    <add key="projectId" value="" />
  </googleConfig>

  <ibmConfig>
    <add key="username" value="" />
    <add key="password" value="" />
  </ibmConfig>

  <azureConfig>
    <add key="subscriptionKey" value="" />
    <add key="region" value="" />
  </azureConfig>

  <azureAdvanced>
    <add key="subscriptionKey" value="" />
    <add key="accountId" value="" />
    <add key="location" value="trial" />
  </azureAdvanced>
```
---
## Using the library within your applications
---
- Install the NuGet Package AiPoweredTools.Speech and update all packages associated to their latest versions.
- After Installing and updating, add a reference to the library within your project. 
- Finally getting to the code, Instantiate a Speech client like so...
``` 
var client = new SpeechClient( ServiceType.<Service Name Here> ); 
```
- The default destination for transcriptions is within the debug folder of the currently running process, but can be changed by changing the value of the TranscriptDestination property on a speechclient object like so...
```
client.FileDestination = @" <Some Directory Path> ";
```
- You can now translate .wav files by passing into the Translate() function an IEnumerable list of paths to the files you want translated as such...

```
var fileList = new List<string>
{
    @" <Path to First File> ",
    @" <Path to Second File> ",
    @" <Path to Third File> ",
};

client.Translate(fileList);
```
- If at any time you want to switch which service provider you are using you can simply call the SwitchService() function and it will keep your customized File Destination as long as you use the same speech client...
```
client.SwitchService( ServiceType.<Service Name Here> );
```
---
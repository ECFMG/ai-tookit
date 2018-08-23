using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Security.Authentication;
using System.Text;
using AiPoweredTools.SpeechToText;
using Amazon;
using Amazon.S3;
using Amazon.TranscribeService;
using Google.Cloud.Storage.V1;

[assembly: InternalsVisibleTo("AiToolsTesting")]

namespace AiPoweredTools
{
    internal static class Factory // Auth class implementation/Extension here? Can instantiate basic container variables and use them for all switch statements? 
    {

        #region SpeechServices

        internal static ISpeechServiceStrategy CreateService(SpeechServiceType type)
        {
            switch (type)
            {
                case SpeechServiceType.aws:
                    string accessKey;
                    string secretAccessKey;
                    string regionEndpointName;
                    if (ConfigurationManager.GetSection("awsConfig") is NameValueCollection awsSettings)
                    {
                        accessKey = awsSettings["accessKey"];
                        secretAccessKey = awsSettings["secretAccessKey"];
                        regionEndpointName = awsSettings["region"];
                    }
                    else
                    {
                        throw new InvalidCredentialException();
                    }
                    var s3Client = new AmazonS3Client(accessKey, secretAccessKey, RegionEndpoint.GetBySystemName(regionEndpointName));
                    var atClient = new AmazonTranscribeServiceClient(accessKey, secretAccessKey, RegionEndpoint.GetBySystemName(regionEndpointName));
                    var awsBucketName = Guid.NewGuid().ToString();
                    return new Aws(s3Client, atClient, awsBucketName);


                case SpeechServiceType.google:
                    string credentialPath;
                    string projectId;
                    if (ConfigurationManager.GetSection("googleConfig") is NameValueCollection googleSettings)
                    {
                        credentialPath = googleSettings["pathToCredentialsFile"];
                        projectId = googleSettings["projectId"];
                        if (Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS") == null)
                        {
                            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", credentialPath);
                        }
                    }
                    else
                    {
                        throw new InvalidCredentialException();
                    }
                    var googleClient = StorageClient.Create();
                    var googBucketName = Guid.NewGuid().ToString();
                    return new Goog( googleClient, googBucketName,  projectId);


                case SpeechServiceType.ibm:
                    string username;
                    string password;
                    if (ConfigurationManager.GetSection("ibmConfig") is NameValueCollection ibmSettings)
                    {
                        username = ibmSettings["username"];
                        password = ibmSettings["password"];
                    }
                    else
                    {
                        throw new InvalidCredentialException();
                    }
                    var authBytes = Encoding.UTF8.GetBytes(username + ":" + password);
                    var base64EncryptedCredentials = Convert.ToBase64String(authBytes);
                    var ibmClient = new HttpClient();
                    ibmClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64EncryptedCredentials);
                    return new Ibm(ibmClient);


                case SpeechServiceType.azure:
                    string apiKey;
                    string apiRegion;
                    if (ConfigurationManager.GetSection("azureConfig") is NameValueCollection azureSettings)
                    {
                        apiKey = azureSettings["subscriptionKey"];
                        apiRegion = azureSettings["region"];
                    }
                    else
                    {
                        throw new InvalidCredentialException();
                    }
                    return new Azure(apiKey, apiRegion);


                case SpeechServiceType.azureAdv:
                    string subscriptionKey;
                    string accountId;
                    string location;
                    if (ConfigurationManager.GetSection("azureAdvanced") is NameValueCollection azureAdvSettings)
                    {
                        subscriptionKey = azureAdvSettings["subscriptionKey"];
                        accountId = azureAdvSettings["accountId"];
                        location = azureAdvSettings["location"];
                    }
                    else
                    {
                        throw new InvalidCredentialException();
                    }
                    return new AzureAdvanced(subscriptionKey, accountId, location);


                default:
                    throw new NotSupportedException();
            }
        }

        #endregion

    }
}
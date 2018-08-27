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
            string secretOne;
            string secretTwo;
            string secretThree;

            switch (type)
            {
                case SpeechServiceType.aws:
                    if (ConfigurationManager.GetSection("awsConfig") is NameValueCollection awsSettings)
                    {
                        secretOne = awsSettings["accessKey"];
                        secretTwo = awsSettings["secretAccessKey"];
                        secretThree = awsSettings["region"];
                    }
                    else
                    {
                        throw new InvalidCredentialException();
                    }
                    var s3Client = new AmazonS3Client(secretOne, secretTwo, RegionEndpoint.GetBySystemName(secretThree));
                    var atClient = new AmazonTranscribeServiceClient(secretOne, secretTwo, RegionEndpoint.GetBySystemName(secretThree));
                    var awsBucketName = Guid.NewGuid().ToString();
                    return new Aws(s3Client, atClient, awsBucketName);


                case SpeechServiceType.google:
                    if (ConfigurationManager.GetSection("googleConfig") is NameValueCollection googleSettings)
                    {
                        secretOne = googleSettings["pathToCredentialsFile"];
                        secretTwo = googleSettings["projectId"];
                        if (Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS") == null)
                        {
                            Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", secretOne);
                        }
                    }
                    else
                    {
                        throw new InvalidCredentialException();
                    }
                    var googleClient = StorageClient.Create();
                    var googBucketName = Guid.NewGuid().ToString();
                    return new Goog( googleClient, googBucketName, secretTwo);


                case SpeechServiceType.ibm:
                    if (ConfigurationManager.GetSection("ibmConfig") is NameValueCollection ibmSettings)
                    {
                        secretOne = ibmSettings["username"];
                        secretTwo = ibmSettings["password"];
                    }
                    else
                    {
                        throw new InvalidCredentialException();
                    }
                    var authBytes = Encoding.UTF8.GetBytes(secretOne + ":" + secretTwo);
                    var base64EncryptedCredentials = Convert.ToBase64String(authBytes);
                    var ibmClient = new HttpClient();
                    ibmClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", base64EncryptedCredentials);
                    return new Ibm(ibmClient);


                case SpeechServiceType.azure:
                    if (ConfigurationManager.GetSection("azureConfig") is NameValueCollection azureSettings)
                    {
                        secretOne = azureSettings["subscriptionKey"];
                        secretTwo = azureSettings["region"];
                    }
                    else
                    {
                        throw new InvalidCredentialException();
                    }
                    return new Azure(secretOne, secretTwo);


                case SpeechServiceType.azureAdv:
                    if (ConfigurationManager.GetSection("azureAdvanced") is NameValueCollection azureAdvSettings)
                    {
                        secretOne = azureAdvSettings["subscriptionKey"];
                        secretTwo = azureAdvSettings["accountId"];
                        secretThree = azureAdvSettings["location"];
                    }
                    else
                    {
                        throw new InvalidCredentialException();
                    }
                    return new AzureAdvanced(secretOne, secretTwo, secretThree);


                default:
                    throw new NotSupportedException();
            }
        }

        #endregion

    }
}
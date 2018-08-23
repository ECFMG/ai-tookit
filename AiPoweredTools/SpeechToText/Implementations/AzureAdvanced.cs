using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NAudio.Wave;

namespace AiPoweredTools.SpeechToText
{
    internal sealed class AzureAdvanced : ISpeechServiceStrategy, IAudioFileUtility
    {
        public string TranscriptDestination { get; set; }

        private readonly string _subscriptionKey;
        private readonly string _accountId;
        private readonly string _location;

        public AzureAdvanced(string subscriptionKey, string accountId, string location)
        {
            _subscriptionKey = subscriptionKey;
            _accountId = accountId;
            _location = location;
        }

        public void ConvertToText(IEnumerable<string> listOfFilesToTranscribe)
        {
            var threadCount = 0;
            const int maxFilesProcessedAtATime = 10;
            var taskManager = new List<Task>();

            foreach (var filePath in listOfFilesToTranscribe)
            {
                threadCount++;
                var nextTask = AzureAdvImplementation(filePath);
                taskManager.Add(nextTask);

                if (threadCount != maxFilesProcessedAtATime) continue;
                Task.WaitAll(taskManager.ToArray());
                threadCount = 0;
                taskManager.Clear();
            }
            Task.WaitAll(taskManager.ToArray());

        }

        private async Task AzureAdvImplementation(string filePath)
        {
            const string apiUrl = "https://api.videoindexer.ai";

            CheckValidityAndReturnFileInfo(filePath);

            System.Net.ServicePointManager.SecurityProtocol = System.Net.ServicePointManager.SecurityProtocol | System.Net.SecurityProtocolType.Tls12;

            // create the http client
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false
            };
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);

            // obtain account access token
            var accountAccessTokenRequestResult = await client.GetAsync($"{apiUrl}/auth/{_location}/Accounts/{_accountId}/AccessToken?allowEdit=true");
            var accountAccessToken = accountAccessTokenRequestResult.Content.ReadAsStringAsync().Result.Replace("\"", "");

            // upload a video
            var content = new MultipartFormDataContent();
            // get the video from URL
            var videoPath = filePath; // replace with the video URL

            // as an alternative to specifying video URL, you can upload a file.
            // remove the videoUrl parameter from the query string below and add the following lines:
            var video = File.OpenRead(videoPath);
            var buffer = new byte[video.Length];
            video.Read(buffer, 0, buffer.Length);
            content.Add(new ByteArrayContent(buffer));

            var uploadRequestResult = await client.PostAsync($"{apiUrl}/{_location}/Accounts/{_accountId}/Videos?accessToken={accountAccessToken}&name=some_name&description=some_description&privacy=private&partition=some_partition&videoUrl=", content);
            var uploadResult = uploadRequestResult.Content.ReadAsStringAsync().Result;

            // get the video id from the upload result
            var videoId = JsonConvert.DeserializeObject<dynamic>(uploadResult)["id"];

            // obtain video access token
            var videoTokenRequestResult = await client.GetAsync($"{apiUrl}/auth/{_location}/Accounts/{_accountId}/Videos/{videoId}/AccessToken?allowEdit=true");
            var videoAccessToken = videoTokenRequestResult.Content.ReadAsStringAsync().Result.Replace("\"", "");

            // wait for the video index to finish
            while (true)
            {
                Thread.Sleep(10000);

                var videoGetIndexRequestResult = await client.GetAsync($"{apiUrl}/{_location}/Accounts/{_accountId}/Videos/{videoId}/Index?accessToken={videoAccessToken}&language=English");
                var videoGetIndexResult = await videoGetIndexRequestResult.Content.ReadAsStringAsync();

                var processingState = JsonConvert.DeserializeObject<dynamic>(videoGetIndexResult)["state"];

                // job is finished
                if (processingState != "Uploaded" && processingState != "Processing")
                {
                    break;
                }
            }

            // get the transcript
            var uri = $"{apiUrl}/{_location}/Accounts/{_accountId}/Videos/{videoId}/Captions?accessToken={accountAccessToken}&format=vtt&language=English";

            var response = await client.GetAsync(uri);
            var responseAsText = await response.Content.ReadAsStringAsync();

            //parsing just the transcript
            if (!Directory.Exists(TranscriptDestination))
            {
                Directory.CreateDirectory(TranscriptDestination);
            }

            if (File.Exists(TranscriptDestination + "azureadv_" + Path.GetFileNameWithoutExtension(videoPath) + ".txt"))
            {
                File.Delete(TranscriptDestination + "azureadv_" + Path.GetFileNameWithoutExtension(videoPath) + ".txt");
            }

            using (var reader = new StringReader(responseAsText))
            {
                using (var writer = new StreamWriter(TranscriptDestination + "azureadv_" + Path.GetFileNameWithoutExtension(videoPath) + ".txt"))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (!line.Contains("-->")) continue;
                        line = reader.ReadLine();
                        if (line != null)
                        {
                            writer.WriteLine(line);
                        }
                    }
                }
            }

            //Delete video from indexer interface
            const string deleteReqRoot = @"https://api.videoindexer.ai/";
            await client.DeleteAsync($"{deleteReqRoot}{_location}/Accounts/{_accountId}/Videos/{videoId}?accessToken={accountAccessToken}");
            client.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");
        }

        public Dictionary<string, int> CheckValidityAndReturnFileInfo(string filePath)
        {
            var info = new Dictionary<string, int>();
            string codec;
            int sampleRate;
            var filetype = Path.GetExtension(filePath);
            switch (filetype)
            {
                case ".flv":
                    codec = "flv";
                    using (var reader = new WaveFileReader(filePath))
                    {
                        sampleRate = reader.WaveFormat.SampleRate;
                        if (sampleRate < 8000 && sampleRate > 48000) { throw new FileLoadException(); }
                    }
                    info.Add(codec, sampleRate);
                    return info;
                case ".mxf":
                    codec = "mxf";
                    using (var reader = new WaveFileReader(filePath))
                    {
                        sampleRate = reader.WaveFormat.SampleRate;
                        if (sampleRate < 8000 && sampleRate > 48000) { throw new FileLoadException(); }
                    }
                    info.Add(codec, sampleRate);
                    return info;
                case ".gxf":
                    codec = "gxf";
                    using (var reader = new WaveFileReader(filePath))
                    {
                        sampleRate = reader.WaveFormat.SampleRate;
                        if (sampleRate < 8000 && sampleRate > 48000) { throw new FileLoadException(); }
                    }
                    info.Add(codec, sampleRate);
                    return info;
                case ".wmv":
                    codec = "wmv";
                    using (var reader = new WaveFileReader(filePath))
                    {
                        sampleRate = reader.WaveFormat.SampleRate;
                        if (sampleRate < 8000 && sampleRate > 48000) { throw new FileLoadException(); }
                    }
                    info.Add(codec, sampleRate);
                    return info;
                case ".asf":
                    codec = "asf";
                    using (var reader = new WaveFileReader(filePath))
                    {
                        sampleRate = reader.WaveFormat.SampleRate;
                        if (sampleRate < 8000 && sampleRate > 48000) { throw new FileLoadException(); }
                    }
                    info.Add(codec, sampleRate);
                    return info;
                case ".avi":
                    codec = "avi";
                    using (var reader = new WaveFileReader(filePath))
                    {
                        sampleRate = reader.WaveFormat.SampleRate;
                        if (sampleRate < 8000 && sampleRate > 48000) { throw new FileLoadException(); }
                    }
                    info.Add(codec, sampleRate);
                    return info; 
                case ".mp4":
                    codec = "mp4";
                    using (var reader = new WaveFileReader(filePath))
                    {
                        sampleRate = reader.WaveFormat.SampleRate;
                        if (sampleRate < 8000 && sampleRate > 48000) { throw new FileLoadException(); }
                    }
                    info.Add(codec, sampleRate);
                    return info;
                case ".m4a":
                    codec = "m4a";
                    using (var reader = new WaveFileReader(filePath))
                    {
                        sampleRate = reader.WaveFormat.SampleRate;
                        if (sampleRate < 8000 && sampleRate > 48000) { throw new FileLoadException(); }
                    }
                    info.Add(codec, sampleRate);
                    return info;
                case ".m4v":
                    codec = "m4v";
                    using (var reader = new WaveFileReader(filePath))
                    {
                        sampleRate = reader.WaveFormat.SampleRate;
                        if (sampleRate < 8000 && sampleRate > 48000) { throw new FileLoadException(); }
                    }
                    info.Add(codec, sampleRate);
                    return info;
                case ".isma":
                    codec = "isma";
                    using (var reader = new WaveFileReader(filePath))
                    {
                        sampleRate = reader.WaveFormat.SampleRate;
                        if (sampleRate < 8000 && sampleRate > 48000) { throw new FileLoadException(); }
                    }
                    info.Add(codec, sampleRate);
                    return info;
                case ".ismv":
                    codec = "ismv";
                    using (var reader = new WaveFileReader(filePath))
                    {
                        sampleRate = reader.WaveFormat.SampleRate;
                        if (sampleRate < 8000 && sampleRate > 48000) { throw new FileLoadException(); }
                    }
                    info.Add(codec, sampleRate);
                    return info;
                case ".dvr-ms":
                    codec = "dvr-ms";
                    using (var reader = new WaveFileReader(filePath))
                    {
                        sampleRate = reader.WaveFormat.SampleRate;
                        if (sampleRate < 8000 && sampleRate > 48000) { throw new FileLoadException(); }
                    }
                    info.Add(codec, sampleRate);
                    return info;
                case ".mkv":
                    codec = "mkv";
                    using (var reader = new WaveFileReader(filePath))
                    {
                        sampleRate = reader.WaveFormat.SampleRate;
                        if (sampleRate < 8000 && sampleRate > 48000) { throw new FileLoadException(); }
                    }
                    info.Add(codec, sampleRate);
                    return info;
                case ".wav":
                    codec = "Wav";
                    using (var reader = new WaveFileReader(filePath))
                    {
                        sampleRate = reader.WaveFormat.SampleRate;
                        if (sampleRate < 8000 && sampleRate > 48000) { throw new FileLoadException(); }
                    }
                    info.Add(codec, sampleRate);
                    return info;
                case ".mov":
                    codec = "mov";
                    using (var reader = new WaveFileReader(filePath))
                    {
                        sampleRate = reader.WaveFormat.SampleRate;
                        if (sampleRate < 8000 && sampleRate > 48000) { throw new FileLoadException(); }
                    }
                    info.Add(codec, sampleRate);
                    return info;
                default:
                    throw new FileLoadException();
            }
        }
    }
}
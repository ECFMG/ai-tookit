using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NAudio.Wave;

namespace AiPoweredTools.SpeechToText
{
    internal sealed class Ibm :  ISpeechServiceStrategy, IAudioFileUtility
    {
        public string TranscriptDestination { get; set; }

        private readonly HttpClient _ibmclient;
        private const string RequestAddress = @"https://stream.watsonplatform.net/speech-to-text/api/v1/recognitions";

        public Ibm(HttpClient ibmClient)
        {
            _ibmclient = ibmClient;
        }

        public void ConvertToText(IEnumerable<string> listOfFilesToTranscribe)
        {
            var threadCount = 0;
            const int maxFilesProcessedAtATime = 10;
            var taskManager = new List<Task>();

            foreach (var file in listOfFilesToTranscribe)
            {
                threadCount++;
                var nextTask = ProcessFile(file);
                taskManager.Add(nextTask);

                if (threadCount != maxFilesProcessedAtATime) continue;
                Task.WaitAll(taskManager.ToArray());
                threadCount = 0;
                taskManager.Clear();
            }
            Task.WaitAll(taskManager.ToArray());
        }

        private async Task ProcessFile(string filePath)
        {
            var initialResponse = await SendRequest(filePath);
            var finalResponse = await PollUntilComplete(initialResponse);
            await ProcessAndCopyLocally(finalResponse, filePath);    
        }

        private async Task<HttpResponseMessage> SendRequest(string filePath)
        {
            var fileBytes = File.ReadAllBytes(filePath);
            var mediaType = CheckAndReturnFileType(filePath);
            var byteContent = new ByteArrayContent(fileBytes);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
            var response = await _ibmclient.PostAsync(RequestAddress, byteContent);
            return response;
        }

        private async Task<HttpResponseMessage> PollUntilComplete(HttpResponseMessage initialResponse)
        {
            //Get job id from initial request
            var recievedContent = await initialResponse.Content.ReadAsStringAsync();
            JToken initialJson = JObject.Parse(recievedContent);
            var id = (string)initialJson.SelectToken("id");
            var callbackurl = new Uri(RequestAddress + "/" + id);

            //Polling
            string status;
            HttpResponseMessage statusCheckResponse;
            do
            {
                statusCheckResponse = await _ibmclient.GetAsync(callbackurl);
                var jsonAsString = await statusCheckResponse.Content.ReadAsStringAsync();
                JToken statusCheckJson = JObject.Parse(jsonAsString);
                status = (string)statusCheckJson.SelectToken("status");
                await Task.Delay(5000); // 5s of time between checks
            } while (status != "completed");

            return statusCheckResponse;
        }

        private async Task ProcessAndCopyLocally(HttpResponseMessage finalResponse, string filePath)
        {
            var recievedContent = await finalResponse.Content.ReadAsStringAsync();
            var finalTranscriptText="";

            dynamic formattedString = JsonConvert.DeserializeObject(recievedContent);

            foreach (var x in formattedString.results[0].results) //for every transcript recieved
            {
                string appendedText = x.alternatives[0].transcript;
                finalTranscriptText = finalTranscriptText + appendedText;
            }

            if (!Directory.Exists(TranscriptDestination))
            {
                Directory.CreateDirectory(TranscriptDestination);
            }

            if (File.Exists(TranscriptDestination + "ibm_" + Path.GetFileNameWithoutExtension(filePath) + ".txt"))
            {
                File.Delete(TranscriptDestination + "ibm_" + Path.GetFileNameWithoutExtension(filePath) + ".txt");
            }

            File.WriteAllText(TranscriptDestination + "ibm_" + Path.GetFileNameWithoutExtension(filePath) + ".txt", finalTranscriptText);
        }

        public string CheckAndReturnFileType(string filePath)
        {
            var filetype = Path.GetExtension(filePath);
            switch (filetype)
            {
                case ".wav":
                    return "audio/wav";
                case ".mp3":
                    return "audio/mp3";
                case ".flac":
                    return "audio/flac";
                case ".ogg":
                    return "audio/ogg";
                case ".pcm":
                    return "audio/l16";
                case ".mkv":
                    return "audio/webm";
                default:
                    throw new FileLoadException();
            }
        }

        public int GetSampleRateInHertz(string filePath)
        {
            using (var reader = new WaveFileReader(filePath))
            {
                var bitrate = reader.WaveFormat.SampleRate;
                if (bitrate > 8000 && bitrate < 48000) { return bitrate; }
                throw new FileLoadException();
            }
        }

        public Dictionary<string, int> CheckValidityAndReturnFileInfo(string filePath)
        {
            var info = new Dictionary<string, int>();
            return info;
        }
    }
}
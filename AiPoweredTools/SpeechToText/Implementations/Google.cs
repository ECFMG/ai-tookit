using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Google.Cloud.Speech.V1;
using Google.Cloud.Storage.V1;
using NAudio.Wave;

namespace AiPoweredTools.SpeechToText
{
    internal sealed class Goog : ISpeechServiceStrategy, IAudioFileUtility
    {
        public string TranscriptDestination { get; set; }
        
        private readonly StorageClient _client;
        private readonly string _pathToCredentials;
        private readonly string _bucketName;
        private readonly string _projectId;
        private readonly string _envName;

        public Goog(StorageClient googleClient, string bucketName, string projectId)
        {
            _client = googleClient;
            _bucketName = bucketName;
            _projectId = projectId;
        }

        public void ConvertToText(IEnumerable<string> listOfFilesToTranscribe)
        {

            var threadCount = 0;
            const int maxFilesProcessedAtATime = 10;
            var taskManager = new List<Task>();

            SetupClientAndBucketAsync().Wait();

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

            DeleteBucketAsync().Wait();
        }

        private async Task ProcessFile(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            await UploadFileAsync(filePath, fileName);
            await ListAndTranscribeFileAsync(filePath, fileName);
            await DeleteFileAsync(fileName);
        }

        private async Task SetupClientAndBucketAsync()
        {
            //Creation of container
            await _client.CreateBucketAsync(_projectId, _bucketName);
        }

        private async Task UploadFileAsync(string filePath, string fileName)
        {
            //Upload File to Container
            using (var f = File.OpenRead(filePath))
            {
                await _client.UploadObjectAsync(_bucketName, fileName, null, f);
            }

        }

        private async Task ListAndTranscribeFileAsync(string filePath, string fileName)
        {

            if (!Directory.Exists(TranscriptDestination))
            {
                Directory.CreateDirectory(TranscriptDestination);
            }

            if (File.Exists(TranscriptDestination + "google_" + fileName + ".txt"))
            {
                File.Delete(TranscriptDestination + "google_" + fileName + ".txt");
            }

            foreach (var obj in _client.ListObjects(_bucketName))
            {
                var rootBucketPath = "gs://" + _bucketName + "/";
                var storageUri = rootBucketPath + obj.Name;
                await AsyncRecognizeGcs(storageUri, filePath);
                
            }
        }

        private async Task AsyncRecognizeGcs(string storageUri, string filePath)
        {
            var transcriptText = "";
            var speech = Google.Cloud.Speech.V1.SpeechClient.Create();
            var codec = CheckAndReturnFileType(filePath);
            var bitrate = GetSampleRateInHertz(filePath);
            var googleCodec = (RecognitionConfig.Types.AudioEncoding)Enum.Parse(typeof(RecognitionConfig.Types.AudioEncoding), codec);

            var config = new RecognitionConfig()
            {
                Encoding = googleCodec,
                SampleRateHertz = bitrate,
                LanguageCode = "en-US",
            };

            var longOperation = speech.LongRunningRecognize(config, RecognitionAudio.FromStorageUri(storageUri));

            longOperation = await longOperation.PollUntilCompletedAsync();
            var response = longOperation.Result;
            foreach (var result in response.Results)
            {
                foreach (var alternative in result.Alternatives)
                {
                    transcriptText = transcriptText + $"{alternative.Transcript}\n";
                }
            }
            if (!Directory.Exists(TranscriptDestination))
            {
                Directory.CreateDirectory(TranscriptDestination);
            }

            if (File.Exists(TranscriptDestination + "google_" + Path.GetFileNameWithoutExtension(filePath) + ".txt"))
            {
                File.Delete(TranscriptDestination + "google_" + Path.GetFileNameWithoutExtension(filePath) + ".txt");
            }
            File.WriteAllText(TranscriptDestination + "google_" + Path.GetFileNameWithoutExtension(filePath) + ".txt", transcriptText);
        }

        private async Task DeleteFileAsync(string fileName)
        {
            //Cleanup the file after done with transcription
            await _client.DeleteObjectAsync(_bucketName, fileName);
        }

        private async Task DeleteBucketAsync()
        {
            //Cleanup the bucket after done with transcription
            await _client.DeleteBucketAsync(_bucketName);
        }

        public string CheckAndReturnFileType(string filePath)
        {
            var filetype = Path.GetExtension(filePath);
            switch (filetype)
            {
                case ".wav":
                    return "Linear16";
                case ".flac":
                    return "FLAC";
                case ".amr":
                    return "AMR";
                case ".3ga":
                    return "AMR";
                case ".awb":
                    return "AMR_WB";
                case ".ogg":
                    return "OGG_OPUS";
                case ".spx":
                    return "SPEEX_WITH_HEADER_BYTE";
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
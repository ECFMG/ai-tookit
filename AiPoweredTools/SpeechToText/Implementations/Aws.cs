using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Amazon.TranscribeService;
using Amazon.TranscribeService.Model;
using Newtonsoft.Json.Linq;
using NAudio.Wave;

namespace AiPoweredTools.SpeechToText
{
    internal sealed class Aws : ISpeechServiceStrategy, IAudioFileUtility
    {
        private readonly string _bucketName;
        private readonly string _bucketRegion;
        private readonly IAmazonS3 _s3Client;
        private readonly IAmazonTranscribeService _atClient;

        public string TranscriptDestination { get; set; }

        public Aws( IAmazonS3 s3Client, IAmazonTranscribeService atClient, string bucketName)
        {
            _s3Client = s3Client;
            _atClient = atClient;
            _bucketName = bucketName;
            _bucketRegion = s3Client.Config.RegionEndpoint.SystemName.ToString();
        }

        public void ConvertToText(IEnumerable<string> listOfFilesToTranscribe)
        {
            var threadCount = 0;
            const int maxFilesProcessedAtATime = 10;
            var taskManager = new List<Task>();

            CreateBucketAsync().Wait();

            foreach (var filePath in listOfFilesToTranscribe)
            {
                threadCount++;
                var nextTask = ProcessFile(filePath);
                taskManager.Add(nextTask);

                if (threadCount != maxFilesProcessedAtATime) continue;
                Task.WaitAll(taskManager.ToArray());
                threadCount = 0;
                taskManager.Clear();
            }

            Task.WaitAll(taskManager.ToArray());

            DeleteBucket().Wait();

        }

        private async Task ProcessFile(string filePath)
        {
            await UploadFileAsync(filePath);
            await Transcribe(filePath);
            await DeleteFileInBucket(filePath);
        }

        private async Task CreateBucketAsync()
        {
                var putBucketRequest = new PutBucketRequest
                {
                    BucketName = _bucketName,
                    BucketRegion = _bucketRegion
                };
                await _s3Client.PutBucketAsync(putBucketRequest);
        }

        private async Task UploadFileAsync(string filePath)
        {
                var fileTransferUtility = new TransferUtility(_s3Client);
                await fileTransferUtility.UploadAsync(filePath, _bucketName);
        }

        private async Task Transcribe(string filePath)
        {
            var fileNameWithExtension = Path.GetFileName(filePath);
            var hash = (DateTime.Now.Ticks);
            var jobName = "Test_" + fileNameWithExtension + "_" + hash;
            var bucketref = "https://s3." + _bucketRegion + ".amazonaws.com/" + _bucketName + "/" + fileNameWithExtension;
            Media fileref = new Media {MediaFileUri = bucketref};
            var fileInfoContainer = CheckValidityAndReturnFileInfo(filePath).GetEnumerator();
            fileInfoContainer.MoveNext();
            var filetype = fileInfoContainer.Current.Key;
            var bitrate = fileInfoContainer.Current.Value;

            //TODO standardize a way to define / figure out the file properties
            var request = new StartTranscriptionJobRequest
            {
                TranscriptionJobName = jobName,
                LanguageCode = "en-US",
                MediaSampleRateHertz = bitrate,
                MediaFormat = filetype,
                Media = fileref
            };

            //initial request to service
            await _atClient.StartTranscriptionJobAsync(request);
            
            //generate polling request to check in on service
            var pollingreq = new GetTranscriptionJobRequest
            {
                TranscriptionJobName = request.TranscriptionJobName
            };

            //status of request
            string status;

            do
            {
                //check the request
                var step = await _atClient.GetTranscriptionJobAsync(pollingreq);
                //generate new status
                status = step.TranscriptionJob.TranscriptionJobStatus.Value;
                //sleep for 5 seconds before trying again
                Thread.Sleep(10000);
                //check if the transcription job failed for any reason
                if (status == "FAILED")
                {
                    Environment.Exit(0);
                }

            } while (status != "COMPLETED"); //run until complete

            //Check the request one more time to get final updated info
            var final = await _atClient.GetTranscriptionJobAsync(pollingreq);

            //Generate an Http client to use to GET the JSON data from the URI
            using (var temp = new HttpClient())
            {

                var location = final.TranscriptionJob.Transcript.TranscriptFileUri;
                var msg = await temp.GetAsync(location);
                var responseBody = await msg.Content.ReadAsStringAsync();

                //Newtonsoft Parsing body
                var data = JObject.Parse(responseBody);
                var query = (string) data["results"]["transcripts"][0]["transcript"];

                if (!Directory.Exists(TranscriptDestination))
                {
                    Directory.CreateDirectory(TranscriptDestination);
                }

                if (File.Exists(TranscriptDestination + "aws_" + Path.GetFileNameWithoutExtension(filePath) + ".txt"))
                {
                    File.Delete(TranscriptDestination + "aws_" + Path.GetFileNameWithoutExtension(filePath) + ".txt");
                }
                File.WriteAllText((TranscriptDestination + "aws_" + Path.GetFileNameWithoutExtension(filePath) + ".txt"), query);

            }
        }

        private async Task DeleteFileInBucket(string filePath)
        {

            // a multi-object delete by specifying the key names and version IDs.
            DeleteObjectRequest objectDeleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = Path.GetFileName(filePath)
            };
            // You can add specific object key to the delete request using the AddKey
            // multiObjectDeleteRequest.AddKey("TickerReference.csv", null);
            await _s3Client.DeleteObjectAsync(objectDeleteRequest);
        }

        private async Task DeleteBucket()
        {
            var deleteObjectRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName
            };
            await _s3Client.DeleteObjectAsync(deleteObjectRequest);
        }

        
        public Dictionary<string, int> CheckValidityAndReturnFileInfo(string filePath)
        {
            var info = new Dictionary<string, int>();
            string codec;
            int sampleRate;
            var filetype = Path.GetExtension(filePath);
            switch (filetype)
            {
                case ".wav":
                    codec = "Wav";
                    using (var reader = new WaveFileReader(filePath))
                    {
                        sampleRate = reader.WaveFormat.SampleRate;
                        if (sampleRate < 8000 && sampleRate > 48000) { throw new FileLoadException(); }
                    }
                    info.Add(codec, sampleRate);
                    return info;
                case ".mp3":
                    codec = "Mp3";
                    using (var reader = new Mp3FileReader(filePath))
                    {
                        sampleRate = reader.Mp3WaveFormat.SampleRate;
                        if (sampleRate < 8000 && sampleRate > 48000) { throw new FileLoadException(); }
                    }
                    info.Add(codec, sampleRate);
                    return info;
                case ".flac":
                    codec = "Flac";
                    using (var reader = new WaveFileReader(filePath))
                    {
                        sampleRate = reader.WaveFormat.SampleRate;
                        if (sampleRate < 8000 && sampleRate > 48000) { throw new FileLoadException(); }
                    }
                    info.Add(codec, sampleRate);
                    return info;
                case ".m4a":
                    codec = "M4a";
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


        //TODO expose wordlist interface
        /* //vocabulary builder (come back to after temp one works)
        CreateVocabularyRequest wordsreq = new CreateVocabularyRequest
        {
            VocabularyName = "wordlist",
            LanguageCode = "en-US",
            Phrases = { "99", "ninety-nines", "ahh", "tongue", "okay", "temporary", "broken" }
        }; */

        //var setref = new Settings {VocabularyName = "wordlist"};

        //transcription request creation

    }
}
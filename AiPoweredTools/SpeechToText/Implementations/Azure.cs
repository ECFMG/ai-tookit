using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using NAudio.Wave;

namespace AiPoweredTools.SpeechToText
{
    internal sealed class Azure : ISpeechServiceStrategy, IAudioFileUtility
    {
        public string TranscriptDestination { get; set; }

        private readonly string _apiKey;
        private readonly string _apiRegion;

        public Azure(string apiKey, string apiRegion)
        {
            _apiKey = apiKey;
            _apiRegion = apiRegion;
        }

        public void ConvertToText(IEnumerable<string> listOfFilesToTranscribe)
        {
            foreach (var filePath in listOfFilesToTranscribe)
            {
                ProcessFile(filePath).Wait();
            }
            
        }

        private async Task ProcessFile(string filePath)
        {
            // Creates an instance of a speech factory with specified
            // subscription key and service region. Replace with your own subscription key
            // and service region (e.g., "westus").
            //var fileInfoContainer = CheckValidityAndReturnFileInfo(filePath).GetEnumerator();
            //fileInfoContainer.MoveNext();
            //var filetype = fileInfoContainer.Current.Key;
            //var bitrate = fileInfoContainer.Current.Value;

            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var factory = SpeechFactory.FromSubscription(_apiKey, _apiRegion);
            var isStop = false;

            if (!Directory.Exists(TranscriptDestination))
            {
                Directory.CreateDirectory(TranscriptDestination);
            }

            if (File.Exists(TranscriptDestination + "azure_" + fileName + ".txt"))
            {
                File.Delete(TranscriptDestination + "azure_" + fileName + ".txt");
            }


            // Creates a speech recognizer using microphone as audio input.
            using (var recognizer = factory.CreateSpeechRecognizerWithFileInput(filePath, "en-US"))
            {
                recognizer.FinalResultReceived += (s, e) =>
                {
                    if ((e.Result.RecognitionStatus == RecognitionStatus.Recognized))
                    {
                        File.AppendAllText(TranscriptDestination + "azure_" + fileName + ".txt", e.Result.Text);
                    }
                };

                recognizer.OnSessionEvent += (s, e) =>
                {
                    isStop = e.EventType != SessionEventType.SessionStartedEvent;
                };

                // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                await recognizer.StartContinuousRecognitionAsync();

                //stopping check
                while (true)
                {
                    if (isStop)
                    {
                        break;
                    }
                }

                await recognizer.StopContinuousRecognitionAsync();
            }
        }

        public string CheckAndReturnFileType(string filePath)
        {
            var filetype = Path.GetExtension(filePath);
            switch (filetype)
            {
                case ".wav":
                    return "Wav";
                case ".pcm":
                    return "Pcm";
                default:
                    throw new FileLoadException();
            }
        }

        public int GetSampleRateInHertz(string filePath)
        {
            using (var reader = new WaveFileReader(filePath))
            {
                var bitrate = reader.WaveFormat.SampleRate;
                //this proves it is mono channel audio as in audio with multiple channels the AverageBytesPerSecond will be a multiple of the SampleRate
                if (bitrate == reader.WaveFormat.AverageBytesPerSecond) { return bitrate; }
                throw new FileLoadException();
            }
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
                        if (sampleRate != reader.WaveFormat.AverageBytesPerSecond) { throw new FileLoadException(); }
                    }
                    info.Add(codec, sampleRate);
                    return info;
                case ".pcm":
                    codec = "Pcm";
                    using (var reader = new WaveFileReader(filePath))
                    {
                        sampleRate = reader.WaveFormat.SampleRate;
                        if (sampleRate != reader.WaveFormat.AverageBytesPerSecond) { throw new FileLoadException(); }
                    }
                    info.Add(codec, sampleRate);
                    return info;
                default:
                    throw new FileLoadException();
            }
        }
    }
}
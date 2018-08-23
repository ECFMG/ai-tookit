using System.Collections.Generic;
using System.IO;

namespace AiPoweredTools.SpeechToText
{

    public enum SpeechServiceType
    {
        aws,
        google,
        ibm,
        azure,
        azureAdv
    }

    public class SpeechClient : ISpeechClient
    {
        internal ISpeechServiceStrategy Strategy { get; private set; }

        public string FileDestination
        {
            set
            {
                if (Directory.Exists(value))
                {
                    Strategy.TranscriptDestination = value;
                }

                //TODO throw exception here?
            }
        }

        public SpeechClient(SpeechServiceType serviceName)
        {
                Strategy = Factory.CreateService(serviceName);
                Strategy.TranscriptDestination = Directory.GetCurrentDirectory() + @"\Transcriptions\";
        }

        public void SwitchService(SpeechServiceType serviceName)
        {
                var previousDestination = Strategy.TranscriptDestination;

                Strategy = Factory.CreateService(serviceName);
                Strategy.TranscriptDestination = previousDestination;
        }

        public void Transcribe(IEnumerable<string> fileListToTranscribe)
        {
            Strategy.ConvertToText(fileListToTranscribe);
        }
    }
}
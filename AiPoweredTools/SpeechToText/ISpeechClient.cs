using System.Collections.Generic;

namespace AiPoweredTools.SpeechToText
{
    public interface ISpeechClient
    {
        string FileDestination { set; }

        void SwitchService(SpeechServiceType serviceName);

        void Transcribe(IEnumerable<string> fileListToTranscribe);
    }
}

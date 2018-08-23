using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("AiCoreTester")]

namespace AiPoweredTools.SpeechToText
{
    public interface ISpeechServiceStrategy
    {
        string TranscriptDestination { get; set; }

        void ConvertToText(IEnumerable<string> listOfFilesToTranscribe);
    }
}
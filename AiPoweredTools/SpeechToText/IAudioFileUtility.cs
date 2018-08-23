using System.Collections.Generic;

namespace AiPoweredTools.SpeechToText
{
    internal interface IAudioFileUtility
    {
        Dictionary<string, int> CheckValidityAndReturnFileInfo(string filePath);
    }
}

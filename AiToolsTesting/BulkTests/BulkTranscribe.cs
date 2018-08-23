using System.Collections.Generic;
using System.IO;
using AiPoweredTools.SpeechToText;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AiToolsTesting.BulkTests
{

    //This is not for testing purposes but to actually utilize the library (short term usage)


    [TestClass]
    public class BulkTranscribe
    {
        [TestMethod]
        public void TestMethod1()
        {
            var filelist = new List<string>
            {
                @"C:\Users\nmccarthy\Desktop\wavyvid1.wav"
            };

            var a = new SpeechClient(SpeechServiceType.aws);
            a.Transcribe(filelist);

            a.SwitchService(SpeechServiceType.azure);
            a.Transcribe(filelist);

            a.SwitchService(SpeechServiceType.azureAdv);
            a.Transcribe(filelist);

            a.SwitchService(SpeechServiceType.google);
            a.Transcribe(filelist);

            a.SwitchService(SpeechServiceType.ibm);
            a.Transcribe(filelist);

            Assert.IsTrue(File.Exists(@"C:\Users\nmccarthy\Desktop\AIProject\aiApiCode\AiPoweredTools\AiToolsTesting\bin\x64\Debug\Transcriptions\aws_wavyvid1.txt"));
            Assert.IsTrue(File.Exists(@"C:\Users\nmccarthy\Desktop\AIProject\aiApiCode\AiPoweredTools\AiToolsTesting\bin\x64\Debug\Transcriptions\azure_wavyvid1.txt"));
            Assert.IsTrue(File.Exists(@"C:\Users\nmccarthy\Desktop\AIProject\aiApiCode\AiPoweredTools\AiToolsTesting\bin\x64\Debug\Transcriptions\azureAdv_wavyvid1.txt"));
            Assert.IsTrue(File.Exists(@"C:\Users\nmccarthy\Desktop\AIProject\aiApiCode\AiPoweredTools\AiToolsTesting\bin\x64\Debug\Transcriptions\google_wavyvid1.txt"));
            Assert.IsTrue(File.Exists(@"C:\Users\nmccarthy\Desktop\AIProject\aiApiCode\AiPoweredTools\AiToolsTesting\bin\x64\Debug\Transcriptions\ibm_wavyvid1.txt"));

        }
    }
}

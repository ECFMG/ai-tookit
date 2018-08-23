using System.Collections.Generic;
using System.IO;
using AiPoweredTools.SpeechToText;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AiToolsTesting.Tests
{
    [TestClass]
    public class GoogleTests
    {
        [TestMethod]
        public void GoogleOne()
        {
            //arrange
            var filelist = new List<string>
            {
                @"C:\Users\nmccarthy\Desktop\whatstheweatherlike.wav",
                @"C:\Users\nmccarthy\Desktop\whatstheweatherlike2.wav",
                @"C:\Users\nmccarthy\Desktop\whatstheweatherlike3.wav"
            };
            var y = new SpeechClient(SpeechServiceType.google);

            //act
            y.Transcribe(filelist);

            //assert
            Assert.IsTrue(File.Exists(@"C:\Users\nmccarthy\Desktop\AIProject\aiApiCode\AiPoweredTools\AiToolsTesting\bin\x64\Debug\Transcriptions\google_whatstheweatherlike.txt"));
            Assert.IsTrue(File.Exists(@"C:\Users\nmccarthy\Desktop\AIProject\aiApiCode\AiPoweredTools\AiToolsTesting\bin\x64\Debug\Transcriptions\google_whatstheweatherlike2.txt"));
            Assert.IsTrue(File.Exists(@"C:\Users\nmccarthy\Desktop\AIProject\aiApiCode\AiPoweredTools\AiToolsTesting\bin\x64\Debug\Transcriptions\google_whatstheweatherlike3.txt"));
        }
    }
}

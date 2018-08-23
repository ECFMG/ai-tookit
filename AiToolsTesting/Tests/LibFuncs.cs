using System.Collections.Generic;
using System.IO;
using AiPoweredTools.SpeechToText;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AiToolsTesting.Tests
{
    [TestClass]
    public class LibFuncs
    {
        [TestMethod]
        public void SwitchService()
        {
            //arrange
            var x = new SpeechClient(SpeechServiceType.aws);
            var y = new SpeechClient(SpeechServiceType.azure);

            var a = AiPoweredTools.Factory.CreateService(SpeechServiceType.google);
            var b = AiPoweredTools.Factory.CreateService(SpeechServiceType.ibm);

            //act
            x.SwitchService(SpeechServiceType.google);
            y.SwitchService(SpeechServiceType.ibm);

            //assert
            Assert.IsTrue(x.Strategy.ToString() == a.ToString());
            Assert.IsTrue(y.Strategy.ToString() == b.ToString());
        }

        [TestMethod]
        public void ChangeFileDestinationDefault()
        {
            //arrange
            var x = new SpeechClient(SpeechServiceType.ibm);
            var filelist = new List<string>
            {
                @"C:\Users\nmccarthy\Desktop\whatstheweatherlike.wav"
            };
            //act
            x.FileDestination = @"C:\Users\nmccarthy\Desktop\";
            x.SwitchService(SpeechServiceType.google);
            x.Transcribe(filelist);
            //assert
            Assert.IsTrue(File.Exists(@"C:\Users\nmccarthy\Desktop\google_whatstheweatherlike.txt"));
        }
    }
}

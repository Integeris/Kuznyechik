using Kuznyechik;
using System;
using System.Linq;
using System.Text;

namespace KuznyechikTests
{
    [TestClass()]
    public class ScramblerTests
    {
        [TestMethod("Шифрование и дешифрование строк")]
        [DataRow("Привет мир!")]
        [DataRow("1234567890")]
        public void EncryptTest(string text)
        {
            byte[] key = new byte[32];
            byte[] message = Encoding.UTF8.GetBytes(text);

            {
                Random random = new Random();
                random.NextBytes(key);
            }

            Scrambler scrambler = new Scrambler(key);

            scrambler.Encrypt(ref message);
            scrambler.Decrypt(ref message);

            string outText = Encoding.UTF8.GetString(message);

            Assert.AreEqual(text, outText);
        }

        [TestMethod("Шифрование и дешифрование большого объёма данных")]
        public void EncryptBigDataTest()
        {
            byte[] key = new byte[32];
            byte[] message = new byte[16777216];
            byte[] messageCopy = new byte[16777216];

            {
                Random random = new Random();
                random.NextBytes(key);
                random.NextBytes(message);
                Array.Copy(message, messageCopy, message.Length);
            }

            Scrambler scrambler = new Scrambler(key);
            scrambler.Encrypt(ref message);
            scrambler.Decrypt(ref message);

            Assert.IsTrue(message.SequenceEqual(messageCopy));
        }
    }
}
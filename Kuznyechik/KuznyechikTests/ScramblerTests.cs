using Kuznyechik;
using System;
using System.IO;
using System.Linq;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace KuznyechikTests
{
    [TestClass()]
    public class ScramblerTests
    {
        [TestMethod("Шифрование и дешифрование строк")]
        [DataRow("Привет мир!")]
        [DataRow("1234567890")]
        [DataRow("")]
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
        [DataRow(128)]
        [DataRow(256)]
        [DataRow(512)]
        [DataRow(1024)]
        [DataRow(2048)]
        [DataRow(4096)]
        [DataRow(8192)]
        public void EncryptBigDataTest(int bufferSize)
        {
            byte[] key = new byte[32];
            byte[] message = new byte[4194304];
            byte[] messageCopy = new byte[4194304];

            {
                Random random = new Random();
                random.NextBytes(key);
                random.NextBytes(message);
                Array.Copy(message, messageCopy, message.Length);
            }

            Scrambler scrambler = new Scrambler(key)
            {
                BufferSize = bufferSize
            };
            scrambler.Encrypt(ref message);
            scrambler.Decrypt(ref message);

            Assert.IsTrue(message.SequenceEqual(messageCopy));
        }

        [TestMethod("Шифрование и дешифрование потока")]
        [DataRow("Привет мир!")]
        [DataRow("1234567890")]
        [DataRow("1234567890")]
        public void EncryptStream(string text)
        {
            byte[] key = new byte[32];
            byte[] message = Encoding.UTF8.GetBytes(text);
            byte[] messageCopy = (byte[])message.Clone();

            using (MemoryStream dataStream = new MemoryStream(message))
            {
                using (MemoryStream encryptedStream = new MemoryStream())
                {
                    Random random = new Random();
                    random.NextBytes(key);

                    Scrambler scrambler = new Scrambler(key);
                    scrambler.Encrypt(dataStream, encryptedStream);
                    dataStream.Position = encryptedStream.Position = 0;
                    scrambler.Decrypt(encryptedStream, dataStream);
                }
            }

            Assert.IsTrue(message.SequenceEqual(messageCopy));
        }
    }
}
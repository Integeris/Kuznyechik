using Kuznyechik;
using System;
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

            Console.WriteLine(message.Length);
            Console.WriteLine(String.Join(", ", message));

            {
                Random random = new Random();
                random.NextBytes(key);
            }

            Scrambler scrambler = new Scrambler(key);
            message = scrambler.Encrypt(message);
            Console.WriteLine(message.Length);
            Console.WriteLine(String.Join(", ", message));
            message = scrambler.Decrypt(message);
            Console.WriteLine(message.Length);

            string outText = Encoding.UTF8.GetString(message);

            Assert.AreEqual(text, outText);
        }
    }
}
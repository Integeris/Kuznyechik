using Kuznyechik;
using System;
using System.IO;
using System.Threading.Tasks;

namespace KuznyechikTests
{
    [TestClass()]
    public class ScramblerECBTests
    {
        [TestMethod("Расшифровка блока данных.")]
        [DataRow(0, DisplayName = "Нулевой блок")]
        [DataRow(10, DisplayName = "Неполный блок")]
        [DataRow(16, DisplayName = "Полный блок")]
        [DataRow(26, DisplayName = "Полный блок с остатком")]
        [DataRow(32, DisplayName = "Данные кратные 16.")]
        public void TestSimpleDecrypt(int dataLength)
        {
            Span<byte> key = stackalloc byte[32];
            byte[] data = new byte[dataLength];
            Span<byte> encryptedData;

            ScramblerECB scrambler = new ScramblerECB(key);
            encryptedData = scrambler.Encrypt(data);

            Span<byte> decryptedData = scrambler.Decrypt(encryptedData);
            Assert.IsTrue(data.SequenceEqual(decryptedData));
        }

        [TestMethod("Расшифровка с использованием потока.")]
        [DataRow(0, DisplayName = "Нулевой блок")]
        [DataRow(10, DisplayName = "Неполный блок")]
        [DataRow(16, DisplayName = "Полный блок")]
        [DataRow(26, DisplayName = "Полный блок с остатком")]
        [DataRow(32, DisplayName = "Данные кратные 16.")]
        public void TestStreamDecrypt(int dataLength)
        {
            Span<byte> key = stackalloc byte[32];
            byte[] data = new byte[dataLength];
            byte[] encryptedData;

            ScramblerECB scrambler = new ScramblerECB(key);
            encryptedData = scrambler.Encrypt(data).ToArray();

            using (MemoryStream encryptedStream = new MemoryStream(encryptedData, false))
            using (MemoryStream decryptedStream = new MemoryStream())
            {
                scrambler.Decrypt(encryptedStream, decryptedStream);
                Assert.IsTrue(data.SequenceEqual(decryptedStream.ToArray()));
            }
        }

        [TestMethod("Асинхронное шифрование и расшифровка с использованием потока.")]
        [DataRow(0, DisplayName = "Нулевой блок")]
        [DataRow(10, DisplayName = "Неполный блок")]
        [DataRow(16, DisplayName = "Полный блок")]
        [DataRow(26, DisplayName = "Полный блок с остатком")]
        [DataRow(32, DisplayName = "Данные кратные 16.")]
        public async Task TestAsyncStream(int dataLength)
        {
            Span<byte> key = stackalloc byte[32];
            byte[] data = new byte[dataLength];
            byte[] encryptedData = new byte[data.Length + 16 - data.Length % 16];

            ScramblerECB scrambler = new ScramblerECB(key);

            using (MemoryStream sourceStream = new MemoryStream(data, false))
            using (MemoryStream encryptedStream = new MemoryStream(encryptedData, true))
            {
                await scrambler.EncryptAsync(sourceStream, encryptedStream);

                encryptedStream.Position = 0;
                using (MemoryStream decryptedStream = new MemoryStream())
                {
                    await scrambler.DecryptAsync(encryptedStream, decryptedStream);
                    Assert.IsTrue(data.SequenceEqual(decryptedStream.ToArray()));
                }
            }
        }
    }
}

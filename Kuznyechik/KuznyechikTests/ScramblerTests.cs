using Kuznyechik;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KuznyechikTests
{
    [TestClass()]
    public class ScramblerTests
    {
        [TestMethod("Шифрование и расшифрование строк")]
        [DataRow("Привет мир!", DisplayName = "Привет мир!")]
        [DataRow("1234567890", DisplayName = "1234567890")]
        [DataRow("", DisplayName = "Пустая строка")]
        public void EncryptArr(string text)
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

        [TestMethod("Шифрование и Расшифрование потока")]
        [DataRow("Привет мир!", DisplayName = "Привет мир!")]
        [DataRow("1234567890", DisplayName = "1234567890")]
        public void EncryptStream(string text)
        {
            byte[] key = new byte[32];
            byte[] message = Encoding.UTF8.GetBytes(text);
            byte[] messageCopy = (byte[])message.Clone();

            {
                Random random = new Random();
                random.NextBytes(key);
            }

            using (MemoryStream dataStream = new MemoryStream())
            using (MemoryStream encryptedStream = new MemoryStream())
            {
                Scrambler scrambler = new Scrambler(key);

                dataStream.Write(messageCopy, 0, messageCopy.Length);
                dataStream.Seek(0, SeekOrigin.Begin);

                scrambler.Encrypt(dataStream, encryptedStream);
                dataStream.Position = encryptedStream.Position = 0;
                scrambler.Decrypt(encryptedStream, dataStream);

                dataStream.Position = 0;
                dataStream.Read(messageCopy, 0, messageCopy.Length);
            }

            Assert.IsTrue(message.SequenceEqual(messageCopy));
        }

        [TestMethod("Шифрование и расшифрование большого объёма данных")]
        [DataRow(160000, DisplayName = "160000 байт")]
        [DataRow(16000000, DisplayName = "16000000 байт")]
        [DataRow(201326592, DisplayName = "201326592 байт")]
        [DataRow(402653184, DisplayName = "402653184 байт")]
        [DataRow(805306368, DisplayName = "805306368 байт")]
        public void EncryptBigData(long arraySize)
        {
            Stopwatch stopwatch = new Stopwatch();
            byte[] key = new byte[32];
            byte[] arr = new byte[arraySize];

            {
                Random random = new Random();
                random.NextBytes(key);
                random.NextBytes(arr);
            }

            byte[] arrCopy = new byte[arr.Length];
            Array.Copy(arr, arrCopy, arr.Length);

            Scrambler scrambler = new Scrambler(key);

            stopwatch.Start();
            scrambler.Encrypt(ref arrCopy);
            stopwatch.Stop();

            Console.WriteLine("Шифрование закончено за: {0}", stopwatch.Elapsed);

            stopwatch.Restart();
            scrambler.Decrypt(ref arrCopy);
            stopwatch.Stop();
            Console.WriteLine("Расшифоровывание закончено за: {0}", stopwatch.Elapsed);

            Assert.IsTrue(arr.SequenceEqual(arrCopy));
        }

        [TestMethod("Шифрование и расшифрование строк асинхронно")]
        [DataRow("Привет мир!", DisplayName = "Привет мир!")]
        [DataRow("1234567890", DisplayName = "1234567890")]
        [DataRow("", DisplayName = "Пустая строка")]
        public async Task EncryptAsyncArr(string text)
        {
            byte[] key = new byte[32];
            byte[] message = Encoding.UTF8.GetBytes(text);

            {
                Random random = new Random();
                random.NextBytes(key);
            }

            Scrambler scrambler = new Scrambler(key);

            message = await scrambler.EncryptAsync(message);
            message = await scrambler.DecryptAsync(message);

            string outText = Encoding.UTF8.GetString(message);
            Assert.AreEqual(text, outText);
        }

        [TestMethod("Шифрование и расшифрование потока асинхронно")]
        [DataRow("Привет мир!", DisplayName = "Привет мир!")]
        [DataRow("1234567890", DisplayName = "1234567890")]
        public async Task EncryptAsyncStream(string text)
        {
            byte[] key = new byte[32];
            byte[] message = Encoding.UTF8.GetBytes(text);
            byte[] messageCopy = (byte[])message.Clone();

            {
                Random random = new Random();
                random.NextBytes(key);
            }

            using (MemoryStream dataStream = new MemoryStream())
            using (MemoryStream encryptedStream = new MemoryStream())
            {
                Scrambler scrambler = new Scrambler(key);

                dataStream.Write(messageCopy, 0, messageCopy.Length);
                dataStream.Seek(0, SeekOrigin.Begin);

                await scrambler.EncryptAsync(dataStream, encryptedStream);
                dataStream.Position = encryptedStream.Position = 0;
                await scrambler.DecryptAsync(encryptedStream, dataStream);

                dataStream.Position = 0;
                dataStream.Read(messageCopy, 0, messageCopy.Length);
            }

            Assert.IsTrue(message.SequenceEqual(messageCopy));
        }

        [TestMethod("Отмена асинхронного зашифровывания данных")]
        [DataRow(805306368)]
        public async Task CancelEncryptAsync(long arraySize)
        {
            byte[] key = new byte[32];
            byte[] arr = new byte[arraySize];

            {
                Random random = new Random();
                random.NextBytes(key);
                random.NextBytes(arr);
            }

            byte[] arrCopy = new byte[arr.Length];
            Array.Copy(arr, arrCopy, arr.Length);

            Scrambler scrambler = new Scrambler(key);

            CancellationTokenSource cancellationToken = new CancellationTokenSource();
            Task task = scrambler.EncryptAsync(arr, cancellationToken: cancellationToken.Token);
            cancellationToken.CancelAfter(100);

            try
            {
                await task;
                Assert.Fail($"Ожидалась {nameof(OperationCanceledException)}");
            }
            catch (OperationCanceledException)
            {
                Assert.IsTrue(true);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Ожидалась {nameof(OperationCanceledException)}, но получено {ex.GetType()}");
            }
        }

        [TestMethod("Проверка прогресса асинхронного зашифровывания данных")]
        [DataRow(1610612736, DisplayName = "1610612736")]
        public void ProgressEncryptAsync(long arraySize)
        {
            byte[] key = new byte[32];
            byte[] arr = new byte[arraySize];

            {
                Random random = new Random();
                random.NextBytes(key);
                random.NextBytes(arr);
            }

            byte[] arrCopy = new byte[arr.Length];
            Array.Copy(arr, arrCopy, arr.Length);

            Scrambler scrambler = new Scrambler(key);

            Progress<CryptoStatus> progress = new Progress<CryptoStatus>((status) =>
                Console.WriteLine("Позиция {0} из {1} (буфер: {2}). Процент: {3:P2}",
                    status.DataPosition,
                    status.DataLength,
                    status.BufferLength,
                    status.DataPosition / status.DataLength));

            CancellationTokenSource cancellationToken = new CancellationTokenSource();
            Task task = scrambler.EncryptAsync(arr, progress, cancellationToken.Token);

            cancellationToken.CancelAfter(10000);
            task.Wait();

            Assert.IsTrue(true);
        }
    }
}
using System;
using System.Collections.Immutable;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Kuznyechik
{
    /// <summary>
    /// Шифровщик (алгоритм "Кузнечик").
    /// </summary>
    public sealed class Scrambler : IDisposable
    {
        /// <summary>
        /// Параметры.
        /// </summary>
        private readonly CryptoParameters parameters;

        /// <summary>
        /// Очищен ли объект.
        /// </summary>
        private bool disposed;

        /// <summary>
        /// Параметры.
        /// </summary>
        public CryptoParameters Parameters
        {
            get => this.parameters;
        }

        /// <summary>
        /// Создание шифратора.
        /// </summary>
        /// <param name="parameters">Параметры шифратора</param>
        public Scrambler(CryptoParameters parameters)
        {
            this.parameters = parameters;
        }

        /// <summary>
        /// Создание шифратора.
        /// </summary>
        /// <param name="key">Ключ (32 байта).</param>
        public Scrambler(byte[] key) : this(new CryptoParameters(key)) { }

        /// <summary>
        /// Уничтожение шифровщика.
        /// </summary>
        ~Scrambler()
        {
            this.Dispose();
        }

        /// <summary>
        /// Шифрование массива блоков.
        /// </summary>
        /// <param name="arr">Массив.</param>
        /// <exception cref="ArgumentException"></exception>
        public void Encrypt(ref byte[] arr)
        {
            using (MemoryStream readStream = new MemoryStream())
            using (MemoryStream writeStream = new MemoryStream())
            {
                readStream.Write(arr, 0, arr.Length);
                readStream.Position = 0;

                this.Encrypt(readStream, writeStream);
                arr = writeStream.ToArray();
            }
        }

        /// <summary>
        /// Шифрование данных из потока в поток.
        /// </summary>
        /// <param name="readStream">Поток данных.</param>
        /// <param name="writeStream">Выходной поток с зашифрованными данными.</param>
        /// <exception cref="ArgumentException"></exception>
        public void Encrypt(Stream readStream, Stream writeStream)
        {
            this.EncryptProcess(readStream, writeStream);
        }

        /// <summary>
        /// Шифрование массива блоков.
        /// </summary>
        /// <param name="arr">Массив.</param>
        /// <param name="progress">Прогресс шифрования.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задча шифрования.</returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<byte[]> EncryptAsync(
            byte[] arr,
            IProgress<CryptoStatus> progress = default,
            CancellationToken cancellationToken = default)
        {
            using (MemoryStream readStream = new MemoryStream())
            using (MemoryStream writeStream = new MemoryStream())
            {
                readStream.Write(arr, 0, arr.Length);
                readStream.Position = 0;

                await this.EncryptAsync(readStream, writeStream, progress, cancellationToken);
                return writeStream.ToArray();
            }
        }

        /// <summary>
        /// Шифрование данных из потока в поток.
        /// </summary>
        /// <param name="readStream">Поток для чтения данных.</param>
        /// <param name="writeStream">Выходной поток с зашифрованными данными.</param>
        /// <param name="progress">Прогресс шифрования.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача шифрования.</returns>
        public async Task EncryptAsync(
            Stream readStream,
            Stream writeStream,
            IProgress<CryptoStatus> progress = default,
            CancellationToken cancellationToken = default)
        {
            await Task.Run(() => this.EncryptProcess(
                readStream,
                writeStream,
                progress,
                cancellationToken));
        }

        /// <summary>
        /// Расшифрование массива.
        /// </summary>
        /// <param name="arr">Массив.</param>
        /// <exception cref="ArgumentException"></exception>
        public void Decrypt(ref byte[] arr)
        {
            using (MemoryStream readStream = new MemoryStream())
            using (MemoryStream writeStream = new MemoryStream())
            {
                readStream.Write(arr, 0, arr.Length);
                readStream.Position = 0;

                this.Decrypt(readStream, writeStream);
                arr = writeStream.ToArray();
            }
        }

        /// <summary>
        /// Расшифрование данных из потока в поток.
        /// </summary>
        /// <param name="readStream">Поток для чтения данных.</param>
        /// <param name="writeStream">Выходной поток с расшифрованными данными.</param>
        /// <exception cref="ArgumentException"></exception>
        public void Decrypt(Stream readStream, Stream writeStream)
        {
            this.CheckDecryptStream(readStream, writeStream);
            this.DecryptProcess(readStream, writeStream);
        }

        /// <summary>
        /// Расшифрование массива.
        /// </summary>
        /// <param name="arr">Массив.</param>
        /// <param name="progress">Прогресс расшифровки.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача расшифровки.</returns>
        public async Task<byte[]> DecryptAsync(
            byte[] arr,
            IProgress<CryptoStatus> progress = default,
            CancellationToken cancellationToken = default)
        {
            using (MemoryStream readStream = new MemoryStream())
            using (MemoryStream writeStream = new MemoryStream())
            {
                readStream.Write(arr, 0, arr.Length);
                readStream.Position = 0;

                await this.DecryptAsync(readStream, writeStream, progress, cancellationToken);
                return writeStream.ToArray();
            }
        }

        /// <summary>
        /// Расшифрование данных из потока в поток.
        /// </summary>
        /// <param name="readStream">Поток для чтения данных.</param>
        /// <param name="writeStream">Выходной поток с расшифрованными данными.</param>
        /// <param name="progress">Прогресс расшифровки.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача расшифровки.</returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task DecryptAsync(
            Stream readStream,
            Stream writeStream,
            IProgress<CryptoStatus> progress = default,
            CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                this.CheckDecryptStream(readStream, writeStream);

                this.DecryptProcess(
                    readStream,
                    writeStream,
                    progress,
                    cancellationToken);
            });
        }

        /// <summary>
        /// Освобождение неуправляемых ресурсов.
        /// </summary>
        public void Dispose()
        {
            // TODO: Рассмотреть удаление метода Dispose.
            if (this.disposed)
            {
                return;
            }

            GC.SuppressFinalize(this);
            this.disposed = true;
        }

        /// <summary>
        /// Проверка потоков.
        /// </summary>
        /// <param name="readStream">Поток для чтения данных.</param>
        /// <param name="writeStream">Поток для записи.</param>
        /// <exception cref="ArgumentException"></exception>
        private void CheckStreams(Stream readStream, Stream writeStream)
        {
            if (readStream == null)
            {
                throw new ArgumentException("Поток для чтения данных не может быть null.", nameof(readStream));
            }
            else if (writeStream == null)
            {
                throw new ArgumentException("Поток для записи данных не может быть null.", nameof(writeStream));
            }
            else if (!writeStream.CanWrite)
            {
                throw new ArgumentException("Поток для записи должен быть доступен для записи.", nameof(writeStream));
            }
            else if (readStream == writeStream)
            {
                throw new ArgumentException("Нельзя выполнить чтение и запись в один и тот же поток.", nameof(writeStream));
            }
        }

        /// <summary>
        /// Проверка потоков.
        /// </summary>
        /// <param name="readStream">Поток для чтения данных.</param>
        /// <param name="writeStream">Поток для записи.</param>
        /// <exception cref="ArgumentException"></exception>
        private void CheckDecryptStream(Stream readStream, Stream writeStream)
        {
            if (readStream.Length == 0)
            {
                throw new ArgumentException("Некорректный размер потока для чтения данных: размер не может быть равен нулю.",
                    nameof(readStream));
            }
            else if (readStream.Length % CryptoUtils.BlockSize != 0)
            {
                throw new ArgumentException("Некорректный размер потока для чтения данных: размер должен быть кратен размеру блока.",
                    nameof(readStream));
            }

            this.CheckStreams(readStream, writeStream);
        }

        /// <summary>
        /// Выполнение шифрования на устройстве.
        /// </summary>
        /// <param name="readStream">Поток данных.</param>
        /// <param name="writeStream">Поток преобразованных данных.</param>
        /// <param name="progress">Прогресс операции.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        private void EncryptProcess(
            Stream readStream,
            Stream writeStream,
            IProgress<CryptoStatus> progress = default,
            CancellationToken cancellationToken = default)
        {
            progress ??= new Progress<CryptoStatus>();

            long totalBytes = readStream.Length - readStream.Position;
            long blockCount = totalBytes / CryptoUtils.BlockSize;
            byte remainingBytes = (byte)(CryptoUtils.BlockSize - totalBytes % CryptoUtils.BlockSize);

            Block block;
            CryptoStatus status;

            byte[] buffer = new byte[CryptoUtils.BlockSize];

            for (; blockCount > 0; blockCount--)
            {
                cancellationToken.ThrowIfCancellationRequested();

                readStream.Read(buffer, 0, CryptoUtils.BlockSize);

                block = buffer;
                CryptoUtils.EncryptBlock(ref block, this.parameters);
                buffer = block;

                writeStream.Write(buffer, 0, buffer.Length);

                status = new CryptoStatus(readStream.Position, readStream.Length, CryptoUtils.BlockSize);
                progress.Report(status);
            }

            readStream.Read(buffer, 0, CryptoUtils.BlockSize);

            buffer[^1] = remainingBytes;

            block = buffer;
            CryptoUtils.EncryptBlock(ref block, this.parameters);
            buffer = block;

            writeStream.Write(buffer, 0, buffer.Length);

            status = new CryptoStatus(readStream.Position, readStream.Length, CryptoUtils.BlockSize);
            progress.Report(status);
        }

        /// <summary>
        /// Выполнение расшифровывания на устройстве.
        /// </summary>
        /// <param name="readStream">Поток данных.</param>
        /// <param name="writeStream">Поток преобразованных данных.</param>
        /// <param name="progress">Прогресс операции.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        private void DecryptProcess(
            Stream readStream,
            Stream writeStream,
            IProgress<CryptoStatus> progress = default,
            CancellationToken cancellationToken = default)
        {
            progress ??= new Progress<CryptoStatus>();

            long totalBytes = readStream.Length - readStream.Position;
            long blockCount = totalBytes / CryptoUtils.BlockSize - 1;

            Block block;
            CryptoStatus status;

            byte[] buffer = new byte[CryptoUtils.BlockSize];

            for (; blockCount > 0; blockCount--)
            {
                cancellationToken.ThrowIfCancellationRequested();

                readStream.Read(buffer, 0, CryptoUtils.BlockSize);

                block = buffer;
                CryptoUtils.DecryptBlock(ref block, this.parameters);
                buffer = block;

                writeStream.Write(buffer, 0, buffer.Length);

                status = new CryptoStatus(readStream.Position, readStream.Length, CryptoUtils.BlockSize);
                progress.Report(status);
            }

            readStream.Read(buffer, 0, CryptoUtils.BlockSize);

            block = buffer;
            CryptoUtils.DecryptBlock(ref block, this.parameters);
            buffer = block;

            byte paddingLength = buffer[^1];

            writeStream.Write(buffer, 0, CryptoUtils.BlockSize - paddingLength);

            status = new CryptoStatus(readStream.Position, readStream.Length, CryptoUtils.BlockSize);
            progress.Report(status);
        }
    }
}
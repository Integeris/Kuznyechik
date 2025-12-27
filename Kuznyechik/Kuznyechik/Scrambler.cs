using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using static Kuznyechik.CryptoUtils;

namespace Kuznyechik
{
    /// <summary>
    /// Шифровщик (алгоритм "Кузнечик").
    /// </summary>
    public sealed class Scrambler
    {
        /// <summary>
        /// Размер буфера в байтах.
        /// </summary>
        private uint bufferLength;

        /// <summary>
        /// Параметры.
        /// </summary>
        private readonly CryptoParameters parameters;

        /// <summary>
        /// Размер буфера в байтах.
        /// </summary>
        public uint BufferLength
        {
            get => this.bufferLength;
            set
            {
                if (value % CryptoUtils.BlockSize != 0)
                {
                    throw new ArgumentException("Буфер должен быть кратен размеру блока.", nameof(this.BufferLength));
                }

                this.bufferLength = value;
            }
        }

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
            this.bufferLength = 65536;
            this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));
        }

        /// <summary>
        /// Создание шифратора.
        /// </summary>
        /// <param name="key">Ключ (32 байта).</param>
        public Scrambler(byte[] key) : this(new CryptoParameters(key)) { }

        /// <summary>
        /// Зашифровывание массива блоков.
        /// </summary>
        /// <param name="arr">Массив.</param>
        /// <exception cref="ArgumentException"></exception>
        public void Encrypt(ref byte[] arr)
        {
            using (MemoryStream readStream = new MemoryStream(arr, 0, arr.Length))
            using (MemoryStream writeStream = new MemoryStream())
            {
                this.Encrypt(readStream, writeStream);
                arr = writeStream.ToArray();
            }
        }

        /// <summary>
        /// Зашифровывание данных из потока в поток.
        /// </summary>
        /// <param name="readStream">Поток данных.</param>
        /// <param name="writeStream">Выходной поток с зашифрованными данными.</param>
        /// <exception cref="ArgumentException"></exception>
        public void Encrypt(Stream readStream, Stream writeStream)
        {
            this.CheckStreams(readStream, writeStream);

            TaskAwaiter awaiter = this.EncryptProcessAsync(readStream, writeStream).GetAwaiter();
            awaiter.GetResult();
        }

        /// <summary>
        /// Зашифровывание массива блоков.
        /// </summary>
        /// <param name="arr">Массив.</param>
        /// <param name="progress">Прогресс зашифровывания.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача зашифровывания.</returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task<byte[]> EncryptAsync(
            byte[] arr,
            IProgress<CryptoStatus> progress = default,
            CancellationToken cancellationToken = default)
        {
            using (MemoryStream readStream = new MemoryStream(arr, 0, arr.Length))
            using (MemoryStream writeStream = new MemoryStream())
            {
                await this.EncryptAsync(readStream, writeStream, progress, cancellationToken)
                    .ConfigureAwait(false);
                return writeStream.ToArray();
            }
        }

        /// <summary>
        /// Зашифровывание данных из потока в поток.
        /// </summary>
        /// <param name="readStream">Поток для чтения данных.</param>
        /// <param name="writeStream">Выходной поток с зашифрованными данными.</param>
        /// <param name="progress">Прогресс зашифровывания.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача зашифровывания.</returns>
        public Task EncryptAsync(
            Stream readStream,
            Stream writeStream,
            IProgress<CryptoStatus> progress = default,
            CancellationToken cancellationToken = default)
        {
            this.CheckStreams(readStream, writeStream);

            return Task.Run(async () => 
                await this.EncryptProcessAsync(
                    readStream,
                    writeStream,
                    progress,
                    cancellationToken), cancellationToken);
        }

        /// <summary>
        /// Расшифрование массива.
        /// </summary>
        /// <param name="arr">Массив.</param>
        /// <exception cref="ArgumentException"></exception>
        public void Decrypt(ref byte[] arr)
        {
            using (MemoryStream readStream = new MemoryStream(arr, 0, arr.Length))
            using (MemoryStream writeStream = new MemoryStream())
            {
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
            this.CheckStreams(readStream, writeStream);
            this.CheckDecryptStream(readStream);

            TaskAwaiter awaiter = this.DecryptProcessAsync(readStream, writeStream).GetAwaiter();
            awaiter.GetResult();
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
            using (MemoryStream readStream = new MemoryStream(arr, 0, arr.Length))
            using (MemoryStream writeStream = new MemoryStream())
            {
                await this.DecryptAsync(readStream, writeStream, progress, cancellationToken)
                    .ConfigureAwait(false);
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
        public Task DecryptAsync(
            Stream readStream,
            Stream writeStream,
            IProgress<CryptoStatus> progress = default,
            CancellationToken cancellationToken = default)
        {
            this.CheckStreams(readStream, writeStream);
            this.CheckDecryptStream(readStream);

            return Task.Run(async () => 
                await this.DecryptProcessAsync(
                    readStream,
                    writeStream,
                    progress,
                    cancellationToken), cancellationToken);
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
        /// <exception cref="ArgumentException"></exception>
        private void CheckDecryptStream(Stream readStream)
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
        }

        /// <summary>
        /// Выполнение зашифровывания.
        /// </summary>
        /// <param name="readStream">Поток данных.</param>
        /// <param name="writeStream">Поток преобразованных данных.</param>
        /// <param name="progress">Прогресс операции.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        private async Task EncryptProcessAsync(
            Stream readStream,
            Stream writeStream,
            IProgress<CryptoStatus> progress = default,
            CancellationToken cancellationToken = default)
        {
            progress ??= new Progress<CryptoStatus>();

            uint bufferBlockSize = this.bufferLength / CryptoUtils.BlockSize;
            long totalBytes = readStream.Length - readStream.Position;
            long blockCount = totalBytes / CryptoUtils.BlockSize;
            long partCount = blockCount / bufferBlockSize;
            int leftBlockCount = (int)(blockCount - partCount * bufferBlockSize);

            byte paddingLength = (byte)(CryptoUtils.BlockSize - totalBytes % CryptoUtils.BlockSize);

            Memory<byte> buffer = new byte[this.bufferLength];

            for (; partCount > 0; partCount--)
            {
                await this.ProcessBufferAsync(
                    readStream,
                    writeStream,
                    buffer,
                    CryptoUtils.EncryptBlock,
                    progress,
                    cancellationToken);
            }

            buffer = new byte[leftBlockCount * CryptoUtils.BlockSize];

            await this.ProcessBufferAsync(
                readStream,
                writeStream,
                buffer,
                CryptoUtils.EncryptBlock,
                progress,
                cancellationToken);

            buffer = new byte[CryptoUtils.BlockSize];
            await readStream.ReadAsync(buffer, cancellationToken);

            buffer.Span[^1] = paddingLength;

            ref Block block = ref Unsafe.As<byte, Block>(ref MemoryMarshal.GetReference(buffer.Span));
            CryptoUtils.EncryptBlock(ref block, this.parameters);

            await writeStream.WriteAsync(buffer, cancellationToken);

            CryptoStatus status = new CryptoStatus(readStream.Position, readStream.Length, CryptoUtils.BlockSize);
            progress.Report(status);
        }

        /// <summary>
        /// Выполнение расшифровывания.
        /// </summary>
        /// <param name="readStream">Поток данных.</param>
        /// <param name="writeStream">Поток преобразованных данных.</param>
        /// <param name="progress">Прогресс операции.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        private async Task DecryptProcessAsync(
            Stream readStream,
            Stream writeStream,
            IProgress<CryptoStatus> progress = default,
            CancellationToken cancellationToken = default)
        {
            progress ??= new Progress<CryptoStatus>();

            uint bufferBlockSize = this.bufferLength / CryptoUtils.BlockSize;
            long totalBytes = readStream.Length - readStream.Position;
            long blockCount = totalBytes / CryptoUtils.BlockSize - 1;
            long partCount = blockCount / bufferBlockSize;
            int leftBlockCount = (int)(blockCount - partCount * bufferBlockSize);

            Memory<byte> buffer = new byte[this.bufferLength];

            for (; partCount > 0; partCount--)
            {
                await this.ProcessBufferAsync(
                    readStream,
                    writeStream,
                    buffer,
                    CryptoUtils.DecryptBlock,
                    progress,
                    cancellationToken);
            }

            buffer = new byte[leftBlockCount * CryptoUtils.BlockSize];

            await this.ProcessBufferAsync(
                readStream,
                writeStream,
                buffer,
                CryptoUtils.DecryptBlock,
                progress,
                cancellationToken);

            buffer = new byte[CryptoUtils.BlockSize];
            await readStream.ReadAsync(buffer, cancellationToken);

            ref Block block = ref Unsafe.As<byte, Block>(ref MemoryMarshal.GetReference(buffer.Span));

            CryptoUtils.DecryptBlock(ref block, this.parameters);

            byte paddingLength = buffer.Span[^1];

            await writeStream.WriteAsync(buffer[..(CryptoUtils.BlockSize - paddingLength)], cancellationToken);

            CryptoStatus status = new CryptoStatus(readStream.Position, readStream.Length, CryptoUtils.BlockSize);
            progress.Report(status);
        }

        /// <summary>
        /// Обработать часть блоков буфера.
        /// </summary>
        /// <param name="readStream">Поток данных.</param>
        /// <param name="writeStream">Поток преобразованных данных.</param>
        /// <param name="buffer">Буфер.</param>
        /// <param name="action">Делегат для зашифровывания или расшифрования блока.</param>
        /// <param name="progress">Прогресс операции.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        private async Task ProcessBufferAsync(
            Stream readStream,
            Stream writeStream,
            Memory<byte> buffer,
            CryptBlockDelegate action,
            IProgress<CryptoStatus> progress,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await readStream.ReadAsync(buffer, cancellationToken);

            Span<Block> blocks = MemoryMarshal.Cast<byte, Block>(buffer.Span);

            for (int i = 0; i < blocks.Length; i++)
            {
                action.Invoke(ref blocks[i], this.parameters);
            }

            await writeStream.WriteAsync(buffer, cancellationToken);

            CryptoStatus status = new CryptoStatus(readStream.Position, readStream.Length, this.bufferLength);
            progress.Report(status);
        }
    }
}
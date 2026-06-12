using System;
using System.Buffers;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
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
        /// Делегат шифрования.
        /// </summary>
        private readonly CryptBlockDelegate encryptDelegate;

        /// <summary>
        /// Делегат расшифровывания.
        /// </summary>
        private readonly CryptBlockDelegate decryptDelegate;

        /// <summary>
        /// Создание шифратора.
        /// </summary>
        /// <param name="parameters">Параметры шифратора.</param>
        public Scrambler(CryptoParameters parameters)
        {
            this.bufferLength = UInt16.MaxValue + 1;
            this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));

            this.encryptDelegate = parameters.CryptoUtils.EncryptBlock;
            this.decryptDelegate = parameters.CryptoUtils.DecryptBlock;
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

                byte[] buffer = writeStream.GetBuffer();
                Array.Resize(ref buffer, (int)writeStream.Length);
                arr = buffer;
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
            CheckStreams(readStream, writeStream);

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

                byte[] buffer = writeStream.GetBuffer();
                Array.Resize(ref buffer, (int)writeStream.Length);
                return buffer;
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
            CheckStreams(readStream, writeStream);

            return Task.Run(() => 
                this.EncryptProcessAsync(
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

                byte[] buffer = writeStream.GetBuffer();
                Array.Resize(ref buffer, (int)writeStream.Length);
                arr = buffer;
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
            CheckStreams(readStream, writeStream);
            CheckDecryptStream(readStream);

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

                byte[] buffer = writeStream.GetBuffer();
                Array.Resize(ref buffer, (int)writeStream.Length);
                return buffer;
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
            CheckStreams(readStream, writeStream);
            CheckDecryptStream(readStream);

            return Task.Run(() => 
                this.DecryptProcessAsync(
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
        private static void CheckStreams(Stream readStream, Stream writeStream)
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
        private static void CheckDecryptStream(Stream readStream)
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

            byte[] bufferArr = ArrayPool<byte>.Shared.Rent((int)this.bufferLength);
            Memory<byte> buffer = bufferArr;

            try
            {
                for (; partCount > 0; partCount--)
                {
                    await this.ProcessBufferAsync(
                        readStream,
                        writeStream,
                        buffer,
                        this.encryptDelegate,
                        progress,
                        cancellationToken);
                }

                buffer = bufferArr.AsMemory(0, leftBlockCount * CryptoUtils.BlockSize);

                await this.ProcessBufferAsync(
                    readStream,
                    writeStream,
                    buffer,
                    this.encryptDelegate,
                    progress,
                    cancellationToken);

                buffer = bufferArr.AsMemory(0, CryptoUtils.BlockSize);
                _ = await readStream.ReadAsync(buffer, cancellationToken);

                buffer.Span[^1] = paddingLength;

                Vector128<byte> block = Vector128.Create(buffer.Span);
                this.encryptDelegate(ref block);

                await writeStream.WriteAsync(buffer, cancellationToken);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bufferArr);
            }

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

            byte[] bufferArr = ArrayPool<byte>.Shared.Rent((int)this.bufferLength);
            Memory<byte> buffer = bufferArr;

            try
            {
                for (; partCount > 0; partCount--)
                {
                    await this.ProcessBufferAsync(
                        readStream,
                        writeStream,
                        buffer,
                        this.decryptDelegate,
                        progress,
                        cancellationToken);
                }

                buffer = bufferArr.AsMemory(0, leftBlockCount * CryptoUtils.BlockSize);

                await this.ProcessBufferAsync(
                    readStream,
                    writeStream,
                    buffer,
                    this.decryptDelegate,
                    progress,
                    cancellationToken);

                buffer = bufferArr.AsMemory(0, CryptoUtils.BlockSize);
                _ = await readStream.ReadAsync(buffer, cancellationToken);

                Vector128<byte> block = Vector128.Create(buffer.Span);
                this.decryptDelegate(ref block);

                byte paddingLength = buffer.Span[^1];

                await writeStream.WriteAsync(buffer[..(CryptoUtils.BlockSize - paddingLength)], cancellationToken);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bufferArr);
            }

            CryptoStatus status = new CryptoStatus(readStream.Position, readStream.Length, CryptoUtils.BlockSize);
            progress.Report(status);
        }

        /// <summary>
        /// Обработка части буфера с блоками.
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
            await readStream.ReadExactlyAsync(buffer, cancellationToken);

            Span<Vector128<byte>> blocks = MemoryMarshal.Cast<byte, Vector128<byte>>(buffer.Span);

            for (int i = 0; i < blocks.Length; i++)
            {
                action.Invoke(ref blocks[i]);
            }

            await writeStream.WriteAsync(buffer, cancellationToken);

            CryptoStatus status = new CryptoStatus(readStream.Position, readStream.Length, this.bufferLength);
            progress.Report(status);
        }
    }
}
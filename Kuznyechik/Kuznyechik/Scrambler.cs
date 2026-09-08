using System;
using System.Buffers;
using System.IO;
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
    public abstract class Scrambler<TContext> where TContext : struct
    {
        /// <summary>
        /// Размер буфера.
        /// </summary>
        protected int bufferLength;

        /// <summary>
        /// Параметры шифрования.
        /// </summary>
        protected readonly CryptoParameters parameters;

        /// <summary>
        /// Делегат шифрования.
        /// </summary>
        internal readonly CryptBlockDelegate encryptDelegate;

        /// <summary>
        /// Делегат расшифровывания.
        /// </summary>
        internal readonly CryptBlockDelegate decryptDelegate;

        /// <summary>
        /// Размер буфера.
        /// </summary>
        public int BufferLength
        {
            get => this.bufferLength;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(this.BufferLength), "Размер буфера должен быть больше нуля.");
                }
                else if (value % CryptoUtils.BlockSize != 0)
                {
                    throw new ArgumentException("Размер буфера обязан быть кратен размеру блока (16 байт).");
                }

                this.bufferLength = value;
            }
        }

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
        public Scrambler(ReadOnlySpan<byte> key) : this(new CryptoParameters(key)) { }

        /// <summary>
        /// Шифрование блока данных.
        /// </summary>
        /// <param name="source">Источник данных.</param>
        /// <returns>Зашифрованные данные.</returns>
        public virtual Span<byte> Encrypt(ReadOnlySpan<byte> source)
        {
            long destinationLength = this.GetEncryptLength(source.Length);
            Span<byte> destination = new byte[destinationLength];

            TContext context = this.Initialize(source, destination);

            ref byte sourceRef = ref MemoryMarshal.GetReference(source);
            ref byte destinationRef = ref MemoryMarshal.GetReference(destination);

            int fullBlocksLength = source.Length - source.Length % CryptoUtils.BlockSize;
            int dataPosition = 0;

            for (; dataPosition < fullBlocksLength; dataPosition += CryptoUtils.BlockSize)
            {
                nuint offset = (nuint)dataPosition;

                Vector128<byte> block = Vector128.LoadUnsafe(ref sourceRef, offset);
                block = this.PreprocessEncrypt(block, ref context);

                this.encryptDelegate(ref block);

                Vector128.StoreUnsafe(block, ref destinationRef, offset);
            }

            ReadOnlySpan<byte> lastBytes = source[dataPosition..];
            Vector128<byte> lastBlock = this.ProcessLastBlock(lastBytes, ref context);

            this.encryptDelegate(ref lastBlock);

            Vector128.StoreUnsafe(lastBlock, ref destinationRef, (nuint)dataPosition);

            return destination;
        }

        /// <summary>
        /// Шифрование потока данных.
        /// </summary>
        /// <param name="sourceStream">Поток данных.</param>
        /// <param name="destinationStream">Целевой поток данных.</param>
        public virtual void Encrypt(Stream sourceStream, Stream destinationStream)
        {
            CheckStreams(sourceStream, destinationStream);
            TContext context = this.Initialize(sourceStream, destinationStream);

            long sourceLength = sourceStream.Length - sourceStream.Position;
            int blocksPerBuffer = this.bufferLength / CryptoUtils.BlockSize;
            long fullBufferCount = sourceLength / this.bufferLength;
            int remainingBytes = (int)(sourceLength % this.bufferLength);

            byte[] bufferArr = ArrayPool<byte>.Shared.Rent(this.bufferLength);
            Span<byte> buffer = bufferArr.AsSpan(0, this.bufferLength);
            ref byte bufferRef = ref MemoryMarshal.GetReference(buffer);

            nuint bufferPosition;

            try
            {
                for (int i = 0; i < fullBufferCount; i++)
                {
                    sourceStream.ReadExactly(buffer);
                    bufferPosition = 0;

                    for (int j = 0; j < blocksPerBuffer; j++)
                    {
                        Vector128<byte> block = Vector128.LoadUnsafe(in bufferRef, bufferPosition);

                        block = this.PreprocessEncrypt(block, ref context);

                        this.encryptDelegate(ref block);
                        block.StoreUnsafe(ref bufferRef, bufferPosition);

                        bufferPosition += CryptoUtils.BlockSize;
                    }

                    destinationStream.Write(buffer);
                }

                int tailBlockLeft = remainingBytes / CryptoUtils.BlockSize;
                int tailBlocksBytes = tailBlockLeft * CryptoUtils.BlockSize;

                if (tailBlockLeft > 0)
                {
                    buffer = buffer[..tailBlocksBytes];
                    sourceStream.ReadExactly(buffer);

                    bufferPosition = 0;

                    for (int i = 0; i < tailBlockLeft; i++)
                    {
                        Vector128<byte> block = Vector128.LoadUnsafe(in bufferRef, bufferPosition);

                        block = this.PreprocessEncrypt(block, ref context);

                        this.encryptDelegate(ref block);
                        block.StoreUnsafe(ref bufferRef, bufferPosition);

                        bufferPosition += CryptoUtils.BlockSize;
                    }

                    destinationStream.Write(buffer);
                }

                int lastBytes = remainingBytes - tailBlocksBytes;

                Span<byte> tail = buffer[..lastBytes];
                buffer = buffer[..CryptoUtils.BlockSize];

                sourceStream.ReadExactly(tail);

                Vector128<byte> lastBlock = this.ProcessLastBlock(tail, ref context);

                this.encryptDelegate(ref lastBlock);
                Vector128.StoreUnsafe(lastBlock, ref bufferRef);

                destinationStream.Write(buffer);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bufferArr);
            }
        }

        /// <summary>
        /// Расшифровывание блока данных.
        /// </summary>
        /// <param name="source">Источник данных.</param>
        /// <returns>Расшифрованные данные.</returns>
        public virtual Span<byte> Decrypt(ReadOnlySpan<byte> source)
        {
            if (source.Length % CryptoUtils.BlockSize != 0)
            {
                throw new ArgumentException("Размер зашифрованных данных должен быть кратен размеру блока.", nameof(source));
            }

            Span<byte> destination = new byte[source.Length];

            TContext context = this.Initialize(source, destination);

            ref byte sourceRef = ref MemoryMarshal.GetReference(source);
            ref byte destinationRef = ref MemoryMarshal.GetReference(destination);

            int fullBlocksLength = source.Length - source.Length % CryptoUtils.BlockSize;
            int dataPosition = 0;

            for (; dataPosition < fullBlocksLength; dataPosition += CryptoUtils.BlockSize)
            {
                nuint offset = (nuint)dataPosition;

                Vector128<byte> block = Vector128.LoadUnsafe(ref sourceRef, offset);
                this.decryptDelegate(ref block);

                block = this.PostprocessDecrypt(block, ref context);
                Vector128.StoreUnsafe(block, ref destinationRef, offset);
            }

            this.RemovePadding(ref destination, ref context);

            return destination;
        }

        /// <summary>
        /// Расшифровывание потока данных.
        /// </summary>
        /// <param name="sourceStream">Поток данных.</param>
        /// <param name="destinationStream">Целевой поток данных.</param>
        public virtual void Decrypt(Stream sourceStream, Stream destinationStream)
        {
            CheckStreams(sourceStream, destinationStream);
            TContext context = this.Initialize(sourceStream, destinationStream);

            long sourceLength = sourceStream.Length - sourceStream.Position;
            int blocksPerBuffer = this.bufferLength / CryptoUtils.BlockSize;
            long fullBufferCount = sourceLength / this.bufferLength;

            if (sourceLength % CryptoUtils.BlockSize != 0)
            {
                throw new ArgumentException("Размер зашифрованного потока должен быть кратен размеру блока.", nameof(sourceStream));
            }

            byte[] bufferArr = ArrayPool<byte>.Shared.Rent(this.bufferLength);
            Span<byte> buffer = bufferArr.AsSpan(0, this.bufferLength);
            ref byte bufferRef = ref MemoryMarshal.GetReference(buffer);

            nuint bufferPosition;

            try
            {
                for (int i = 0; i < fullBufferCount; i++)
                {
                    sourceStream.ReadExactly(buffer);
                    bufferPosition = 0;

                    for (int j = 0; j < blocksPerBuffer; j++)
                    {
                        Vector128<byte> block = Vector128.LoadUnsafe(in bufferRef, bufferPosition);

                        this.decryptDelegate(ref block);
                        block = this.PostprocessDecrypt(block, ref context);

                        Vector128.StoreUnsafe(block, ref bufferRef, bufferPosition);

                        bufferPosition += CryptoUtils.BlockSize;
                    }

                    destinationStream.Write(buffer);
                }

                int tailSize = (int)(sourceLength - fullBufferCount * this.bufferLength);
                int blockLeft = tailSize / CryptoUtils.BlockSize;

                buffer = buffer[..tailSize];
                sourceStream.ReadExactly(buffer);

                bufferPosition = 0;

                for (int i = 0; i < blockLeft; i++)
                {
                    Vector128<byte> block = Vector128.LoadUnsafe(in bufferRef, bufferPosition);

                    this.decryptDelegate(ref block);
                    block = this.PostprocessDecrypt(block, ref context);

                    Vector128.StoreUnsafe(block, ref bufferRef, bufferPosition);

                    bufferPosition += CryptoUtils.BlockSize;
                }

                this.RemovePadding(ref buffer, ref context);

                destinationStream.Write(buffer);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bufferArr);
            }
        }

        /// <summary>
        /// Шифрование потока данных.
        /// </summary>
        /// <param name="sourceStream">Поток данных.</param>
        /// <param name="destinationStream">Целевой поток данных.</param>
        /// <param name="progress">Прогресс.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача шифрования.</returns>
        public virtual async Task EncryptAsync(Stream sourceStream,
            Stream destinationStream, 
            IProgress<CryptoStatus> progress = default,
            CancellationToken cancellationToken = default)
        {
            CheckStreams(sourceStream, destinationStream);
            TContext context = this.Initialize(sourceStream, destinationStream);

            long sourceLength = sourceStream.Length - sourceStream.Position;
            int blocksPerBuffer = this.bufferLength / CryptoUtils.BlockSize;
            long fullBufferCount = sourceLength / this.bufferLength;
            int remainingBytes = (int)(sourceLength % this.bufferLength);

            byte[] bufferArr = ArrayPool<byte>.Shared.Rent(this.bufferLength);
            Memory<byte> buffer = bufferArr.AsMemory(0, this.bufferLength);

            cancellationToken.ThrowIfCancellationRequested();

            nuint bufferPosition;
            long currentDataPosition = 0;

            try
            {
                for (int i = 0; i < fullBufferCount; i++)
                {
                    await sourceStream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);

                    ref byte bufferRef = ref MemoryMarshal.GetReference(buffer.Span);
                    bufferPosition = 0;

                    for (int j = 0; j < blocksPerBuffer; j++)
                    {
                        Vector128<byte> block = Vector128.LoadUnsafe(in bufferRef, bufferPosition);

                        block = this.PreprocessEncrypt(block, ref context);

                        this.encryptDelegate(ref block);
                        block.StoreUnsafe(ref bufferRef, bufferPosition);

                        bufferPosition += CryptoUtils.BlockSize;
                    }

                    await destinationStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

                    if (progress != null)
                    {
                        currentDataPosition += this.bufferLength;

                        CryptoStatus status = new CryptoStatus(currentDataPosition, sourceLength, this.bufferLength);
                        progress.Report(status);
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                }

                int tailBlockLeft = remainingBytes / CryptoUtils.BlockSize;
                int tailBlocksBytes = tailBlockLeft * CryptoUtils.BlockSize;

                if (tailBlockLeft > 0)
                {
                    buffer = buffer[..tailBlocksBytes];
                    await sourceStream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);

                    ref byte tailBufferRef = ref MemoryMarshal.GetReference(buffer.Span);
                    bufferPosition = 0;

                    for (int i = 0; i < tailBlockLeft; i++)
                    {
                        Vector128<byte> block = Vector128.LoadUnsafe(in tailBufferRef, bufferPosition);

                        block = this.PreprocessEncrypt(block, ref context);

                        this.encryptDelegate(ref block);
                        block.StoreUnsafe(ref tailBufferRef, bufferPosition);

                        bufferPosition += CryptoUtils.BlockSize;
                    }

                    await destinationStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

                    if (progress != null)
                    {
                        currentDataPosition += tailBlocksBytes;

                        CryptoStatus status = new CryptoStatus(currentDataPosition, sourceLength, this.bufferLength);
                        progress.Report(status);
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                }

                int lastBytes = remainingBytes - tailBlocksBytes;

                Memory<byte> tail = buffer[..lastBytes];
                buffer = buffer[..CryptoUtils.BlockSize];

                await sourceStream.ReadExactlyAsync(tail, cancellationToken).ConfigureAwait(false);

                ref byte lastBufferRef = ref MemoryMarshal.GetReference(buffer.Span);

                Vector128<byte> lastBlock = this.ProcessLastBlock(tail.Span, ref context);

                this.encryptDelegate(ref lastBlock);
                Vector128.StoreUnsafe(lastBlock, ref lastBufferRef);

                await destinationStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

                if (progress != null)
                {
                    CryptoStatus status = new CryptoStatus(sourceLength, sourceLength, this.bufferLength);
                    progress.Report(status);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bufferArr);
            }
        }

        /// <summary>
        /// Расшифровывание потока данных.
        /// </summary>
        /// <param name="sourceStream">Поток данных.</param>
        /// <param name="destinationStream">Целевой поток данных.</param>
        /// <param name="progress">Прогресс.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        public virtual async Task DecryptAsync(Stream sourceStream,
            Stream destinationStream,
            IProgress<CryptoStatus> progress = default,
            CancellationToken cancellationToken = default)
        {
            CheckStreams(sourceStream, destinationStream);
            TContext context = this.Initialize(sourceStream, destinationStream);

            long sourceLength = sourceStream.Length - sourceStream.Position;
            int blocksPerBuffer = this.bufferLength / CryptoUtils.BlockSize;
            long fullBufferCount = sourceLength / this.bufferLength;

            if (sourceLength % CryptoUtils.BlockSize != 0)
            {
                throw new ArgumentException("Размер зашифрованного потока должен быть кратен размеру блока.", nameof(sourceStream));
            }

            byte[] bufferArr = ArrayPool<byte>.Shared.Rent(this.bufferLength);
            Memory<byte> buffer = bufferArr.AsMemory(0, this.bufferLength);

            cancellationToken.ThrowIfCancellationRequested();

            nuint bufferPosition;
            long currentDataPosition = 0;

            try
            {
                for (int i = 0; i < fullBufferCount; i++)
                {
                    await sourceStream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
                    ref byte bufferRef = ref MemoryMarshal.GetReference(buffer.Span);
                    bufferPosition = 0;

                    for (int j = 0; j < blocksPerBuffer; j++)
                    {
                        Vector128<byte> block = Vector128.LoadUnsafe(in bufferRef, bufferPosition);

                        this.decryptDelegate(ref block);
                        block = this.PostprocessDecrypt(block, ref context);

                        Vector128.StoreUnsafe(block, ref bufferRef, bufferPosition);

                        bufferPosition += CryptoUtils.BlockSize;
                    }

                    await destinationStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

                    if (progress != null)
                    {
                        currentDataPosition += this.bufferLength;

                        CryptoStatus status = new CryptoStatus(currentDataPosition, sourceLength, this.bufferLength);
                        progress.Report(status);
                    }

                    cancellationToken.ThrowIfCancellationRequested();
                }

                int tailSize = (int)(sourceLength - fullBufferCount * this.bufferLength);
                int blockLeft = tailSize / CryptoUtils.BlockSize;

                buffer = buffer[..tailSize];

                await sourceStream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
                ref byte tailBufferRef = ref MemoryMarshal.GetReference(buffer.Span);
                bufferPosition = 0;

                for (int i = 0; i < blockLeft; i++)
                {
                    Vector128<byte> block = Vector128.LoadUnsafe(in tailBufferRef, bufferPosition);

                    this.decryptDelegate(ref block);
                    block = this.PostprocessDecrypt(block, ref context);

                    Vector128.StoreUnsafe(block, ref tailBufferRef, bufferPosition);

                    bufferPosition += CryptoUtils.BlockSize;
                }

                Span<byte> bufferSpan = buffer.Span;

                this.RemovePadding(ref bufferSpan, ref context);
                buffer = buffer[..bufferSpan.Length];

                await destinationStream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);

                if (progress != null)
                {
                    CryptoStatus status = new CryptoStatus(sourceLength, sourceLength, this.bufferLength);
                    progress.Report(status);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bufferArr);
            }
        }

        /// <summary>
        /// Проверка потоков.
        /// </summary>
        /// <param name="sourceStream">Поток данных.</param>
        /// <param name="destinationStream">Целевой поток данных.</param>
        protected static void CheckStreams(Stream sourceStream, Stream destinationStream)
        {
            if (sourceStream is null)
            {
                throw new ArgumentNullException(nameof(sourceStream), "Поток данных не может быть null.");
            }
            else if (destinationStream is null)
            {
                throw new ArgumentNullException(nameof(destinationStream), "Целевой поток не может быть null.");
            }
            else if (!sourceStream.CanRead)
            {
                throw new ArgumentException("Поток данных должен быть доступен для чтения.", nameof(sourceStream));
            }
            else if (!destinationStream.CanWrite)
            {
                throw new ArgumentException("Целевой поток должен быть доступен для записи.", nameof(destinationStream));
            }
            else if (!sourceStream.CanSeek)
            {
                throw new ArgumentException("Поток данных не поддерживает поиск (Seek).", nameof(sourceStream));
            }
            else if (!destinationStream.CanSeek)
            {
                throw new ArgumentException("Целевой поток не поддерживает поиск (Seek).", nameof(destinationStream));
            }
        }

        /// <summary>
        /// Получение длины зашифрованных данных.
        /// </summary>
        /// <param name="dataLength">Длина данных.</param>
        /// <returns>Длина зашифрованных данных.</returns>
        protected abstract long GetEncryptLength(long dataLength);

        /// <summary>
        /// Инициализация шифрования.
        /// </summary>
        /// <param name="source">Источник данных.</param>
        /// <param name="destination">Целевая область данных.</param>
        /// <returns>Контекст.</returns>
        protected abstract TContext Initialize(ReadOnlySpan<byte> source, Span<byte> destination);

        /// <summary>
        /// Инициализация шифрования.
        /// </summary>
        /// <param name="sourceStream">Поток источника.</param>
        /// <param name="destinationStream">Целевой поток.</param>
        /// <returns>Контекст.</returns>
        protected abstract TContext Initialize(Stream sourceStream, Stream destinationStream);

        /// <summary>
        /// Предварительная обработка данных перед шифрованием.
        /// </summary>
        /// <param name="block">Блок данных.</param>
        /// <param name="context">Контекст.</param>
        /// <returns>Обработанный блок.</returns>
        protected abstract Vector128<byte> PreprocessEncrypt(Vector128<byte> block, ref TContext context);

        /// <summary>
        /// Предварительная обработка данных после расшифровывания.
        /// </summary>
        /// <param name="block">Блок данных.</param>
        /// <param name="context">Контекст.</param>
        /// <returns>Обработанный блок.</returns>
        protected abstract Vector128<byte> PostprocessDecrypt(Vector128<byte> block, ref TContext context);

        /// <summary>
        /// Обработка остатка данных, не вошедших в блок.
        /// </summary>
        /// <param name="lastBytes">Последние байты, не вошедшие в блок.</param>
        /// <param name="context">Контекст.</param>
        /// <returns>Последний блок.</returns>
        protected abstract Vector128<byte> ProcessLastBlock(ReadOnlySpan<byte> lastBytes, ref TContext context);

        /// <summary>
        /// Удаление дополнения.
        /// </summary>
        /// <param name="data">Данные.</param>
        /// <param name="context">Контекст.</param>
        protected abstract void RemovePadding(scoped ref Span<byte> data, scoped ref TContext context);
    }
}
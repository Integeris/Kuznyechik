using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.CPU;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.OpenCL;
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
    public sealed class Scrambler: IDisposable
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
        /// Контекст ILGPU.
        /// </summary>
        private readonly Context context;

        /// <summary>
        /// Устройство.
        /// </summary>
        private Device device;

        /// <summary>
        /// Акселератор.
        /// </summary>
        private Accelerator accelerator;

        /// <summary>
        /// Параметры.
        /// </summary>
        public CryptoParameters Parameters
        {
            get => parameters;
        }

        /// <summary>
        /// Устройство.
        /// </summary>
        public Device Device
        {
            get => device;
            set
            {
                accelerator?.Dispose();

                device = value;
                accelerator = device.CreateAccelerator(context);
            }
        }

        /// <summary>
        /// Создание шифратора.
        /// </summary>
        /// <param name="parameters">Параметры шифратора</param>
        public Scrambler(CryptoParameters parameters)
        {
            this.parameters = parameters;

            context = Context.Create(builder =>
            {
                builder
                .Cuda()
                .OpenCL()
                .CPU()
                .Math(MathMode.Fast)
                .Optimize(OptimizationLevel.O2);
            });

            Device = context.GetPreferredDevice(false);
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
            Dispose();
        }

        /// <summary>
        /// Шифрование массива блоков.
        /// </summary>
        /// <param name="arr">Массив.</param>
        /// <exception cref="ArgumentException"></exception>
        public void Encrypt(ref byte[] arr)
        {
            using (MemoryStream dataStream = new MemoryStream())
            using (MemoryStream encryptedStream = new MemoryStream())
            {
                dataStream.Write(arr, 0, arr.Length);
                dataStream.Position = 0;
                Encrypt(dataStream, encryptedStream);
                arr = encryptedStream.ToArray();
            }
        }

        /// <summary>
        /// Шифрование данных из потока в поток.
        /// </summary>
        /// <param name="dataStream">Поток данных.</param>
        /// <param name="encryptedStream">Выходной поток с зашифрованными данными.</param>
        /// <exception cref="ArgumentException"></exception>
        public void Encrypt(Stream dataStream, Stream encryptedStream)
        {
            ProcessData(dataStream, encryptedStream, Kernel.Encrypt, parameters.ReplaceBytes);
            AddBlockPadding(dataStream, encryptedStream);
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
            using (MemoryStream dataStream = new MemoryStream())
            using (MemoryStream encryptedStream = new MemoryStream())
            {
                dataStream.Write(arr, 0, arr.Length);
                dataStream.Position = 0;
                await EncryptAsync(dataStream, encryptedStream, progress, cancellationToken);
                return encryptedStream.ToArray();
            }
        }

        /// <summary>
        /// Шифрование данных из потока в поток.
        /// </summary>
        /// <param name="dataStream">Поток данных.</param>
        /// <param name="encryptedStream">Выходной поток с зашифрованными данными.</param>
        /// <param name="progress">Прогресс шифрования.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача шифрования.</returns>
        public async Task EncryptAsync(
            Stream dataStream, 
            Stream encryptedStream,
            IProgress<CryptoStatus> progress = default,
            CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                ProcessData(
                    dataStream,
                    encryptedStream,
                    Kernel.Encrypt,
                    parameters.ReplaceBytes,
                    progress,
                    cancellationToken);

                AddBlockPadding(dataStream, encryptedStream);
            });
        }

        /// <summary>
        /// Расшифрование массива.
        /// </summary>
        /// <param name="arr">Массив.</param>
        /// <exception cref="ArgumentException"></exception>
        public void Decrypt(ref byte[] arr)
        {
            using (MemoryStream dataStream = new MemoryStream())
            using (MemoryStream decryptedStream = new MemoryStream())
            {
                dataStream.Write(arr, 0, arr.Length);
                dataStream.Position = 0;
                Decrypt(dataStream, decryptedStream);
                arr = decryptedStream.ToArray();
            }
        }
        
        /// <summary>
        /// Расшифрование данных из потока в поток.
        /// </summary>
        /// <param name="dataStream">Поток данных.</param>
        /// <param name="decryptedStream">Выходной поток с расшифрованными данными.</param>
        /// <exception cref="ArgumentException"></exception>
        public void Decrypt(Stream dataStream, Stream decryptedStream)
        {
            CheckDecryptStream(dataStream, decryptedStream);
            ProcessData(dataStream, decryptedStream, Kernel.Decrypt, parameters.ReverseReplaceBytes);
            RemoveDecryptPadding(decryptedStream);
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
            using (MemoryStream dataStream = new MemoryStream())
            using (MemoryStream decryptedStream = new MemoryStream())
            {
                dataStream.Write(arr, 0, arr.Length);
                dataStream.Position = 0;
                await DecryptAsync(dataStream, decryptedStream, progress, cancellationToken);
                return decryptedStream.ToArray();
            }
        }

        /// <summary>
        /// Расшифрование данных из потока в поток.
        /// </summary>
        /// <param name="dataStream">Поток данных.</param>
        /// <param name="decryptedStream">Выходной поток с расшифрованными данными.</param>
        /// <param name="progress">Прогресс расшифровки.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        /// <returns>Задача расшифровки.</returns>
        /// <exception cref="ArgumentException"></exception>
        public async Task DecryptAsync(
            Stream dataStream, 
            Stream decryptedStream,
            IProgress<CryptoStatus> progress = default,
            CancellationToken cancellationToken = default)
        {
            await Task.Run(() =>
            {
                CheckDecryptStream(dataStream, decryptedStream);

                ProcessData(
                    dataStream,
                    decryptedStream,
                    Kernel.Decrypt,
                    parameters.ReverseReplaceBytes,
                    progress,
                    cancellationToken);

                RemoveDecryptPadding(decryptedStream);
            });
        }

        /// <summary>
        /// Освобождение неуправляемых ресурсов.
        /// </summary>
        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            accelerator.Dispose();
            context.Dispose();
            device = null;

            GC.SuppressFinalize(this);
            disposed = true;
        }

        /// <summary>
        /// Получение всех устройств.
        /// </summary>
        /// <returns>Все устройства.</returns>
        public ImmutableArray<Device> GetDevices()
        {
            return context.Devices;
        }

        /// <summary>
        /// Проверка потоков.
        /// </summary>
        /// <param name="dataStream">Поток чтения данных.</param>
        /// <param name="writeStream">Поток для записи.</param>
        /// <exception cref="ArgumentException"></exception>
        private void CheckStreams(Stream dataStream, Stream writeStream)
        {
            if (dataStream == null)
            {
                throw new ArgumentException("Поток данных не может быть null.", nameof(dataStream));
            }
            else if (writeStream == null)
            {
                throw new ArgumentException("Поток для записи данных не может быть null.", nameof(writeStream));
            }
            else if (!writeStream.CanWrite)
            {
                throw new ArgumentException("Поток для записи должен быть доступен для записи.", nameof(writeStream));
            }
            else if (dataStream == writeStream)
            {
                throw new ArgumentException("Нельзя выполнить чтение и запись в один и тот же поток.", nameof(writeStream));
            }
        }

        /// <summary>
        /// Проверка потоков.
        /// </summary>
        /// <param name="dataStream">Поток чтения данных.</param>
        /// <param name="writeStream">Поток для записи.</param>
        /// <exception cref="ArgumentException"></exception>
        private void CheckDecryptStream(Stream dataStream, Stream writeStream)
        {
            if (dataStream.Length == 0)
            {
                throw new ArgumentException("Некорректный размер потока данных: размер не может быть равен нулю.",
                    nameof(dataStream));
            }
            else if (dataStream.Length % CryptoUtils.BlockSize != 0)
            {
                throw new ArgumentException("Некорректный размер потока данных: размер должен быть кратен размеру блока.",
                    nameof(dataStream));
            }
            else if (!writeStream.CanRead)
            {
                throw new ArgumentException("Поток записи должен быть доступен для чтения.", nameof(writeStream));
            }

            CheckStreams(dataStream, writeStream);
        }

        /// <summary>
        /// Добавление недостающих данных для полноты блока.
        /// </summary>
        /// <param name="dataStream">Поток данных.</param>
        /// <param name="encryptedStream">Выходной поток с зашифрованными данными.</param>
        /// <returns>Количество добавленных байт.</returns>
        private void AddBlockPadding(Stream dataStream, Stream encryptedStream)
        {
            byte tailLength = (byte)(dataStream.Length % CryptoUtils.BlockSize);
            byte[] padding = new byte[CryptoUtils.BlockSize];

            dataStream.Read(padding, 0, tailLength);
            padding[^1] = (byte)(CryptoUtils.BlockSize - tailLength);

            using (MemoryStream paddingStream = new MemoryStream(padding))
            {
                ProcessData(paddingStream, encryptedStream, Kernel.Encrypt, parameters.ReplaceBytes);
            }
        }

        /// <summary>
        /// Удаление заполнения шифрования.
        /// </summary>
        /// <param name="decryptedStream">Выходной поток с расшифрованными данными.</param>
        /// <exception cref="ArgumentException"></exception>
        private void RemoveDecryptPadding(Stream decryptedStream)
        {
            decryptedStream.Position -= 1;
            int paddingLength = decryptedStream.ReadByte();

            if (paddingLength > CryptoUtils.BlockSize)
            {
                throw new ArgumentException("Некорректный размер дополнения. Данные могут быть повреждены.");
            }

            decryptedStream.SetLength(decryptedStream.Length - paddingLength);
        }

        /// <summary>
        /// Выполнение операции на устройстве.
        /// </summary>
        /// <param name="dataStream">Поток данных.</param>
        /// <param name="writeStream">Поток преобразованных данных.</param>
        /// <param name="action">Действие.</param>
        /// <param name="replaceBytes">Массив для нелинейного преобразования.</param>
        /// <param name="progress">Прогресс операции.</param>
        /// <param name="cancellationToken">Токен отмены операции.</param>
        private void ProcessData(
            Stream dataStream, 
            Stream writeStream, 
            Action<Index1D,
                ArrayView<byte>,
                ArrayView<byte>,
                ArrayView<byte>,
                ArrayView<byte>> action, 
            byte[] replaceBytes,
            IProgress<CryptoStatus> progress = default,
            CancellationToken cancellationToken = default)
        {
            long usefulDataLength = dataStream.Length - (dataStream.Length % CryptoUtils.BlockSize);

            if (usefulDataLength == 0)
            {
                return;
            }

            dataStream.Position = 0;
            progress ??= new Progress<CryptoStatus>();

            using (MemoryBuffer1D<byte, Stride1D.Dense> keysBuffer = 
                accelerator.Allocate1D(parameters.FlatKeys))
            using (MemoryBuffer1D<byte, Stride1D.Dense> linearTransformationBuffer = 
                accelerator.Allocate1D(parameters.LinearTransformation))
            using (MemoryBuffer1D<byte, Stride1D.Dense> replaceBytesBuffer = 
                accelerator.Allocate1D(replaceBytes))
            {
                var kernel = accelerator.LoadAutoGroupedStreamKernel(action);

                for (long i = usefulDataLength; i > 0;)
                {
                    using (MemoryBuffer1D<byte, Stride1D.Dense> dataBuffer = GetMaxBuffer(i))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        Index1D index = new Index1D((int)(dataBuffer.Length / CryptoUtils.BlockSize));
                        byte[] buffer = new byte[dataBuffer.Length];

                        dataStream.Read(buffer, 0, buffer.Length);
                        dataBuffer.CopyFromCPU(buffer);

                        kernel(index, dataBuffer.View, keysBuffer.View, linearTransformationBuffer.View, replaceBytesBuffer.View);
                        accelerator.Synchronize();

                        dataBuffer.CopyToCPU(buffer);
                        writeStream.Write(buffer, 0, buffer.Length);

                        i -= buffer.Length;

                        CryptoStatus status = new CryptoStatus(dataStream.Position, usefulDataLength, buffer.LongLength);
                        progress.Report(status);
                    }
                }
            }
        }

        /// <summary>
        /// Получение буфера макимального размера.
        /// </summary>
        /// <param name="initSize">Максимальный нужный размер.</param>
        /// <returns>Буфер максимального размера.</returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="OutOfMemoryException"></exception>
        private MemoryBuffer1D<byte, Stride1D.Dense> GetMaxBuffer(long initSize)
        {
            initSize = Math.Min(initSize, device.MemorySize);

            for (; initSize >= CryptoUtils.BlockSize; initSize = (long)(initSize * 0.8))
            {
                try
                {
                    byte[] test = new byte[initSize];
                    initSize -= initSize % CryptoUtils.BlockSize;
                    return accelerator.Allocate1D<byte>(initSize);
                }
                catch (Exception) { }
            }
            
            throw new OutOfMemoryException("Недостаточно памяти даже для минимального буфера.");
        }
    }
}
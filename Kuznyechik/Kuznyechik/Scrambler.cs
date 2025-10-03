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
            get => this.parameters;
        }

        /// <summary>
        /// Устройство.
        /// </summary>
        public Device Device
        {
            get => this.device;
            set
            {
                this.accelerator?.Dispose();

                this.device = value;
                this.accelerator = this.device.CreateAccelerator(this.context);
            }
        }

        /// <summary>
        /// Создание шифратора.
        /// </summary>
        /// <param name="parameters">Параметры шифратора</param>
        public Scrambler(CryptoParameters parameters)
        {
            this.parameters = parameters;

            this.context = Context.Create(builder => builder
                .Cuda()
                .OpenCL()
                .CPU()
                .Math(MathMode.Fast)
                .Optimize(OptimizationLevel.O2));

            this.Device = this.context.GetPreferredDevice(false);
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
            if (this.disposed)
            {
                return;
            }

            this.accelerator.Dispose();
            this.context.Dispose();
            this.device = null;

            GC.SuppressFinalize(this);
            this.disposed = true;
        }

        /// <summary>
        /// Получение всех устройств.
        /// </summary>
        /// <returns>Все устройства.</returns>
        public ImmutableArray<Device> GetDevices()
        {
            return this.context.Devices;
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
            long readableBytes = readStream.Length - readStream.Position;
            long byteLength = readableBytes + (CryptoUtils.BlockSize - readableBytes % CryptoUtils.BlockSize);
            long byteLengthLoss = byteLength;

            progress ??= new Progress<CryptoStatus>();

            using (MemoryBuffer1D<byte, Stride1D.Dense> keysBuffer =
                this.accelerator.Allocate1D(this.parameters.FlatKeys))
            using (MemoryBuffer1D<byte, Stride1D.Dense> linearTransformationBuffer =
                this.accelerator.Allocate1D(this.parameters.LinearTransformation))
            using (MemoryBuffer1D<byte, Stride1D.Dense> replaceBytesBuffer =
                this.accelerator.Allocate1D(this.parameters.ReplaceBytes))
            {
                KernelData kernelData = new KernelData()
                {
                    Keys = keysBuffer.View,
                    LinearTransformation = linearTransformationBuffer.View,
                    ReplaceBytes = replaceBytesBuffer.View
                };

                Action<Index1D, KernelData> kernel = 
                    this.accelerator.LoadAutoGroupedStreamKernel((Action<Index1D, KernelData>)Kernel.Encrypt);

                using (MemoryBuffer1D<byte, Stride1D.Dense> dataBuffer =
                    this.GetMaxBuffer(byteLengthLoss))
                {
                    for (; byteLengthLoss > dataBuffer.Length; byteLengthLoss -= dataBuffer.Length)
                    {
                        this.ProcessBuffer(dataBuffer,
                        readStream,
                        writeStream,
                        kernel,
                        kernelData,
                        byteLength,
                        byteLengthLoss,
                        progress,
                        cancellationToken);
                    }
                }

                using (MemoryBuffer1D<byte, Stride1D.Dense> dataBuffer =
                    this.accelerator.Allocate1D<byte>(byteLengthLoss))
                {
                    kernelData.Data = dataBuffer.View;

                    byte[] paddingCount = new byte[] { (byte)(byteLength - readStream.Length) };
                    dataBuffer.View.SubView(dataBuffer.Length - 1, 1).CopyFromCPU(paddingCount);

                    this.ProcessBuffer(dataBuffer, 
                        readStream, 
                        writeStream, 
                        kernel, 
                        kernelData, 
                        byteLength, 
                        byteLengthLoss, 
                        progress, 
                        cancellationToken);
                }
            }
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
            long byteLength = readStream.Length - readStream.Position;
            long byteLengthLoss = byteLength;

            progress ??= new Progress<CryptoStatus>();

            using (MemoryBuffer1D<byte, Stride1D.Dense> keysBuffer =
                this.accelerator.Allocate1D(this.parameters.FlatKeys))
            using (MemoryBuffer1D<byte, Stride1D.Dense> linearTransformationBuffer =
                this.accelerator.Allocate1D(this.parameters.LinearTransformation))
            using (MemoryBuffer1D<byte, Stride1D.Dense> replaceBytesBuffer =
                this.accelerator.Allocate1D(this.parameters.ReverseReplaceBytes))
            {
                KernelData kernelData = new KernelData()
                {
                    Keys = keysBuffer.View,
                    LinearTransformation = linearTransformationBuffer.View,
                    ReplaceBytes = replaceBytesBuffer.View
                };

                Action<Index1D, KernelData> kernel =
                    this.accelerator.LoadAutoGroupedStreamKernel((Action<Index1D, KernelData>)Kernel.Decrypt);

                using (MemoryBuffer1D<byte, Stride1D.Dense> dataBuffer =
                    this.GetMaxBuffer(byteLengthLoss))
                {
                    for (; byteLengthLoss > dataBuffer.Length; byteLengthLoss -= dataBuffer.Length)
                    {
                        this.ProcessBuffer(dataBuffer,
                        readStream,
                        writeStream,
                        kernel,
                        kernelData,
                        byteLength,
                        byteLengthLoss,
                        progress,
                        cancellationToken);
                    }
                }

                using (MemoryBuffer1D<byte, Stride1D.Dense> dataBuffer =
                    this.accelerator.Allocate1D<byte>(byteLengthLoss))
                {
                    kernelData.Data = dataBuffer.View;

                    this.ProcessBufferWithPadding(dataBuffer,
                        readStream,
                        writeStream,
                        kernel,
                        kernelData,
                        byteLength,
                        byteLengthLoss,
                        progress,
                        cancellationToken);
                }
            }
        }

        /// <summary>
        /// Обработка буфера устройством.
        /// </summary>
        /// <param name="dataBuffer">Буфер устройства.</param>
        /// <param name="readStream">Поток чтения данных.</param>
        /// <param name="writeStream">Поток записи.</param>
        /// <param name="kernel">Метод ядра.</param>
        /// <param name="kernelData">Данные ядра.</param>
        /// <param name="byteLength">Длинна данных.</param>
        /// <param name="byteLengthLoss">Остаток данных.</param>
        /// <param name="progress">Прогресс.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        private void ProcessBuffer(MemoryBuffer1D<byte, Stride1D.Dense> dataBuffer,
            Stream readStream,
            Stream writeStream,
            Action<Index1D, KernelData> kernel,
            KernelData kernelData,
            long byteLength,
            long byteLengthLoss,
            IProgress<CryptoStatus> progress,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Index1D index = new Index1D((int)(dataBuffer.Length / CryptoUtils.BlockSize));

            this.CopyFromCPU(readStream, dataBuffer, Int16.MaxValue);

            kernel(index, kernelData);
            this.accelerator.Synchronize();

            this.CopyToCPU(writeStream, dataBuffer, Int16.MaxValue);

            CryptoStatus status = new CryptoStatus(byteLength - byteLengthLoss, byteLength, dataBuffer.Length);
            progress.Report(status);
        }

        /// <summary>
        /// Обработка буфера устройством с дополнением.
        /// </summary>
        /// <param name="dataBuffer">Буфер устройства.</param>
        /// <param name="readStream">Поток чтения данных.</param>
        /// <param name="writeStream">Поток записи.</param>
        /// <param name="kernel">Метод ядра.</param>
        /// <param name="kernelData">Данные ядра.</param>
        /// <param name="byteLength">Длинна данных.</param>
        /// <param name="byteLengthLoss">Остаток данных.</param>
        /// <param name="progress">Прогресс.</param>
        /// <param name="cancellationToken">Токен отмены.</param>
        private void ProcessBufferWithPadding(MemoryBuffer1D<byte, Stride1D.Dense> dataBuffer,
            Stream readStream,
            Stream writeStream,
            Action<Index1D, KernelData> kernel,
            KernelData kernelData,
            long byteLength,
            long byteLengthLoss,
            IProgress<CryptoStatus> progress,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Index1D index = new Index1D((int)(dataBuffer.Length / CryptoUtils.BlockSize));

            this.CopyFromCPU(readStream, dataBuffer, Int16.MaxValue);

            kernel(index, kernelData);
            this.accelerator.Synchronize();

            byte[] paddingCount = new byte[1];
            dataBuffer.View.SubView(dataBuffer.Length - 1, 1).CopyToCPU(paddingCount);
            int padding = paddingCount[0];

            if (padding > CryptoUtils.BlockSize)
            {
                throw new ArgumentException("Некорректный размер дополнения. Данные могут быть повреждены.");
            }

            this.CopyToCPU(writeStream, dataBuffer, Int16.MaxValue, padding);

            CryptoStatus status = new CryptoStatus(byteLength - byteLengthLoss, byteLength, dataBuffer.Length);
            progress.Report(status);
        }

        /// <summary>
        /// Копирование данных на устройство.
        /// </summary>
        /// <param name="readStream">Поток чтения данных.</param>
        /// <param name="gpuData">Выделенная память на устройстве.</param>
        /// <param name="bufferLength">Размер буфера.</param>
        private void CopyFromCPU(Stream readStream, MemoryBuffer1D<byte, Stride1D.Dense> gpuData, int bufferLength)
        {
            byte[] buffer = new byte[bufferLength];

            for (long offset = 0; offset < gpuData.Length;)
            {
                ArrayView<byte> dataPart = gpuData.View.SubView(offset, Math.Min(bufferLength, gpuData.Length - offset));

                int readBytes = readStream.Read(buffer, 0, dataPart.IntLength);
                ReadOnlySpan<byte> span = buffer.AsSpan(0, readBytes);

                dataPart.CopyFromCPU(span);
                offset += dataPart.Length;
            }
        }

        /// <summary>
        /// Копирование данных с устройства.
        /// </summary>
        /// <param name="writeStream">Поток записи.</param>
        /// <param name="gpuData">Выделенная память на устройстве.</param>
        /// <param name="bufferLength">Размер буфера.</param>
        /// <param name="paddingLength">Размер дополнения.</param>
        private void CopyToCPU(Stream writeStream, MemoryBuffer1D<byte, Stride1D.Dense> gpuData, int bufferLength, int paddingLength = 0)
        {
            byte[] buffer = new byte[bufferLength];
            long gpuLength = gpuData.Length - paddingLength;

            for (long offset = 0; offset < gpuLength;)
            {
                ArrayView<byte> dataPart = gpuData.View.SubView(offset, Math.Min(bufferLength, gpuLength - offset));
                Span<byte> span = buffer.AsSpan(0, dataPart.IntLength);

                dataPart.CopyToCPU(span);
                writeStream.Write(span);

                offset += dataPart.Length;
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
            initSize = Math.Min(initSize, (long)(this.device.MemorySize * 0.8));

            for (; initSize >= CryptoUtils.BlockSize; initSize = (long)(initSize * 0.8))
            {
                try
                {
                    initSize -= initSize % CryptoUtils.BlockSize;
                    return this.accelerator.Allocate1D<byte>(initSize);
                }
                catch (Exception) { }
            }
            
            throw new OutOfMemoryException("Недостаточно памяти даже для минимального буфера.");
        }
    }
}
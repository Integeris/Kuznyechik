using System;
using System.IO;
using System.Runtime.Intrinsics;

namespace Kuznyechik
{
    /// <summary>
    /// Шифровальщик в режиме ECB.
    /// </summary>
    /// <remarks>
    /// Режим ECB не рекомендуется для шифрования больших массивов данных из-за утечки паттернов.
    /// </remarks>
    public class ScramblerECB : Scrambler<byte>
    {
        /// <summary>
        /// Создание шифровальщика в режиме ECB.
        /// </summary>
        /// <param name="key">Ключ.</param>
        public ScramblerECB(ReadOnlySpan<byte> key) : base(key) { }

        /// <summary>
        /// Создание шифровальщика в режиме ECB.
        /// </summary>
        /// <param name="parameters">Параметры шифрования.</param>
        public ScramblerECB(CryptoParameters parameters) : base(parameters) { }

        /// <summary>
        /// Получение длины зашифрованных данных.
        /// </summary>
        /// <param name="dataLength">Длина данных.</param>
        /// <returns>Длина зашифрованных данных.</returns>
        protected override long GetEncryptLength(long dataLength)
        {
            return dataLength + CryptoUtils.BlockSize - dataLength % CryptoUtils.BlockSize;
        }

        /// <summary>
        /// Инициализация шифрования.
        /// </summary>
        /// <param name="source">Источник данных.</param>
        /// <param name="destination">Целевая область данных.</param>
        /// <returns>Контекст.</returns>
        protected override byte Initialize(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            return 0;
        }

        /// <summary>
        /// Инициализация шифрования.
        /// </summary>
        /// <param name="sourceStream">Поток источника.</param>
        /// <param name="destinationStream">Целевой поток.ы</param>
        /// <returns>Контекст.</returns>
        protected override byte Initialize(Stream sourceStream, Stream destinationStream)
        {
            return 0;
        }

        /// <summary>
        /// Предварительная обработка данных перед шифрованием.
        /// </summary>
        /// <param name="block">Блок данных.</param>
        /// <param name="context">Контекст.</param>
        /// <returns>Обработанный блок.</returns>
        protected override Vector128<byte> PreprocessEncrypt(Vector128<byte> block, ref byte context)
        {
            return block;
        }

        /// <summary>
        /// Предварительная обработка данных после расшифровывания.
        /// </summary>
        /// <param name="block">Блок данных.</param>
        /// <param name="context">Контекст.</param>
        /// <returns>Обработанный блок.</returns>
        protected override Vector128<byte> PostprocessDecrypt(Vector128<byte> block, ref byte context)
        {
            return block;
        }

        /// <summary>
        /// Обработка остатка данных, не вошедших в блок.
        /// </summary>
        /// <param name="lastBytes">Последние байты, не вошедшие в блок.</param>
        /// <param name="context">Контекст.</param>
        /// <returns>Последний блок.</returns>
        protected override Vector128<byte> ProcessLastBlock(ReadOnlySpan<byte> lastBytes, ref byte context)
        {
            byte lostByteLength = (byte)(CryptoUtils.BlockSize - lastBytes.Length);

            Span<byte> blockSpan = stackalloc byte[CryptoUtils.BlockSize];

            blockSpan.Fill(lostByteLength);
            lastBytes.CopyTo(blockSpan);

            return Vector128.Create(blockSpan);
        }

        /// <summary>
        /// Удаление дополнения.
        /// </summary>
        /// <param name="data">Данные.</param>
        /// <param name="context">Контекст.</param>
        protected override void RemovePadding(scoped ref Span<byte> data, scoped ref byte context)
        {
            byte paddingLength = data[^1];

            if (paddingLength == 0 || paddingLength > CryptoUtils.BlockSize)
            {
                throw new ArgumentException("Некорректный размер дополнения. Возможно, данные повреждены.", nameof(data));
            }

            Span<byte> padding = data[^paddingLength..];

            if (padding.IndexOfAnyExcept(paddingLength) != -1)
            {
                throw new ArgumentException("Ошибка структуры дополнения.", nameof(data));
            }

            data = data[..^paddingLength];
        }
    }
}

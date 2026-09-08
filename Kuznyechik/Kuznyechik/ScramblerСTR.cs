using System;
using System.IO;
using System.Runtime.Intrinsics;

namespace Kuznyechik
{
    /// <summary>
    /// Шифровальщик в режиме CTR.
    /// </summary>
    public class ScramblerСTR : Scrambler<long>
    {
        /// <summary>
        /// Создание шифровальщика в режиме CTR.
        /// </summary>
        /// <param name="key">Ключ.</param>
        public ScramblerСTR(ReadOnlySpan<byte> key) : base(key) { }

        /// <summary>
        /// Создание шифровальщика в режиме CTR.
        /// </summary>
        /// <param name="parameters">Параметры.</param>
        public ScramblerСTR(CryptoParameters parameters) : base(parameters) { }

        /// <summary>
        /// Получение длины зашифрованных данных.
        /// </summary>
        /// <param name="dataLength">Длина данных.</param>
        /// <returns>Длина зашифрованных данных.</returns>
        protected override long GetEncryptLength(long dataLength)
        {
            return dataLength;
        }

        /// <summary>
        /// Инициализация шифрования.
        /// </summary>
        /// <param name="source">Источник данных.</param>
        /// <param name="destination">Целевая область данных.</param>
        /// <returns>Контекст.</returns>
        protected override long Initialize(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Инициализация шифрования.
        /// </summary>
        /// <param name="sourceStream">Поток источника.</param>
        /// <param name="destinationStream">Целевой поток.</param>
        /// <returns>Контекст.</returns>
        protected override long Initialize(Stream sourceStream, Stream destinationStream)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Предварительная обработка данных перед шифрованием.
        /// </summary>
        /// <param name="block">Блок данных.</param>
        /// <param name="context">Контекст.</param>
        /// <returns>Обработанный блок.</returns>
        protected override Vector128<byte> PreprocessEncrypt(Vector128<byte> block, ref long context)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Обработка данных после расшифровывания.
        /// </summary>
        /// <param name="block">Блок данных.</param>
        /// <param name="context">Контекст.</param>
        /// <returns>Обработанный блок.</returns>
        protected override Vector128<byte> PostprocessDecrypt(Vector128<byte> block, ref long context)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Обработка остатка данных, не вошедших в блок.
        /// </summary>
        /// <param name="lastBytes">Последние байты, не вошедшие в блок.</param>
        /// <param name="context">Контекст.</param>
        /// <returns>Последний блок.</returns>
        protected override Vector128<byte> ProcessLastBlock(ReadOnlySpan<byte> lastBytes, ref long context)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Удаление дополнения.
        /// </summary>
        /// <param name="data">Данные.</param>
        /// <param name="context">Контекст.</param>
        protected override void RemovePadding(scoped ref Span<byte> data, scoped ref long context)
        {
            throw new NotImplementedException();
        }
    }
}

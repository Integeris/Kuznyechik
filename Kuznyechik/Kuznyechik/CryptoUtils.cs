using ILGPU;
using System;

namespace Kuznyechik
{
    /// <summary>
    /// Методы преобразования данных при шифровании.
    /// </summary>
    internal static class CryptoUtils
    {
        /// <summary>
        /// Размер блока.
        /// </summary>
        internal const byte BlockSize = 16;

        /// <summary>
        /// Размер ключа.
        /// </summary>
        internal const byte KeySize = 32;

        /// <summary>
        /// Шифрование блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="data">Данные.</param>
        internal static void EncryptBlock(ArrayView<byte> block, KernelData data)
        {
            ArrayView<byte> key;

            for (int i = 0; i < 9; i++)
            {
                key = data.Keys.SubView(i * BlockSize, BlockSize);

                ExclusiveOR(block, key);
                ReplaceBytes(block, data);
                MultiTransformEncrypt(block, data);
            }

            key = data.Keys.SubView(9 * BlockSize, BlockSize);
            ExclusiveOR(block, key);
        }

        /// <summary>
        /// Расшифрование блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="data">Данные.</param>
        internal static void DecryptBlock(ArrayView<byte> block, KernelData data)
        {
            ArrayView<byte> key = data.Keys.SubView(9 * BlockSize, BlockSize);
            ExclusiveOR(block, key);

            for (int i = 8; i >= 0; i--)
            {
                key = data.Keys.SubView(i * BlockSize, BlockSize);

                MultiTransformDecrypt(block, data);
                ReplaceBytes(block, data);
                ExclusiveOR(block, key);
            }
        }

        /// <summary>
        /// Исключающее ИЛИ для блоков.
        /// </summary>
        /// <param name="source">Источник.</param>
        /// <param name="key">Маска.</param>
        private static void ExclusiveOR(ArrayView<byte> source, ArrayView<byte> key)
        {
            for (int i = 0; i < BlockSize; i++)
            {
                source[i] ^= key[i];
            }
        }

        /// <summary>
        /// Замена байт блока на байты из указанной таблицы.
        /// </summary>
        /// <param name="block">Блок данных.</param>
        /// <param name="data">Данные.</param>
        private static void ReplaceBytes(ArrayView<byte> block, KernelData data)
        {
            for (int i = 0; i < BlockSize; i++)
            {
                block[i] = data.ReplaceBytes[block[i]];
            }
        }

        /// <summary>
        /// Трансформация блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="data">Данные.</param>
        private static void TransformBlock(ArrayView<byte> block, KernelData data)
        {
            byte sum = data.GaloisMultiplicationTable[block[0], data.LinearTransformation[0]];

            for (int i = 1; i < BlockSize; i++)
            {
                block[i - 1] = block[i];
                sum ^= data.GaloisMultiplicationTable[block[i], data.LinearTransformation[i]];
            }

            block[15] = sum;
        }

        /// <summary>
        /// Обратная трансформация блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="data">Данные.</param>
        private static void ReverseTransformBlock(ArrayView<byte> block, KernelData data)
        {
            byte sum = block[15];

            for (int i = BlockSize - 1; i > 0; i--)
            {
                block[i] = block[i - 1];
                sum ^= data.GaloisMultiplicationTable[block[i], data.LinearTransformation[i]];
            }

            block[0] = sum;
        }

        /// <summary>
        /// Шифрование блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="data">Данные.</param>
        private static void MultiTransformEncrypt(ArrayView<byte> block, KernelData data)
        {
            for (int i = 0; i < BlockSize; i++)
            {
                TransformBlock(block, data);
            }
        }

        /// <summary>
        /// Расшифрование блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="data">Данные.</param>
        private static void MultiTransformDecrypt(ArrayView<byte> block, KernelData data)
        {
            for (int i = 0; i < BlockSize; i++)
            {
                ReverseTransformBlock(block, data);
            }
        }
    }
}

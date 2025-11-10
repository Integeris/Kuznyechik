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
        /// Количество раундовых ключей.
        /// </summary>
        internal const byte RoundKeysLength = 10;

        /// <summary>
        /// Шифрование блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="parameters">Параметры.</param>
        internal static void EncryptBlock(ref Block block, in CryptoParameters parameters)
        {
            for (int i = 0; i < 9; i++)
            {
                ref Block key = ref parameters.Keys[i];

                block ^= key;
                ReplaceBytes(ref block, parameters.ReplaceBytes);
                MultiTransformEncrypt(ref block, parameters);
            }

            ref Block finalKey = ref parameters.Keys[9];
            block ^= finalKey;
        }

        /// <summary>
        /// Расшифрование блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="parameters">Параметры.</param>
        internal static void DecryptBlock(ref Block block, in CryptoParameters parameters)
        {
            ref Block firstKey = ref parameters.Keys[9];
            block ^= firstKey;

            for (int i = 8; i >= 0; i--)
            {
                Block key = parameters.Keys[i];

                MultiTransformDecrypt(ref block, parameters);
                ReplaceBytes(ref block, parameters.ReverseReplaceBytes);
                block ^= key;
            }
        }

        /// <summary>
        /// Замена байт блока на байты из указанной таблицы.
        /// </summary>
        /// <param name="block">Блок данных.</param>
        /// <param name="replaceBytes">таблица для нелинейного преобразования.</param>
        private static unsafe void ReplaceBytes(ref Block block, in ReadOnlySpan<byte> replaceBytes)
        {
            fixed (Block* ptr = &block)
            {
                byte* current = (byte*)ptr;
                byte* end = current + BlockSize;

                while (current < end)
                {
                    *current = replaceBytes[*current];
                    current++;
                }
            }
        }

        /// <summary>
        /// Трансформация блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="parameters">Параметры.</param>
        private static unsafe void TransformBlock(ref Block block, in CryptoParameters parameters)
        {
            ref readonly GaloisTable galoisTable = ref parameters.GaloisTable;
            ref readonly Block linearTransformation = ref parameters.LinearTransformation;

            fixed (Block* ptr = &block)
            {
                byte* blockPtr = (byte*)ptr;
                byte sum = galoisTable[blockPtr[0], linearTransformation[0]];

                for (int i = 1; i < BlockSize; i++)
                {
                    blockPtr[i - 1] = blockPtr[i];
                    sum ^= galoisTable[blockPtr[i], linearTransformation[i]];
                }

                blockPtr[15] = sum;
            }
        }

        /// <summary>
        /// Обратная трансформация блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="parameters">Параметры.</param>
        private static unsafe void ReverseTransformBlock(ref Block block, in CryptoParameters parameters)
        {
            ref readonly GaloisTable galoisTable = ref parameters.GaloisTable;
            ref readonly Block linearTransformation = ref parameters.LinearTransformation;

            fixed (Block* ptr = &block)
            {
                byte* bytes = (byte*)ptr;
                byte sum = bytes[15];

                // Обратный порядок: от 15 до 1
                for (int i = BlockSize - 1; i > 0; i--)
                {
                    bytes[i] = bytes[i - 1];
                    sum ^= galoisTable[bytes[i], linearTransformation[i]];
                }

                bytes[0] = sum;
            }
        }

        /// <summary>
        /// Шифрование блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="parameters">Параметры.</param>
        private static void MultiTransformEncrypt(ref Block block, in CryptoParameters parameters)
        {
            for (int i = 0; i < BlockSize; i++)
            {
                TransformBlock(ref block, parameters);
            }
        }

        /// <summary>
        /// Расшифрование блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="parameters">Параметры.</param>
        private static void MultiTransformDecrypt(ref Block block, in CryptoParameters parameters)
        {
            for (int i = 0; i < BlockSize; i++)
            {
                ReverseTransformBlock(ref block, parameters);
            }
        }
    }
}

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
            ref byte replaceBase = ref MemoryMarshal.GetReference(replaceBytes);

            fixed (Block* ptr = &block)
            fixed (byte* replacePtr = &replaceBase)
            {
                byte* current = (byte*)ptr;
                byte* end = current + BlockSize;

                while (current < end)
                {
                    *current = replacePtr[*current];
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

                sum ^= galoisTable[blockPtr[1], linearTransformation[1]];
                sum ^= galoisTable[blockPtr[2], linearTransformation[2]];
                sum ^= galoisTable[blockPtr[3], linearTransformation[3]];
                sum ^= galoisTable[blockPtr[4], linearTransformation[4]];
                sum ^= galoisTable[blockPtr[5], linearTransformation[5]];
                sum ^= galoisTable[blockPtr[6], linearTransformation[6]];
                sum ^= galoisTable[blockPtr[7], linearTransformation[7]];
                sum ^= galoisTable[blockPtr[8], linearTransformation[8]];
                sum ^= galoisTable[blockPtr[9], linearTransformation[9]];
                sum ^= galoisTable[blockPtr[10], linearTransformation[10]];
                sum ^= galoisTable[blockPtr[11], linearTransformation[11]];
                sum ^= galoisTable[blockPtr[12], linearTransformation[12]];
                sum ^= galoisTable[blockPtr[13], linearTransformation[13]];
                sum ^= galoisTable[blockPtr[14], linearTransformation[14]];
                sum ^= galoisTable[blockPtr[15], linearTransformation[15]];


                Unsafe.CopyBlock(blockPtr, blockPtr + 1, BlockSize - 1);
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
                byte* blockPtr = (byte*)ptr;

                byte sum = blockPtr[15];
                Unsafe.CopyBlock(blockPtr + 1, blockPtr, BlockSize - 1);

                sum ^= galoisTable[blockPtr[15], linearTransformation[15]];
                sum ^= galoisTable[blockPtr[14], linearTransformation[14]];
                sum ^= galoisTable[blockPtr[13], linearTransformation[13]];
                sum ^= galoisTable[blockPtr[12], linearTransformation[12]];
                sum ^= galoisTable[blockPtr[11], linearTransformation[11]];
                sum ^= galoisTable[blockPtr[10], linearTransformation[10]];
                sum ^= galoisTable[blockPtr[9], linearTransformation[9]];
                sum ^= galoisTable[blockPtr[8], linearTransformation[8]];
                sum ^= galoisTable[blockPtr[7], linearTransformation[7]];
                sum ^= galoisTable[blockPtr[6], linearTransformation[6]];
                sum ^= galoisTable[blockPtr[5], linearTransformation[5]];
                sum ^= galoisTable[blockPtr[4], linearTransformation[4]];
                sum ^= galoisTable[blockPtr[3], linearTransformation[3]];
                sum ^= galoisTable[blockPtr[2], linearTransformation[2]];
                sum ^= galoisTable[blockPtr[1], linearTransformation[1]];

                blockPtr[0] = sum;
            }
        }

        /// <summary>
        /// Шифрование блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="parameters">Параметры.</param>
        private static void MultiTransformEncrypt(ref Block block, in CryptoParameters parameters)
        {
            TransformBlock(ref block, parameters);
            TransformBlock(ref block, parameters);
            TransformBlock(ref block, parameters);
            TransformBlock(ref block, parameters);
            TransformBlock(ref block, parameters);
            TransformBlock(ref block, parameters);
            TransformBlock(ref block, parameters);
            TransformBlock(ref block, parameters);
            TransformBlock(ref block, parameters);
            TransformBlock(ref block, parameters);
            TransformBlock(ref block, parameters);
            TransformBlock(ref block, parameters);
            TransformBlock(ref block, parameters);
            TransformBlock(ref block, parameters);
            TransformBlock(ref block, parameters);
            TransformBlock(ref block, parameters);
        }

        /// <summary>
        /// Расшифрование блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="parameters">Параметры.</param>
        private static void MultiTransformDecrypt(ref Block block, in CryptoParameters parameters)
        {
            ReverseTransformBlock(ref block, parameters);
            ReverseTransformBlock(ref block, parameters);
            ReverseTransformBlock(ref block, parameters);
            ReverseTransformBlock(ref block, parameters);
            ReverseTransformBlock(ref block, parameters);
            ReverseTransformBlock(ref block, parameters);
            ReverseTransformBlock(ref block, parameters);
            ReverseTransformBlock(ref block, parameters);
            ReverseTransformBlock(ref block, parameters);
            ReverseTransformBlock(ref block, parameters);
            ReverseTransformBlock(ref block, parameters);
            ReverseTransformBlock(ref block, parameters);
            ReverseTransformBlock(ref block, parameters);
            ReverseTransformBlock(ref block, parameters);
            ReverseTransformBlock(ref block, parameters);
            ReverseTransformBlock(ref block, parameters);
        }
    }
}

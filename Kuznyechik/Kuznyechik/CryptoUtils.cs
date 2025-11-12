using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

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
            Block[] keys = parameters.Keys;

            EncryptBlockInline(ref block, ref keys[0], parameters);
            EncryptBlockInline(ref block, ref keys[1], parameters);
            EncryptBlockInline(ref block, ref keys[2], parameters);
            EncryptBlockInline(ref block, ref keys[3], parameters);
            EncryptBlockInline(ref block, ref keys[4], parameters);
            EncryptBlockInline(ref block, ref keys[5], parameters);
            EncryptBlockInline(ref block, ref keys[6], parameters);
            EncryptBlockInline(ref block, ref keys[7], parameters);
            EncryptBlockInline(ref block, ref keys[8], parameters);

            block ^= keys[9];
        }

        /// <summary>
        /// Расшифрование блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="parameters">Параметры.</param>
        internal static void DecryptBlock(ref Block block, in CryptoParameters parameters)
        {
            Block[] keys = parameters.Keys;

            block ^= keys[9];

            DecryptBlockInline(ref block, ref keys[8], parameters);
            DecryptBlockInline(ref block, ref keys[7], parameters);
            DecryptBlockInline(ref block, ref keys[6], parameters);
            DecryptBlockInline(ref block, ref keys[5], parameters);
            DecryptBlockInline(ref block, ref keys[4], parameters);
            DecryptBlockInline(ref block, ref keys[3], parameters);
            DecryptBlockInline(ref block, ref keys[2], parameters);
            DecryptBlockInline(ref block, ref keys[1], parameters);
            DecryptBlockInline(ref block, ref keys[0], parameters);
        }

        /// <summary>
        /// Замена байт блока на байты из указанной таблицы.
        /// </summary>
        /// <param name="block">Блок данных.</param>
        /// <param name="replaceBytes">таблица для нелинейного преобразования.</param>
        internal static unsafe void ReplaceBytes(ref Block block, in ReadOnlySpan<byte> replaceBytes)
        {
            block[0] = replaceBytes[block[0]];
            block[1] = replaceBytes[block[1]];
            block[2] = replaceBytes[block[2]];
            block[3] = replaceBytes[block[3]];
            block[4] = replaceBytes[block[4]];
            block[5] = replaceBytes[block[5]];
            block[6] = replaceBytes[block[6]];
            block[7] = replaceBytes[block[7]];
            block[8] = replaceBytes[block[8]];
            block[9] = replaceBytes[block[9]];
            block[10] = replaceBytes[block[10]];
            block[11] = replaceBytes[block[11]];
            block[12] = replaceBytes[block[12]];
            block[13] = replaceBytes[block[13]];
            block[14] = replaceBytes[block[14]];
            block[15] = replaceBytes[block[15]];
        }

        /// <summary>
        /// Шифрование блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="parameters">Параметры.</param>
        internal static void MultiTransformEncrypt(ref Block block, in CryptoParameters parameters)
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
        /// Вспомогательный метод для шифрования.
        /// </summary>
        /// <param name="block">Шифруемый блок.</param>
        /// <param name="key">Ключ.</param>
        /// <param name="parameters">Параметры.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void EncryptBlockInline(
            ref Block block, 
            ref Block key,
            in CryptoParameters parameters)
        {
            block ^= key;
            ReplaceBytes(ref block, parameters.ReplaceBytes);
            MultiTransformEncrypt(ref block, parameters);
        }

        /// <summary>
        /// Вспомогательный метод для расшифровывания.
        /// </summary>
        /// <param name="block">Расшифруемый блок.</param>
        /// <param name="key">Ключ.</param>
        /// <param name="parameters">Параметры.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void DecryptBlockInline(
            ref Block block,
            ref Block key,
            in CryptoParameters parameters)
        {
            MultiTransformDecrypt(ref block, parameters);
            ReplaceBytes(ref block, parameters.ReverseReplaceBytes);
            block ^= key;
        }

        /// <summary>
        /// Трансформация блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="parameters">Параметры.</param>
        private static unsafe void TransformBlock(ref Block block, in CryptoParameters parameters)
        {
            ref readonly GaloisTable galoisTable = ref parameters.GaloisTable;

            fixed (Block* ptr = &block)
            fixed (Block* linearTransformationBlock = &parameters.LinearTransformation)
            {
                byte* blockPtr = (byte*)ptr;
                byte* linearTransformationBlockPtr = (byte*)linearTransformationBlock;

                byte sum = galoisTable[blockPtr[0], linearTransformationBlockPtr[0]];

                sum ^= galoisTable[blockPtr[1], linearTransformationBlockPtr[1]];
                sum ^= galoisTable[blockPtr[2], linearTransformationBlockPtr[2]];
                sum ^= galoisTable[blockPtr[3], linearTransformationBlockPtr[3]];
                sum ^= galoisTable[blockPtr[4], linearTransformationBlockPtr[4]];
                sum ^= galoisTable[blockPtr[5], linearTransformationBlockPtr[5]];
                sum ^= galoisTable[blockPtr[6], linearTransformationBlockPtr[6]];
                sum ^= galoisTable[blockPtr[7], linearTransformationBlockPtr[7]];
                sum ^= galoisTable[blockPtr[8], linearTransformationBlockPtr[8]];
                sum ^= galoisTable[blockPtr[9], linearTransformationBlockPtr[9]];
                sum ^= galoisTable[blockPtr[10], linearTransformationBlockPtr[10]];
                sum ^= galoisTable[blockPtr[11], linearTransformationBlockPtr[11]];
                sum ^= galoisTable[blockPtr[12], linearTransformationBlockPtr[12]];
                sum ^= galoisTable[blockPtr[13], linearTransformationBlockPtr[13]];
                sum ^= galoisTable[blockPtr[14], linearTransformationBlockPtr[14]];
                sum ^= galoisTable[blockPtr[15], linearTransformationBlockPtr[15]];


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
            fixed (Block* linearTransformationBlock = &parameters.LinearTransformation)
            {
                byte* blockPtr = (byte*)ptr;
                byte* linearTransformationBlockPtr = (byte*)linearTransformationBlock;

                byte sum = blockPtr[15];
                Unsafe.CopyBlock(blockPtr + 1, blockPtr, BlockSize - 1);

                sum ^= galoisTable[blockPtr[15], linearTransformationBlockPtr[15]];
                sum ^= galoisTable[blockPtr[14], linearTransformationBlockPtr[14]];
                sum ^= galoisTable[blockPtr[13], linearTransformationBlockPtr[13]];
                sum ^= galoisTable[blockPtr[12], linearTransformationBlockPtr[12]];
                sum ^= galoisTable[blockPtr[11], linearTransformationBlockPtr[11]];
                sum ^= galoisTable[blockPtr[10], linearTransformationBlockPtr[10]];
                sum ^= galoisTable[blockPtr[9], linearTransformationBlockPtr[9]];
                sum ^= galoisTable[blockPtr[8], linearTransformationBlockPtr[8]];
                sum ^= galoisTable[blockPtr[7], linearTransformationBlockPtr[7]];
                sum ^= galoisTable[blockPtr[6], linearTransformationBlockPtr[6]];
                sum ^= galoisTable[blockPtr[5], linearTransformationBlockPtr[5]];
                sum ^= galoisTable[blockPtr[4], linearTransformationBlockPtr[4]];
                sum ^= galoisTable[blockPtr[3], linearTransformationBlockPtr[3]];
                sum ^= galoisTable[blockPtr[2], linearTransformationBlockPtr[2]];
                sum ^= galoisTable[blockPtr[1], linearTransformationBlockPtr[1]];

                blockPtr[0] = sum;
            }
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

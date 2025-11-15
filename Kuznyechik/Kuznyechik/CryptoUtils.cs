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

            for (int i = 0; i <= 8; i++)
            {
                ref Block key = ref keys[i];
                block ^= key;
                ReplaceBytes(ref block, parameters.ReplaceBytes);
                MultiTransformEncrypt(ref block, parameters);
            }

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

            for (int i = 8; i >= 0; i--)
            {
                ref Block key = ref keys[i];

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
        internal static unsafe void ReplaceBytes(ref Block block, in ReadOnlySpan<byte> replaceBytes)
        {
            ref byte blockPtr = ref Unsafe.As<Block, byte>(ref block);
            ref byte replaceBytesPtr = ref MemoryMarshal.GetReference(replaceBytes);

            for (int i = 0; i < BlockSize; i++)
            {
                ref byte currentBlock = ref Unsafe.Add<byte>(ref blockPtr, i);
                currentBlock = Unsafe.Add(ref replaceBytesPtr, currentBlock);
            }
        }

        /// <summary>
        /// Шифрование блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="parameters">Параметры.</param>
        internal static void MultiTransformEncrypt(ref Block block, in CryptoParameters parameters)
        {
            for (int i = 0; i < BlockSize; i++)
            {
                TransformBlock(ref block, parameters);
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
            for (int i = 0; i < BlockSize; i++)
            {
                ReverseTransformBlock(ref block, parameters);
            }
        }
    }
}

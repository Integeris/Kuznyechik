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
        /// Делегат для вызова методов шифрования и расшифровывания.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="parameters">Параметры.</param>
        internal delegate void CryptBlockDelegate(ref Block block, in CryptoParameters parameters);

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
                block.Xor(key);
                ReplaceBytes(ref block, parameters.ReplaceBytes);
                LinearTransformEncrypt(ref block, parameters);
            }

            block.Xor(keys[9]);
        }

        /// <summary>
        /// Расшифрование блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="parameters">Параметры.</param>
        internal static void DecryptBlock(ref Block block, in CryptoParameters parameters)
        {
            Block[] keys = parameters.Keys;

            block.Xor(keys[9]);

            for (int i = 8; i >= 0; i--)
            {
                ref Block key = ref keys[i];

                LinearTransformDecrypt(ref block, parameters);
                ReplaceBytes(ref block, parameters.ReverseReplaceBytes);
                block.Xor(key);
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
                ref byte currentByte = ref Unsafe.Add(ref blockPtr, i);
                currentByte = Unsafe.Add(ref replaceBytesPtr, currentByte);
            }
        }

        /// <summary>
        /// Шифрование блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="parameters">Параметры.</param>
        internal static void LinearTransformEncrypt(ref Block block, in CryptoParameters parameters)
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
            byte copyLength = BlockSize - 1;

            fixed (byte* blockPtr = &Unsafe.As<Block, byte>(ref block))
            {
                byte sum = galoisTable[blockPtr[0], 0];

                sum ^= galoisTable[blockPtr[1], 1];
                sum ^= galoisTable[blockPtr[2], 2];
                sum ^= galoisTable[blockPtr[3], 3];
                sum ^= galoisTable[blockPtr[4], 4];
                sum ^= galoisTable[blockPtr[5], 5];
                sum ^= galoisTable[blockPtr[6], 6];
                sum ^= galoisTable[blockPtr[7], 7];
                sum ^= galoisTable[blockPtr[8], 8];
                sum ^= galoisTable[blockPtr[9], 9];
                sum ^= galoisTable[blockPtr[10], 10];
                sum ^= galoisTable[blockPtr[11], 11];
                sum ^= galoisTable[blockPtr[12], 12];
                sum ^= galoisTable[blockPtr[13], 13];
                sum ^= galoisTable[blockPtr[14], 14];
                sum ^= galoisTable[blockPtr[15], 15];

                Buffer.MemoryCopy(blockPtr + 1, blockPtr, copyLength, copyLength);
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
            byte copyLength = BlockSize - 1;

            fixed (byte* blockPtr = &Unsafe.As<Block, byte>(ref block))
            {
                byte sum = blockPtr[15];
                Buffer.MemoryCopy(blockPtr, blockPtr + 1, copyLength, copyLength);

                sum ^= galoisTable[blockPtr[15], 15];
                sum ^= galoisTable[blockPtr[14], 14];
                sum ^= galoisTable[blockPtr[13], 13];
                sum ^= galoisTable[blockPtr[12], 12];
                sum ^= galoisTable[blockPtr[11], 11];
                sum ^= galoisTable[blockPtr[10], 10];
                sum ^= galoisTable[blockPtr[9], 9];
                sum ^= galoisTable[blockPtr[8], 8];
                sum ^= galoisTable[blockPtr[7], 7];
                sum ^= galoisTable[blockPtr[6], 6];
                sum ^= galoisTable[blockPtr[5], 5];
                sum ^= galoisTable[blockPtr[4], 4];
                sum ^= galoisTable[blockPtr[3], 3];
                sum ^= galoisTable[blockPtr[2], 2];
                sum ^= galoisTable[blockPtr[1], 1];

                blockPtr[0] = sum;
            }
        }

        /// <summary>
        /// Расшифрование блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="parameters">Параметры.</param>
        private static void LinearTransformDecrypt(ref Block block, in CryptoParameters parameters)
        {
            for (int i = 0; i < BlockSize; i++)
            {
                ReverseTransformBlock(ref block, parameters);
            }
        }
    }
}

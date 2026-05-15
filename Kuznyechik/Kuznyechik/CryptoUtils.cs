using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;

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
        /// Делегат для вызова методов зашифровывания и расшифровывания.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="parameters">Параметры.</param>
        internal delegate void CryptBlockDelegate(ref Vector128<byte> block, in CryptoParameters parameters);

        /// <summary>
        /// Зашифровывание блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="parameters">Параметры.</param>
        internal static void EncryptBlock(ref Vector128<byte> block, in CryptoParameters parameters)
        {
            Vector128<byte>[] keys = parameters.Keys;

            for (int i = 0; i <= 8; i++)
            {
                scoped ref Vector128<byte> key = ref keys[i];
                block ^= key;
                ReplaceBytes(ref block, parameters.ReplaceBytes);
                LinearTransformEncrypt(ref block, parameters);
            }

            block ^= keys[9];
        }

        /// <summary>
        /// Расшифрование блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="parameters">Параметры.</param>
        internal static void DecryptBlock(ref Vector128<byte> block, in CryptoParameters parameters)
        {
            Vector128<byte>[] keys = parameters.Keys;

            block ^= keys[9];

            for (int i = 8; i >= 0; i--)
            {
                scoped ref Vector128<byte> key = ref keys[i];

                LinearTransformDecrypt(ref block, parameters);
                ReplaceBytes(ref block, parameters.ReverseReplaceBytes);
                block ^= key;
            }
        }

        /// <summary>
        /// Замена байт блока на байты из указанной таблицы.
        /// </summary>
        /// <param name="block">Блок данных.</param>
        /// <param name="replaceBytes">таблица для нелинейного преобразования.</param>
        internal static void ReplaceBytes(ref Vector128<byte> block, in ReadOnlySpan<byte> replaceBytes)
        {
            scoped ref byte blockPtr = ref Unsafe.As<Vector128<byte>, byte>(ref block);
            scoped ref byte replaceBytesPtr = ref MemoryMarshal.GetReference(replaceBytes);

            for (int i = 0; i < BlockSize; i++)
            {
                scoped ref byte currentByte = ref Unsafe.Add(ref blockPtr, i);
                currentByte = Unsafe.Add(ref replaceBytesPtr, currentByte);
            }
        }

        /// <summary>
        /// Линейное преобразование блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="parameters">Параметры.</param>
        internal static unsafe void LinearTransformEncrypt(ref Vector128<byte> block, in CryptoParameters parameters)
        {
            scoped ref readonly GaloisTable galoisTable = ref parameters.GaloisTable;

            fixed (byte* blockPtr = &Unsafe.As<Vector128<byte>, byte>(ref block))
            {
                for (int i = 0; i < BlockSize; i++)
                {
                    // Вычисляем сумму с использованием таблицы Галуа
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

                    // Сдвигаем байты и записываем новое значение в конец
                    Buffer.MemoryCopy(blockPtr + 1, blockPtr, 15, 15);
                    blockPtr[15] = sum;
                }
            }
        }

        /// <summary>
        /// Расшифрование блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="parameters">Параметры.</param>
        internal static unsafe void LinearTransformDecrypt(ref Vector128<byte> block, in CryptoParameters parameters)
        {
            scoped ref readonly GaloisTable galoisTable = ref parameters.GaloisTable;

            fixed (byte* blockPtr = &Unsafe.As<Vector128<byte>, byte>(ref block))
            {
                for (int i = 0; i < BlockSize; i++)
                {
                    // Сохраняем последний байт для последующего XOR
                    byte sum = blockPtr[15];

                    // Сдвигаем байты вправо (копируем с начала в конец со смещением)
                    Buffer.MemoryCopy(blockPtr, blockPtr + 1, 15, 15);

                    // Применяем обратные преобразования с использованием таблицы Галуа
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

                    // Записываем результат в первый байт
                    blockPtr[0] = sum;
                }
            }
        }
    }
}

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
        internal const int BlockSize = 16;

        /// <summary>
        /// Размер ключа.
        /// </summary>
        internal const int KeySize = 32;

        /// <summary>
        /// Шифрование блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="keys">Ключи.</param>
        /// <param name="linearTransformation">Байты линейной трансформации.</param>
        /// <param name="replaceBytes">Таблица для нелинейного преобразования.</param>
        internal static void EncryptBlock(ArrayView<byte> block, ArrayView<byte> keys, ArrayView<byte> linearTransformation, ArrayView<byte> replaceBytes)
        {
            ArrayView<byte> key;

            for (int i = 0; i < 9; i++)
            {
                key = keys.SubView(i * BlockSize, BlockSize);

                ExclusiveOR(block, key);
                ReplaceBytes(block, replaceBytes);
                MultiTransformEncrypt(block, linearTransformation);
            }

            key = keys.SubView(9 * BlockSize, BlockSize);
            ExclusiveOR(block, key);
        }

        /// <summary>
        /// Расшифрование блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="keys">Ключи.</param>
        /// <param name="linearTransformation">Байты линейной трансформации.</param>
        /// <param name="reversReplaceBytes">Таблица для обратного нелинейного преобразования.</param>
        internal static void DecryptBlock(ArrayView<byte> block, ArrayView<byte> keys, ArrayView<byte> linearTransformation, ArrayView<byte> reversReplaceBytes)
        {
            ArrayView<byte> key = keys.SubView(9 * BlockSize, BlockSize);
            ExclusiveOR(block, key);

            for (int i = 8; i >= 0; i--)
            {
                key = keys.SubView(i * BlockSize, BlockSize);

                MultiTransformDecrypt(block, linearTransformation);
                ReplaceBytes(block, reversReplaceBytes);
                ExclusiveOR(block, key);
            }
        }

        /// <summary>
        /// Генерация раундовых ключей.
        /// </summary>
        /// <param name="keys">Раундовые ключи.</param>
        /// <param name="constants">Константы для расчётов.</param>
        /// <param name="linearTransformation">Байты линейной трансформации.</param>
        /// <param name="replaceBytes">Таблица для нелинейного преобразования.</param>
        internal static void GenerationRoundKeys(byte[][] keys, byte[][] constants, byte[] linearTransformation, byte[] replaceBytes)
        {
            for (int i = 0; i < 4; i++)
            {
                int firstPart = i * 2 + 2;
                int secondPart = i * 2 + 3;

                Array.Copy(keys[firstPart - 2], keys[firstPart], BlockSize);
                Array.Copy(keys[secondPart - 2], keys[secondPart], BlockSize);

                for (int j = 0; j < 8; j++)
                {
                    FeistelCell(keys[firstPart], keys[secondPart], constants[j + 8 * i], linearTransformation, replaceBytes);
                }
            }
        }

        /// <summary>
        /// Исключающее ИЛИ для блоков.
        /// </summary>
        /// <param name="source">Источник.</param>
        /// <param name="key">Маска.</param>
        private static void ExclusiveOR(byte[] source, byte[] key)
        {
            for (int i = 0; i < BlockSize; i++)
            {
                source[i] ^= key[i];
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
        /// <param name="replaceBytes">Таблица замены.</param>
        private static void ReplaceBytes(byte[] block, byte[] replaceBytes)
        {
            for (int i = 0; i < BlockSize; i++)
            {
                block[i] = replaceBytes[block[i]];
            }
        }

        /// <summary>
        /// Замена байт блока на байты из указанной таблицы.
        /// </summary>
        /// <param name="block">Блок данных.</param>
        /// <param name="replaceBytes">Таблица замены.</param>
        private static void ReplaceBytes(ArrayView<byte> block, ArrayView<byte> replaceBytes)
        {
            for (int i = 0; i < BlockSize; i++)
            {
                block[i] = replaceBytes[(int)block[i]];
            }
        }

        /// <summary>
        /// Умножение чисел в поле Галуа.
        /// </summary>
        /// <param name="origin">Исходный байт.</param>
        /// <param name="key">Байт ключа.</param>
        /// <returns>Результат умножения по Галуа.</returns>
        private static byte GaloisMultiplication(byte origin, byte key)
        {
            byte result = 0;

            // цикл для каждого бита (в байте 8 битов)
            for (int i = 0; i < 8; i++)
            {
                // Если младший бит ключа равен 1.
                if ((key & 0b01) == 1)
                {
                    result ^= origin;
                }

                key >>= 1;

                // Вычисляем старший бит исходного байта.
                byte higherBit = (byte)(origin & 0b10000000);
                origin <<= 1;

                if (higherBit != 0)
                {
                    // Неприводимый полином для поля Галуа: x^8 + x^7 + x^6 + x + 1
                    origin ^= 195;
                }
            }

            return result;
        }

        /// <summary>
        /// Трансформация блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="linearTransformation">Байты линейной трансформации.</param>
        private static void TransformBlock(byte[] block, byte[] linearTransformation)
        {
            byte sum = GaloisMultiplication(block[0], linearTransformation[0]);

            for (int i = 1; i < BlockSize; i++)
            {
                block[i - 1] = block[i];
                sum ^= GaloisMultiplication(block[i], linearTransformation[i]);
            }

            block[15] = sum;
        }

        /// <summary>
        /// Трансформация блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="linearTransformation">Байты линейной трансформации.</param>
        private static void TransformBlock(ArrayView<byte> block, ArrayView<byte> linearTransformation)
        {
            byte sum = GaloisMultiplication(block[0], linearTransformation[0]);

            for (int i = 1; i < BlockSize; i++)
            {
                block[i - 1] = block[i];
                sum ^= GaloisMultiplication(block[i], linearTransformation[i]);
            }

            block[15] = sum;
        }

        /// <summary>
        /// Обратная трансформация блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="linearTransformation">Байты линейной трансформации.</param>
        private static void ReverseTransformBlock(ArrayView<byte> block, ArrayView<byte> linearTransformation)
        {
            byte sum = block[15];

            for (int i = BlockSize - 1; i > 0; i--)
            {
                block[i] = block[i - 1];
                sum ^= GaloisMultiplication(block[i], linearTransformation[i]);
            }

            block[0] = sum;
        }

        /// <summary>
        /// Шифрование блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="linearTransformation">Байты линейной трансформации.</param>
        /// <param name="transformBlock">Метод преобразования блока.</param>
        private static void MultiTransform(byte[] block, byte[] linearTransformation, Action<byte[], byte[]> transformBlock)
        {
            for (int i = 0; i < BlockSize; i++)
            {
                transformBlock(block, linearTransformation);
            }
        }

        /// <summary>
        /// Шифрование блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="linearTransformation">Байты линейной трансформации.</param>
        private static void MultiTransformEncrypt(ArrayView<byte> block, ArrayView<byte> linearTransformation)
        {
            for (int i = 0; i < BlockSize; i++)
            {
                TransformBlock(block, linearTransformation);
            }
        }

        /// <summary>
        /// Расшифрование блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="linearTransformation">Байты линейной трансформации.</param>
        private static void MultiTransformDecrypt(ArrayView<byte> block, ArrayView<byte> linearTransformation)
        {
            for (int i = 0; i < BlockSize; i++)
            {
                ReverseTransformBlock(block, linearTransformation);
            }
        }

        /// <summary>
        /// Ячейка Фейстеля.
        /// </summary>
        /// <param name="firstKey">Первый ключ.</param>
        /// <param name="secondKey">Второй ключ.</param>
        /// <param name="constants">Константы.</param>
        /// <param name="linearTransformation">Байты линейной трансформации.</param>
        /// <param name="replaceBytes">Таблица для нелинейного преобразования.</param>
        private static void FeistelCell(byte[] firstKey, byte[] secondKey, byte[] constants, byte[] linearTransformation, byte[] replaceBytes)
        {
            byte[] tmpKey = new byte[firstKey.Length];
            Array.Copy(firstKey, tmpKey, firstKey.Length);

            ExclusiveOR(tmpKey, constants);
            ReplaceBytes(tmpKey, replaceBytes);
            MultiTransform(tmpKey, linearTransformation, TransformBlock);
            ExclusiveOR(tmpKey, secondKey);

            Array.Copy(firstKey, secondKey, firstKey.Length);
            Array.Copy(tmpKey, firstKey, tmpKey.Length);
        }
    }
}

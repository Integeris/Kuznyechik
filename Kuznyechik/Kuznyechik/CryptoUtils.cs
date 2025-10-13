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
        /// <param name="data">Данные.</param>
        internal static void EncryptBlock(ref Block block, ref KernelData data)
        {
            for (int i = 0; i < 9; i++)
            {
                ref Block key = ref data.Keys[i];

                block ^= key;
                ReplaceBytes(ref block, ref data);
                MultiTransformEncrypt(ref block, ref data);
            }

            ref Block finalKey = ref data.Keys[9];
            block ^= finalKey;
        }

        /// <summary>
        /// Расшифрование блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="data">Данные.</param>
        internal static void DecryptBlock(ref Block block, ref KernelData data)
        {
            ref Block firstKey = ref data.Keys[9];
            block ^= firstKey;

            for (int i = 8; i >= 0; i--)
            {
                Block key = data.Keys[i];

                MultiTransformDecrypt(ref block, ref data);
                ReplaceBytes(ref block, ref data);
                block ^= key;
            }
        }

        /// <summary>
        /// Замена байт блока на байты из указанной таблицы.
        /// </summary>
        /// <param name="block">Блок данных.</param>
        /// <param name="data">Данные.</param>
        private static unsafe void ReplaceBytes(ref Block block, ref KernelData data)
        {
            fixed (Block* ptr = &block)
            {
                byte* current = (byte*)ptr;
                byte* end = current + BlockSize;

                while (current < end)
                {
                    *current = data.ReplaceBytes[*current];
                    current++;
                }
            }
        }

        /// <summary>
        /// Трансформация блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="data">Данные.</param>
        private static unsafe void TransformBlock(ref Block block, ref KernelData data)
        {
            ref GaloisTable galoisTable = ref data.GaloisTable.Value;
            ref Block linearTransformation = ref data.LinearTransformation.Value;

            fixed (Block* ptr = &block)
            {
                byte* current = (byte*)ptr;
                byte* end = current + BlockSize - 1;

                byte sum = galoisTable[*current, linearTransformation[0]];
                current++;

                byte index = 1;

                while (current < end)
                {
                    current[-1] = *current;
                    sum ^= galoisTable[*current, linearTransformation[index]];

                    current++;
                    index++;
                }

                current[-1] = *current;
                sum ^= galoisTable[*current, linearTransformation[index]];

                *current = sum;
            }
        }

        /// <summary>
        /// Обратная трансформация блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="data">Данные.</param>
        private static unsafe void ReverseTransformBlock(ref Block block, ref KernelData data)
        {
            ref GaloisTable galoisTable = ref data.GaloisTable.Value;
            ref Block linearTransformation = ref data.LinearTransformation.Value;

            fixed (Block* ptr = &block)
            {
                byte* current = (byte*)(ptr + BlockSize - 1);
                byte sum = *current;

                for (int i = BlockSize - 1; i > 0; i--)
                {
                    *current = current[-1];
                    sum ^= galoisTable[*current, linearTransformation[i]];

                    current--;
                }

                *current = sum;
            }
        }

        /// <summary>
        /// Шифрование блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="data">Данные.</param>
        private static void MultiTransformEncrypt(ref Block block, ref KernelData data)
        {
            for (int i = 0; i < BlockSize; i++)
            {
                TransformBlock(ref block, ref data);
            }
        }

        /// <summary>
        /// Расшифрование блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="data">Данные.</param>
        private static void MultiTransformDecrypt(ref Block block, ref KernelData data)
        {
            for (int i = 0; i < BlockSize; i++)
            {
                ReverseTransformBlock(ref block, ref data);
            }
        }
    }
}

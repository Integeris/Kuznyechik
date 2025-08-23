using ILGPU;

namespace Kuznyechik
{
    /// <summary>
    /// Класс методов для видеокарты.
    /// </summary>
    internal static class Kernel
    {
        /// <summary>
        /// Шифрование данных.
        /// </summary>
        /// <param name="index">Индекс.</param>
        /// <param name="arr">Массив байт для шифрования.</param>
        /// <param name="keys">Ключи.</param>
        /// <param name="linearTransformation">Байты линейной трансформации.</param>
        /// <param name="replaceBytes">Таблица для линейного преобразования.</param>
        internal static void Encrypt(Index1D index, 
            ArrayView<byte> arr, 
            ArrayView<byte> keys, 
            ArrayView<byte> linearTransformation, 
            ArrayView<byte> replaceBytes)
        {
            ArrayView<byte> block = GetCurrentBlock(index, arr);
            CryptoUtils.EncryptBlock(block, keys, linearTransformation, replaceBytes);
        }

        /// <summary>
        /// Расшифрование данных.
        /// </summary>
        /// <param name="index">Индекс.</param>
        /// <param name="arr">Массив байт для расшифровки.</param>
        /// <param name="keys">Ключи.</param>
        /// <param name="linearTransformation">Байты линейной трансформации.</param>
        /// <param name="replaceBytes">Таблица для линейного преобразования.</param>
        internal static void Decrypt(Index1D index, 
            ArrayView<byte> arr, 
            ArrayView<byte> keys, 
            ArrayView<byte> linearTransformation, 
            ArrayView<byte> replaceBytes)
        {
            ArrayView<byte> block = GetCurrentBlock(index, arr);
            CryptoUtils.DecryptBlock(block, keys, linearTransformation, replaceBytes);
        }

        /// <summary>
        /// Получение текущего блока.
        /// </summary>
        /// <param name="index">Индекс блока.</param>
        /// <param name="arr">Общий массив.</param>
        /// <returns>Текущий блок.</returns>
        private static ArrayView<byte> GetCurrentBlock(Index1D index, ArrayView<byte> arr)
        {
            int startIndex = index * CryptoUtils.BlockSize;
            return arr.SubView(startIndex, CryptoUtils.BlockSize);
        }
    }
}

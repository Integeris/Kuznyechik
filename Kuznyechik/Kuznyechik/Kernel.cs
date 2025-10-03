using ILGPU;

namespace Kuznyechik
{
    /// <summary>
    /// Класс методов ядра.
    /// </summary>
    internal static class Kernel
    {
        /// <summary>
        /// Шифрование данных.
        /// </summary>
        /// <param name="index">Индекс.</param>
        /// <param name="data">Данные.</param>
        internal static void Encrypt(Index1D index,
            KernelData data)
        {
            ArrayView<byte> block = GetCurrentBlock(index, data.Data);
            CryptoUtils.EncryptBlock(block, data);
        }

        /// <summary>
        /// Расшифрование данных.
        /// </summary>
        /// <param name="index">Индекс.</param>
        /// <param name="data">Данные.</param>
        internal static void Decrypt(Index1D index,
            KernelData data)
        {
            ArrayView<byte> block = GetCurrentBlock(index, data.Data);
            CryptoUtils.DecryptBlock(block, data);
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

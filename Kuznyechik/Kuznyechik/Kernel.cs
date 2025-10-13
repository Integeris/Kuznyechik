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
            Block block = data.Data[index];
            CryptoUtils.EncryptBlock(ref block, ref data);
        }

        /// <summary>
        /// Расшифрование данных.
        /// </summary>
        /// <param name="index">Индекс.</param>
        /// <param name="data">Данные.</param>
        internal static void Decrypt(Index1D index,
            KernelData data)
        {
            Block block = data.Data[index];
            CryptoUtils.DecryptBlock(ref block, ref data);
        }
    }
}

namespace Kuznyechik
{
    /// <summary>
    /// Статус выполнения шифрования или расшифрования.
    /// </summary>
    public struct CryptoStatus
    {
        /// <summary>
        /// Позиция данных.
        /// </summary>
        public long DataPosition { get; set; }

        /// <summary>
        /// Длина данных.
        /// </summary>
        public long DataLength { get; set; }

        /// <summary>
        /// Размер буфера.
        /// </summary>
        public long BufferLength { get; set; }

        /// <summary>
        /// Создание статуса выполнения шифрования или расшифрования.
        /// </summary>
        /// <param name="dataPosition">Позиция данных.</param>
        /// <param name="dataLength">Длина данных.</param>
        /// <param name="bufferLength">Размер буфера.</param>
        public CryptoStatus(long dataPosition, long dataLength, long bufferLength)
        {
            DataPosition = dataPosition;
            DataLength = dataLength;
            BufferLength = bufferLength;
        }
    }
}

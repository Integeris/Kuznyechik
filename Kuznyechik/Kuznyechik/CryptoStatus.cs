namespace Kuznyechik
{
    /// <summary>
    /// Статус выполнения зашифровывания или расшифрования.
    /// </summary>
    public struct CryptoStatus
    {
        /// <summary>
        /// Позиция данных в байтах.
        /// </summary>
        public long DataPosition { get; set; }

        /// <summary>
        /// Длина данных в байтах.
        /// </summary>
        public long DataLength { get; set; }

        /// <summary>
        /// Размер буфера в байтах.
        /// </summary>
        public long BufferLength { get; set; }

        /// <summary>
        /// Создание статуса выполнения зашифровывания или расшифрования.
        /// </summary>
        /// <param name="dataPosition">Позиция данных в байтах.</param>
        /// <param name="dataLength">Длина данных в байтах.</param>
        /// <param name="bufferLength">Размер буфера в байтах.</param>
        public CryptoStatus(long dataPosition, long dataLength, long bufferLength)
        {
            this.DataPosition = dataPosition;
            this.DataLength = dataLength;
            this.BufferLength = bufferLength;
        }
    }
}

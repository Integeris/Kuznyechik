using System;
using System.Runtime.InteropServices;

namespace Kuznyechik
{
    /// <summary>
    /// Блок данных (16 байт).
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 16)]
    internal struct Block : IEquatable<Block>
    {
        /// <summary>
        /// Первая половина блока.
        /// </summary>
        private ulong low;

        /// <summary>
        /// Вторая половина блока.
        /// </summary>
        private ulong high;

        /// <summary>
        /// Сравнение двух блоков.
        /// </summary>
        /// <param name="other">Сравниваемый блок.</param>
        /// <returns>Одинаковый ли объект.</returns>
        public readonly bool Equals(Block other)
        {
            return other.low == this.low && other.high == this.high;
        }

        /// <summary>
        /// Сравнение объекта с блоком.
        /// </summary>
        /// <param name="obj">Объект.</param>
        /// <returns>Результат сравнения.</returns>
        public override readonly bool Equals(object obj)
        {
            return obj is Block block && this.Equals(block);
        }

        /// <summary>
        /// Получение хещ-кода блока.
        /// </summary>
        /// <returns>Хеш-код.</returns>
        public override readonly int GetHashCode()
        {
            return HashCode.Combine(this.low, this.high);
        }

        /// <summary>
        /// Получение строкового представления блока.
        /// </summary>
        /// <returns>Строковое представление блока.</returns>
        public override readonly string ToString()
        {
            byte[] bytes = this;
            return $"[{String.Join(", ", bytes)}]";
        }

        /// <summary>
        /// XOR двух блоков.
        /// </summary>
        /// <param name="left">Первый блок.</param>
        /// <param name="right">Второй блок.</param>
        /// <returns>Новый блок с вычисленным XOR.</returns>
        public static Block operator ^(Block left, Block right)
        {
            Block block = new Block()
            {
                low = left.low ^ right.low,
                high = left.high ^ right.high,
            };

            return block;
        }

        /// <summary>
        /// Сравнение двух блоков.
        /// </summary>
        /// <param name="left">Первый блок.</param>
        /// <param name="right">Второй блок.</param>
        /// <returns>Результат сравнения.</returns>
        public static bool operator ==(Block left, Block right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Сравнение двух блоков.
        /// </summary>
        /// <param name="left">Первый блок.</param>
        /// <param name="right">Второй блок.</param>
        /// <returns>Результат сравнения.</returns>
        public static bool operator !=(Block left, Block right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Преобразование массива в блок.
        /// </summary>
        /// <param name="data">Массив данных.</param>
        public static unsafe implicit operator Block(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            else if (data.Length != CryptoUtils.BlockSize)
            {
                throw new ArgumentException("Массив должен содержать 16 байт");
            }

            fixed (byte* ptr = data)
            {
                Block block = new Block
                {
                    low = *(ulong*)ptr,
                    high = *(ulong*)(ptr + sizeof(ulong))
                };

                return block;
            }
        }

        /// <summary>
        /// Преобразование блока в массив.
        /// </summary>
        /// <param name="block">Блок.</param>
        public static unsafe implicit operator byte[](Block block)
        {
            byte[] result = new byte[CryptoUtils.BlockSize];

            fixed (byte* ptr = result)
            {
                *(ulong*)ptr = block.low;
                *(ulong*)(ptr + sizeof(ulong)) = block.high;
            }

            return result;
        }
    }
}

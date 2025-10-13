using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Kuznyechik
{
    /// <summary>
    /// Блок данных (16 байт).
    /// </summary>
    [StructLayout(LayoutKind.Explicit, Size = CryptoUtils.BlockSize)]
    internal unsafe struct Block : IEquatable<Block>
    {
        /// <summary>
        /// Указатель на начало структуры.
        /// </summary>
        [FieldOffset(0)]
        private fixed byte bytes[CryptoUtils.BlockSize];

        /// <summary>
        /// Первая половина блока.
        /// </summary>
        [FieldOffset(0)]
        private ulong low;

        /// <summary>
        /// Вторая половина блока.
        /// </summary>
        [FieldOffset(sizeof(ulong))]
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
            StringBuilder stringBuilder = new StringBuilder("[");

            fixed (byte* value = this.bytes)
            {
                byte* ptr = value;
                byte* end = value + CryptoUtils.BlockSize - 1;

                while (ptr < end)
                {
                    stringBuilder.Append(*ptr);
                    stringBuilder.Append(", ");
                    ptr++;
                }

                stringBuilder.Append(*ptr);
            }
            
            stringBuilder.Append("]");
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Получить блок как Span.
        /// </summary>
        /// <returns>Span байтов блока.</returns>
        public readonly unsafe Span<byte> AsSpan()
        {
            fixed (byte* ptr = this.bytes)
            {
                return new Span<byte>(ptr, CryptoUtils.BlockSize);
            }
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
        public static implicit operator Block(byte[] data)
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
        /// Преобразование ReadOnlySpan в блок.
        /// </summary>
        /// <param name="data">Массив данных.</param>
        public static implicit operator Block(ReadOnlySpan<byte> data)
        {
            if (data.Length != CryptoUtils.BlockSize)
            {
                throw new ArgumentException("Массив должен содержать 16 байт");
            }

            fixed (byte* ptr = &MemoryMarshal.GetReference(data))
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
        /// Преобразование Span в блок.
        /// </summary>
        /// <param name="data">Массив данных.</param>
        public static implicit operator Block(Span<byte> data)
        {
            return (ReadOnlySpan<byte>)data;
        }

        /// <summary>
        /// Преобразование блока в массив.
        /// </summary>
        /// <param name="block">Блок.</param>
        public static implicit operator byte[](Block block)
        {
            byte[] result = new byte[CryptoUtils.BlockSize];

            fixed (byte* ptr = result)
            {
                *(ulong*)ptr = block.low;
                *(ulong*)(ptr + sizeof(ulong)) = block.high;
            }

            return result;
        }

        /// <summary>
        /// Получение байта по индексу.
        /// </summary>
        /// <param name="index">Индекс.</param>
        /// <returns>Байт по индексу.</returns>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public byte this[int index]
        {
            get
            {
                if ((uint)index >= CryptoUtils.BlockSize)
                {
                    throw new IndexOutOfRangeException();
                }

                fixed (byte* ptr = this.bytes)
                {
                    return ptr[index];
                }
            }
            set
            {
                if ((uint)index >= CryptoUtils.BlockSize)
                {
                    throw new IndexOutOfRangeException();
                }

                fixed (byte* ptr = this.bytes)
                {
                    ptr[index] = value;
                }
            }
        }
    }
}

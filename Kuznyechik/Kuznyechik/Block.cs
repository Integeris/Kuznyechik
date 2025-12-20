using System;
using System.Runtime.CompilerServices;
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
        /// <returns>Равны ли объекты.</returns>
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
        /// Получение хеш-кода блока.
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
            ReadOnlySpan<byte> span = this.AsReadOnlySpan();

            stringBuilder.Append(span[0]);

            for (int i = 1; i < span.Length; i++)
            {
                stringBuilder.Append(", ");
                stringBuilder.Append(span[i]);
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
            return MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in this.low), 2));
        }

        /// <summary>
        /// Получить блок как ReadOnlySpan.
        /// </summary>
        /// <returns>ReadOnlySpan байтов блока.</returns>
        public readonly unsafe ReadOnlySpan<byte> AsReadOnlySpan()
        {
            return MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in this.low), 2));
        }

        /// <summary>
        /// XOR двух блоков.
        /// </summary>
        /// <param name="other">Второй блок.</param>
        public void Xor(in Block other)
        {
            this.low ^= other.low;
            this.high ^= other.high;
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
                throw new ArgumentException("Размер массива должен быть равен 16 байтам.");
            }

            return Unsafe.As<byte, Block>(ref MemoryMarshal.GetReference<byte>(data));
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

            return Unsafe.As<byte, Block>(ref MemoryMarshal.GetReference<byte>(data));
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
            MemoryMarshal.Write(result, ref block);
            return result;
        }

        /// <summary>
        /// Получение или установка байта по указанному индексу.
        /// </summary>
        /// <param name="index">Индекс.</param>
        /// <returns>Байт по индексу.</returns>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public byte this[int index]
        {
            get => this.bytes[index]; 
            set => this.bytes[index] = value;
        }
    }
}

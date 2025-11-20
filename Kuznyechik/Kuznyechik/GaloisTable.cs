using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Kuznyechik
{
    /// <summary>
    /// Таблица предвычисленных значений поля Галуа.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 4096)]
    internal unsafe struct GaloisTable
    {
        /// <summary>
        /// Предвычесленные значения.
        /// </summary>
        private fixed byte data[4096];

        /// <summary>
        /// Создание таблицы предвычисленных значений поля Галуа.
        /// </summary>
        /// <param name="linearTransformation">Байты линейной трансформации.</param>
        public GaloisTable(in Block linearTransformation)
        {
            ref byte dataRef = ref Unsafe.As<GaloisTable, byte>(ref this);

            for (int i = Byte.MinValue; i <= Byte.MaxValue; i++)
            {
                for (byte j = 0; j < CryptoUtils.BlockSize; j++)
                {
                    this[(byte)i, j] = GaloisMultiplication((byte)i, linearTransformation[j]);
                }
            }
        }

        /// <summary>
        /// Получение представления таблицы в Span.
        /// </summary>
        /// <returns>Представление таблицы.</returns>
        public readonly ReadOnlySpan<byte> AsSpan()
        {
            return MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(this), 1));
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
                if ((key & 1) != 0)
                {
                    result ^= origin;
                }

                key >>= 1;

                // Вычисляем старший бит исходного байта.
                byte higherBit = (byte)(origin & 0x80);
                origin <<= 1;

                if (higherBit != 0)
                {
                    // Неприводимый полином для поля Галуа: x^8 + x^7 + x^6 + x + 1
                    origin ^= 0xC3;
                }
            }

            return result;
        }

        /// <summary>
        /// Получение результата умножения двух байт.
        /// </summary>
        /// <param name="x">Первое значение.</param>
        /// <param name="linearTransformationIndex">Индекс линейной трансформации.</param>
        /// <returns>Результат умножения.</returns>
        public readonly byte this[byte x, byte linearTransformationIndex]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ref byte arrPtr = ref Unsafe.As<GaloisTable, byte>(ref Unsafe.AsRef(this));
                return Unsafe.Add(ref arrPtr, x * 16 + linearTransformationIndex);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                ref byte arrPtr = ref Unsafe.As<GaloisTable, byte>(ref Unsafe.AsRef(this));
                Unsafe.Add(ref arrPtr, x * 16 + linearTransformationIndex) = value;
            }
        }

        /// <summary>
        /// Неявное преобразование таблицы в ReadOnlySpan.
        /// </summary>
        /// <param name="table">Таблица предвычисленных значений поля Галуа.</param>
        public static implicit operator ReadOnlySpan<byte>(GaloisTable table)
        {
            return table.AsSpan();
        }
    }
}

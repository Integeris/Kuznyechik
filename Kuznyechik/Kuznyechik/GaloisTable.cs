using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Kuznyechik
{
    /// <summary>
    /// Таблица предвычесленных значений поля Галуа.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 65536)]
    internal unsafe struct GaloisTable
    {
        /// <summary>
        /// Предвычесленные значения.
        /// </summary>
        private fixed byte data[65536];

        /// <summary>
        /// Создание таблицы предвычесленных значений поля Галуа.
        /// </summary>
        public GaloisTable()
        {
            ref byte dataRef = ref Unsafe.As<GaloisTable, byte>(ref this);

            for (int i = Byte.MinValue; i <= Byte.MaxValue; i++)
            {
                for (int j = Byte.MinValue; j <= Byte.MaxValue; j++)
                {
                    this[(byte)i, (byte)j] = GaloisMultiplication((byte)i, (byte)j);
                }
            }
        }

        /// <summary>
        /// Получение представления таблицы в Span.
        /// </summary>
        /// <returns>Представление таблицы.</returns>
        public readonly ReadOnlySpan<byte> AsSpan()
        {
            fixed (GaloisTable* tablePtr = &this)
            {
                return new ReadOnlySpan<byte>(tablePtr, 65536);
            }
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
        /// <param name="y">Второе значение.</param>
        /// <returns>Результат умножения.</returns>
        public readonly byte this[byte x, byte y]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                ref byte arrPtr = ref Unsafe.AsRef(this.data[0]);
                return Unsafe.Add(ref arrPtr, x << 8 | y);
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                ref byte arrPtr = ref Unsafe.AsRef(this.data[x << 8 | y]);
                arrPtr = value;
            }
        }

        /// <summary>
        /// Неявное преобразование таблицы в ReadOnlySpan.
        /// </summary>
        /// <param name="table">Таблица предвычесленных значений поля Галуа.</param>
        public static implicit operator ReadOnlySpan<byte>(GaloisTable table)
        {
            return table.AsSpan();
        }
    }
}

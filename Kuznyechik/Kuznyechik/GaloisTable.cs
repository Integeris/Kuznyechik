using System;
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
            for (int i = Byte.MinValue; i <= Byte.MaxValue; i++)
            {
                for (int j = Byte.MinValue; j <= Byte.MaxValue; j++)
                {
                    this.data[i * 256 + j] = GaloisMultiplication((byte)i, (byte)j);
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
                if ((key & 0b01) == 1)
                {
                    result ^= origin;
                }

                key >>= 1;

                // Вычисляем старший бит исходного байта.
                byte higherBit = (byte)(origin & 0b10000000);
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
            get
            {
                fixed (byte* ptr = this.data)
                {
                    return ptr[x * 256 + y];
                }
            }
            set
            {
                fixed (byte* ptr = this.data)
                {
                    ptr[x * 256 + y] = value;
                }
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

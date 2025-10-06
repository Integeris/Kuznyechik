using ILGPU;
using ILGPU.Runtime;

namespace Kuznyechik
{
    /// <summary>
    /// Данные для передачи в ядро видеокарты.
    /// </summary>
    internal struct KernelData
    {
        /// <summary>
        /// Данные.
        /// </summary>
        private ArrayView1D<byte, Stride1D.Dense> data;

        /// <summary>
        /// Ключи.
        /// </summary>
        private ArrayView1D<byte, Stride1D.Dense> keys;

        /// <summary>
        /// Байты линейной трансформации.
        /// </summary>
        private ArrayView1D<byte, Stride1D.Dense> linearTransformation;

        /// <summary>
        /// Таблица для нелинейного преобразования.
        /// </summary>
        private ArrayView1D<byte, Stride1D.Dense> replaceBytes;

        /// <summary>
        /// Таблица предвычесленных значений поля Галуа.
        /// </summary>
        private ArrayView2D<byte, Stride2D.DenseX> galoisMultiplicationTable;

        /// <summary>
        /// Данные.
        /// </summary>
        public ArrayView1D<byte, Stride1D.Dense> Data
        {
            readonly get => data;
            set => data = value;
        }

        /// <summary>
        /// Ключи.
        /// </summary>
        public ArrayView1D<byte, Stride1D.Dense> Keys
        {
            readonly get => keys;
            set => keys = value;
        }

        /// <summary>
        /// Байты линейной трансформации.
        /// </summary>
        public ArrayView1D<byte, Stride1D.Dense> LinearTransformation
        {
            readonly get => linearTransformation;
            set => linearTransformation = value;
        }

        /// <summary>
        /// Таблица для нелинейного преобразования.
        /// </summary>
        public ArrayView1D<byte, Stride1D.Dense> ReplaceBytes
        {
            readonly get => replaceBytes;
            set => replaceBytes = value;
        }

        /// <summary>
        /// Таблица предвычесленных значений поля Галуа.
        /// </summary>
        public ArrayView2D<byte, Stride2D.DenseX> GaloisMultiplicationTable
        {
            readonly get => galoisMultiplicationTable;
            set => galoisMultiplicationTable = value;
        }

        /// <summary>
        /// Создание данных для передачи в ядро видеокарты.
        /// </summary>
        /// <param name="data">Данные.</param>
        /// <param name="keys">Ключи.</param>
        /// <param name="linearTransformation">Байты линейной трансформации.</param>
        /// <param name="replaceBytes">Таблица для нелинейного преобразования.</param>
        /// <param name="galoisMultiplicationTable">Таблица предвычесленных значений поля Галуа.</param>
        public KernelData(ArrayView1D<byte, Stride1D.Dense> data, 
            ArrayView1D<byte, Stride1D.Dense> keys, 
            ArrayView1D<byte, Stride1D.Dense> linearTransformation, 
            ArrayView1D<byte, Stride1D.Dense> replaceBytes,
            ArrayView2D<byte, Stride2D.DenseX> galoisMultiplicationTable)
        {
            this.data = data;
            this.keys = keys;
            this.linearTransformation = linearTransformation;
            this.replaceBytes = replaceBytes;
            this.galoisMultiplicationTable = galoisMultiplicationTable;
        }
    }
}

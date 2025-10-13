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
        private ArrayView1D<Block, Stride1D.Dense> data;

        /// <summary>
        /// Ключи.
        /// </summary>
        private ArrayView1D<Block, Stride1D.Dense> keys;

        /// <summary>
        /// Байты линейной трансформации.
        /// </summary>
        private VariableView<Block> linearTransformation;

        /// <summary>
        /// Таблица для нелинейного преобразования.
        /// </summary>
        private ArrayView1D<byte, Stride1D.Dense> replaceBytes;

        /// <summary>
        /// Таблица предвычесленных значений поля Галуа.
        /// </summary>
        private VariableView<GaloisTable> galoisTable;

        /// <summary>
        /// Данные.
        /// </summary>
        public ArrayView1D<Block, Stride1D.Dense> Data
        {
            readonly get => this.data;
            set => this.data = value;
        }

        /// <summary>
        /// Ключи.
        /// </summary>
        public ArrayView1D<Block, Stride1D.Dense> Keys
        {
            readonly get => this.keys;
            set => this.keys = value;
        }

        /// <summary>
        /// Байты линейной трансформации.
        /// </summary>
        public VariableView<Block> LinearTransformation
        {
            readonly get => this.linearTransformation;
            set => this.linearTransformation = value;
        }

        /// <summary>
        /// Таблица для нелинейного преобразования.
        /// </summary>
        public ArrayView1D<byte, Stride1D.Dense> ReplaceBytes
        {
            readonly get => this.replaceBytes;
            set => this.replaceBytes = value;
        }

        /// <summary>
        /// Таблица предвычесленных значений поля Галуа.
        /// </summary>
        public VariableView<GaloisTable> GaloisTable
        {
            readonly get => this.galoisTable;
            set => this.galoisTable = value;
        }

        /// <summary>
        /// Создание данных для передачи в ядро видеокарты.
        /// </summary>
        /// <param name="data">Данные.</param>
        /// <param name="keys">Ключи.</param>
        /// <param name="linearTransformation">Байты линейной трансформации.</param>
        /// <param name="replaceBytes">Таблица для нелинейного преобразования.</param>
        /// <param name="galoisTable">Таблица предвычесленных значений поля Галуа.</param>
        public KernelData(ArrayView1D<Block, Stride1D.Dense> data, 
            ArrayView1D<Block, Stride1D.Dense> keys,
            VariableView<Block> linearTransformation, 
            ArrayView1D<byte, Stride1D.Dense> replaceBytes,
            VariableView<GaloisTable> galoisTable)
        {
            this.data = data;
            this.keys = keys;
            this.linearTransformation = linearTransformation;
            this.replaceBytes = replaceBytes;
            this.galoisTable = galoisTable;
        }
    }
}

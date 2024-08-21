using System;
using System.IO;
using System.Threading.Tasks;

namespace Kuznyechik
{
    /// <summary>
    /// Шифратор (алгоритм "Кузнечик").
    /// </summary>
    public class Scrambler
    {
        /// <summary>
        /// Размер блока.
        /// </summary>
        public const int BlockSize = 16;

        /// <summary>
        /// Размер ключа.
        /// </summary>
        public const int KeySize = 32;

        /// <summary>
        /// Буффер для чтения из потока и одновлеменного шифрования блоков.
        /// </summary>
        private const int bufferSize = 256;

        /// <summary>
        /// Таблица для нелинейного преобразования.
        /// </summary>
        private readonly byte[] replaceBytes =
        {
            0xFC, 0xEE, 0xDD, 0x11, 0xCF, 0x6E, 0x31, 0x16,
            0xFB, 0xC4, 0xFA, 0xDA, 0x23, 0xC5, 0x04, 0x4D,
            0xE9, 0x77, 0xF0, 0xDB, 0x93, 0x2E, 0x99, 0xBA,
            0x17, 0x36, 0xF1, 0xBB, 0x14, 0xCD, 0x5F, 0xC1,
            0xF9, 0x18, 0x65, 0x5A, 0xE2, 0x5C, 0xEF, 0x21,
            0x81, 0x1C, 0x3C, 0x42, 0x8B, 0x01, 0x8E, 0x4F,
            0x05, 0x84, 0x02, 0xAE, 0xE3, 0x6A, 0x8F, 0xA0,
            0x06, 0x0B, 0xED, 0x98, 0x7F, 0xD4, 0xD3, 0x1F,
            0xEB, 0x34, 0x2C, 0x51, 0xEA, 0xC8, 0x48, 0xAB,
            0xF2, 0x2A, 0x68, 0xA2, 0xFD, 0x3A, 0xCE, 0xCC,
            0xB5, 0x70, 0x0E, 0x56, 0x08, 0x0C, 0x76, 0x12,
            0xBF, 0x72, 0x13, 0x47, 0x9C, 0xB7, 0x5D, 0x87,
            0x15, 0xA1, 0x96, 0x29, 0x10, 0x7B, 0x9A, 0xC7,
            0xF3, 0x91, 0x78, 0x6F, 0x9D, 0x9E, 0xB2, 0xB1,
            0x32, 0x75, 0x19, 0x3D, 0xFF, 0x35, 0x8A, 0x7E,
            0x6D, 0x54, 0xC6, 0x80, 0xC3, 0xBD, 0x0D, 0x57,
            0xDF, 0xF5, 0x24, 0xA9, 0x3E, 0xA8, 0x43, 0xC9,
            0xD7, 0x79, 0xD6, 0xF6, 0x7C, 0x22, 0xB9, 0x03,
            0xE0, 0x0F, 0xEC, 0xDE, 0x7A, 0x94, 0xB0, 0xBC,
            0xDC, 0xE8, 0x28, 0x50, 0x4E, 0x33, 0x0A, 0x4A,
            0xA7, 0x97, 0x60, 0x73, 0x1E, 0x00, 0x62, 0x44,
            0x1A, 0xB8, 0x38, 0x82, 0x64, 0x9F, 0x26, 0x41,
            0xAD, 0x45, 0x46, 0x92, 0x27, 0x5E, 0x55, 0x2F,
            0x8C, 0xA3, 0xA5, 0x7D, 0x69, 0xD5, 0x95, 0x3B,
            0x07, 0x58, 0xB3, 0x40, 0x86, 0xAC, 0x1D, 0xF7,
            0x30, 0x37, 0x6B, 0xE4, 0x88, 0xD9, 0xE7, 0x89,
            0xE1, 0x1B, 0x83, 0x49, 0x4C, 0x3F, 0xF8, 0xFE,
            0x8D, 0x53, 0xAA, 0x90, 0xCA, 0xD8, 0x85, 0x61,
            0x20, 0x71, 0x67, 0xA4, 0x2D, 0x2B, 0x09, 0x5B,
            0xCB, 0x9B, 0x25, 0xD0, 0xBE, 0xE5, 0x6C, 0x52,
            0x59, 0xA6, 0x74, 0xD2, 0xE6, 0xF4, 0xB4, 0xC0,
            0xD1, 0x66, 0xAF, 0xC2, 0x39, 0x4B, 0x63, 0xB6
        };

        /// <summary>
        /// Таблица для обратного нелинейного преобразования.
        /// </summary>
        private readonly byte[] reversReplaceBytes =
        {
            0xA5, 0x2D, 0x32, 0x8F, 0x0E, 0x30, 0x38, 0xC0,
            0x54, 0xE6, 0x9E, 0x39, 0x55, 0x7E, 0x52, 0x91,
            0x64, 0x03, 0x57, 0x5A, 0x1C, 0x60, 0x07, 0x18,
            0x21, 0x72, 0xA8, 0xD1, 0x29, 0xC6, 0xA4, 0x3F,
            0xE0, 0x27, 0x8D, 0x0C, 0x82, 0xEA, 0xAE, 0xB4,
            0x9A, 0x63, 0x49, 0xE5, 0x42, 0xE4, 0x15, 0xB7,
            0xC8, 0x06, 0x70, 0x9D, 0x41, 0x75, 0x19, 0xC9,
            0xAA, 0xFC, 0x4D, 0xBF, 0x2A, 0x73, 0x84, 0xD5,
            0xC3, 0xAF, 0x2B, 0x86, 0xA7, 0xB1, 0xB2, 0x5B,
            0x46, 0xD3, 0x9F, 0xFD, 0xD4, 0x0F, 0x9C, 0x2F,
            0x9B, 0x43, 0xEF, 0xD9, 0x79, 0xB6, 0x53, 0x7F,
            0xC1, 0xF0, 0x23, 0xE7, 0x25, 0x5E, 0xB5, 0x1E,
            0xA2, 0xDF, 0xA6, 0xFE, 0xAC, 0x22, 0xF9, 0xE2,
            0x4A, 0xBC, 0x35, 0xCA, 0xEE, 0x78, 0x05, 0x6B,
            0x51, 0xE1, 0x59, 0xA3, 0xF2, 0x71, 0x56, 0x11,
            0x6A, 0x89, 0x94, 0x65, 0x8C, 0xBB, 0x77, 0x3C,
            0x7B, 0x28, 0xAB, 0xD2, 0x31, 0xDE, 0xC4, 0x5F,
            0xCC, 0xCF, 0x76, 0x2C, 0xB8, 0xD8, 0x2E, 0x36,
            0xDB, 0x69, 0xB3, 0x14, 0x95, 0xBE, 0x62, 0xA1,
            0x3B, 0x16, 0x66, 0xE9, 0x5C, 0x6C, 0x6D, 0xAD,
            0x37, 0x61, 0x4B, 0xB9, 0xE3, 0xBA, 0xF1, 0xA0,
            0x85, 0x83, 0xDA, 0x47, 0xC5, 0xB0, 0x33, 0xFA,
            0x96, 0x6F, 0x6E, 0xC2, 0xF6, 0x50, 0xFF, 0x5D,
            0xA9, 0x8E, 0x17, 0x1B, 0x97, 0x7D, 0xEC, 0x58,
            0xF7, 0x1F, 0xFB, 0x7C, 0x09, 0x0D, 0x7A, 0x67,
            0x45, 0x87, 0xDC, 0xE8, 0x4F, 0x1D, 0x4E, 0x04,
            0xEB, 0xF8, 0xF3, 0x3E, 0x3D, 0xBD, 0x8A, 0x88,
            0xDD, 0xCD, 0x0B, 0x13, 0x98, 0x02, 0x93, 0x80,
            0x90, 0xD0, 0x24, 0x34, 0xCB, 0xED, 0xF4, 0xCE,
            0x99, 0x10, 0x44, 0x40, 0x92, 0x3A, 0x01, 0x26,
            0x12, 0x1A, 0x48, 0x68, 0xF5, 0x81, 0x8B, 0xC7,
            0xD6, 0x20, 0x0A, 0x08, 0x00, 0x4C, 0xD7, 0x74
        };

        /// <summary>
        /// Байты линейной трансформации.
        /// </summary>
        private readonly byte[] linearTransformation;

        /// <summary>
        /// Константы для расчётов.
        /// </summary>
        private readonly byte[][] constants = new byte[32][]
        {
            new byte[]
            {
                1, 148, 132, 221,
                16, 189, 39, 93,
                184, 122, 72, 108,
                114, 118, 162, 110
            },
            new byte[]
            {
                2, 235, 203, 121,
                32, 185, 78, 186,
                179, 244, 144, 216,
                228, 236, 135, 220
            },
            new byte[]
            {
                3, 127, 79, 164,
                48, 4, 105, 231,
                11, 142, 216, 180,
                150, 154, 37, 178
            },
            new byte[]
            {
                4, 21, 85, 242,
                64, 177, 156, 183,
                165, 43, 227, 115,
                11, 27, 205, 123
            },
            new byte[]
            {
                5, 129, 209, 47,
                80, 12, 187, 234,
                29, 81, 171, 31,
                121, 109, 111, 21
            },
            new byte[]
            {
                6, 254, 158, 139,
                96, 8, 210, 13,
                22, 223, 115, 171,
                239, 247, 74, 167
            },
            new byte[]
            {
                7, 106, 26, 86,
                112, 181, 245, 80,
                174, 165, 59, 199,
                157, 129, 232, 201
            },
            new byte[]
            {
                8, 42, 170, 39,
                128, 161, 251, 173,
                137, 86, 5, 230,
                22, 54, 89, 246
            },
            new byte[]
            {
                9, 190, 46, 250,
                144, 28, 220, 240,
                49, 44, 77, 138,
                100, 64, 251, 152
            },
            new byte[]
            {
                10, 193, 97, 94,
                160, 24, 181, 23,
                58, 162, 149, 62,
                242, 218, 222, 42
            },
            new byte[]
            {
                11, 85, 229, 131,
                176, 165, 146, 74,
                130, 216, 221, 82,
                128, 172, 124, 68
            },
            new byte[]
            {
                12, 63, 255, 213,
                192, 16, 103, 26,
                44, 125, 230, 149,
                29, 45, 148, 141
            },
            new byte[]
            {
                13, 171, 123, 8,
                208, 173, 64, 71,
                148, 7, 174, 249,
                111, 91, 54, 227
            },
            new byte[]
            {
                14, 212, 52, 172,
                224, 169, 41, 160,
                159, 137, 118, 77,
                249, 193, 19, 81
            },
            new byte[]
            {
                15, 64, 176, 113,
                240, 20, 14, 253,
                39, 243, 62, 33,
                139, 183, 177, 63
            },
            new byte[]
            {
                16, 84, 151, 78,
                195, 129, 53, 153,
                209, 172, 10, 15,
                44, 108, 178, 47
            },
            new byte[]
            {
                17, 192, 19, 147,
                211, 60, 18, 196,
                105, 214, 66, 99,
                94, 26, 16, 65
            },
            new byte[]
            {
                18, 191, 92, 55,
                227, 56, 123, 35,
                98, 88, 154, 215,
                200, 128, 53, 243
            },
            new byte[]
            {
                19, 43, 216, 234,
                243, 133, 92, 126,
                218, 34, 210, 187,
                186, 246, 151, 157
            },
            new byte[]
            {
                20, 65, 194, 188,
                131, 48, 169, 46,
                116, 135, 233, 124,
                39, 119, 127, 84
            },
            new byte[]
            {
                21, 213, 70, 97,
                147, 141, 142, 115,
                204, 253, 161, 16,
                85, 1, 221, 58
            },
            new byte[]
            {
                22, 170, 9, 197,
                163, 137, 231, 148,
                199, 115, 121, 164,
                195, 155, 248, 136
            },
            new byte[]
            {
                23, 62, 141, 24,
                179, 52, 192, 201,
                127, 9, 49, 200,
                177, 237, 90, 230
            },
            new byte[]
            {
            24, 126, 61, 105,
                67, 32, 206, 52,
                88, 250, 15, 233,
                58, 90, 235, 217
            },
            new byte[]
            {
                25, 234, 185, 180,
                83, 157, 233, 105,
                224, 128, 71, 133,
                72, 44, 73, 183
            },
            new byte[]
            {
                26, 149, 246, 16,
                99, 153, 128, 142,
                235, 14, 159, 49,
                222, 182, 108, 5
            },
            new byte[]
            {
                27, 1, 114, 205,
                115, 36, 167, 211,
                83, 116, 215, 93,
                172, 192, 206, 107
            },
            new byte[]
            {
                28, 107, 104, 155,
                3, 145, 82, 131,
                253, 209, 236, 154,
                49, 65, 38, 162
            },new byte[]
            {
                29, 255, 236, 70,
                19, 44, 117, 222,
                69, 171, 164, 246,
                67, 55, 132, 204
            },
            new byte[]
            {
                30, 128, 163, 226,
                35, 40, 28, 57,
                78, 37, 124, 66,
                213, 173, 161, 126
            },
            new byte[]
            {
                31, 20, 39, 63,
                51, 149, 59, 100,
                246, 95, 52, 46,
                167, 219, 3, 16
            },
            new byte[]
            {
                32, 168, 237, 156,
                69, 193, 106, 241,
                97, 155, 20, 30,
                88, 216, 167, 94
            }
        };

        /// <summary>
        /// Ключи.
        /// </summary>
        private readonly byte[][] keys;

        /// <summary>
        /// Создание Шифратора.
        /// </summary>
        /// <param name="key">Ключ.</param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public Scrambler(byte[] key)
        {
            if (key.Length != KeySize)
            {
                throw new ArgumentOutOfRangeException(nameof(key), $"Длинна ключа должна быть {KeySize} байта.");
            }

            linearTransformation = new byte[]
            {
                1, 148, 32, 133, 16, 194, 192, 1, 251, 1, 192, 194, 16, 133, 32, 148
            };

            keys = new byte[10][];

            for (int i = 0; i < keys.Length; i++)
            {
                keys[i] = new byte[BlockSize];
            }

            Array.Copy(key, keys[0], BlockSize);
            Array.Copy(key, BlockSize, keys[1], 0, BlockSize);

            GenerationRoundKeys();
        }

        /// <summary>
        /// Шифрование массива блоков.
        /// </summary>
        /// <param name="arr">Массив.</param>
        /// <exception cref="ArgumentException"></exception>
        public void Encrypt(ref byte[] arr)
        {
            if (arr == null)
            {
                throw new ArgumentException("Массив данных не может быть равен null.");
            }

            int lossBytes = BlockSize - arr.Length % BlockSize;
            Array.Resize(ref arr, arr.Length + lossBytes);
            arr[^1] = (byte)lossBytes;
            
            Encrypt(arr);
        }

        /// <summary>
        /// Шифрование данных из потока в поток.
        /// </summary>
        /// <param name="dataStream">Поток данных.</param>
        /// <param name="encryptedStream">Выходной поток с зашифрованными данными.</param>
        /// <exception cref="ArgumentException"></exception>
        public void Encrypt(Stream dataStream, Stream encryptedStream)
        {
            if (dataStream == null)
            {
                throw new ArgumentException("Поток данных не может быть null.", nameof(dataStream));
            }
            else if (encryptedStream == null)
            {
                throw new ArgumentException("Поток с шифрованными данными не может быть null.", nameof(encryptedStream));
            }
            else if (!encryptedStream.CanWrite)
            {
                throw new ArgumentException("Поток для записи зашифрованных байт должен быть доступен для записи.", nameof(encryptedStream));
            }
            else if (dataStream == encryptedStream)
            {
                throw new ArgumentException("Нельзя выполнить чтение и запись в один и тотже поток.", nameof(encryptedStream));
            }

            byte loss = (byte)(BlockSize - dataStream.Length % BlockSize);
            byte[] buffer = new byte[bufferSize];

            for (int i = 0; i < dataStream.Length / bufferSize; i++)
            {
                dataStream.Read(buffer, 0, bufferSize);
                Encrypt(buffer);
                encryptedStream.Write(buffer, 0, bufferSize);
            }

            {
                long leftByteCount = dataStream.Length - dataStream.Position;
                buffer = new byte[leftByteCount + loss];
                dataStream.Read(buffer, 0, (int)leftByteCount);
                buffer[^1] = loss;
            }
            
            Encrypt(buffer);
            encryptedStream.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Расшифровка массива.
        /// </summary>
        /// <param name="arr">Массив.</param>
        /// <exception cref="ArgumentException"></exception>
        public void Decrypt(ref byte[] arr)
        {
            if (arr == null)
            {
                throw new ArgumentException("Массив данных не может быть равен null.");
            }
            else if (arr.Length % BlockSize != 0)
            {
                throw new ArgumentException($"Можно расшифровать только целое количество блоков ({BlockSize} байт).", nameof(arr));
            }

            Decrypt(arr);
            byte lossByte = arr[^1];

            if (lossByte > BlockSize)
            {
                throw new Exception($"Данные зашифрованны не верно.");
            }

            Array.Resize(ref arr, arr.Length - lossByte);
        }

        /// <summary>
        /// Расшифровывание данных из потока в поток.
        /// </summary>
        /// <param name="dataStream">Поток данных.</param>
        /// <param name="decryptedStream">Выходной поток с расшифрованными данными.</param>
        /// <exception cref="ArgumentException"></exception>
        public void Decrypt(Stream dataStream, Stream decryptedStream)
        {
            if (dataStream == null)
            {
                throw new ArgumentException("Поток данных не может быть null.", nameof(dataStream));
            }
            else if (decryptedStream == null)
            {
                throw new ArgumentException("Поток с шифрованными данными не может быть null.", nameof(decryptedStream));
            }
            else if (!decryptedStream.CanWrite)
            {
                throw new ArgumentException("Поток для записи зашифрованных байт должен быть доступен для записи.", nameof(decryptedStream));
            }
            else if (dataStream == decryptedStream)
            {
                throw new ArgumentException("Нельзя выполнить чтение и запись в один и тотже поток.", nameof(decryptedStream));
            }

            byte[] buffer = new byte[bufferSize];

            for (int i = 0; i < dataStream.Length / bufferSize; i++)
            {
                dataStream.Read(buffer, 0, bufferSize);
                Decrypt(buffer);
                decryptedStream.Write(buffer, 0, bufferSize);
            }

            buffer = new byte[dataStream.Length - dataStream.Position];
            dataStream.Read(buffer, 0, buffer.Length);
            Decrypt(buffer);

            byte loss = buffer[^1];
            decryptedStream.Write(buffer, 0, buffer.Length - loss);
        }

        /// <summary>
        /// Шифрование данных из массива.
        /// </summary>
        /// <param name="arr">Данные.</param>
        private void Encrypt(byte[] arr)
        {
            ProcessBlocks(arr, EncryptBlock);
        }

        /// <summary>
        /// Расшифровка данных из массива.
        /// </summary>
        /// <param name="arr">Данные.</param>
        private void Decrypt(byte[] arr)
        {
            ProcessBlocks(arr, DecryptBlock);
        }

        /// <summary>
        /// Шифрует или расшифровывает массив данных.
        /// </summary>
        /// <param name="arr">Массив данных.</param>
        /// <param name="action">Метод шифрования или расшифровывания данных.</param>
        /// <exception cref="ArgumentException"></exception>
        private static void ProcessBlocks(byte[] arr, Action<byte[]> action)
        {
            if (arr.Length % BlockSize != 0)
            {
                throw new ArgumentException($"Массив данных должен быть кратен размеру блока ({BlockSize} байт).", nameof(arr));
            }

            Parallel.For(0, arr.Length / BlockSize, (i) =>
            {
                Span<byte> span = new Span<byte>(arr, i * BlockSize, BlockSize);
                byte[] tmpBlock = span.ToArray();
                action(tmpBlock);
                tmpBlock.CopyTo(span);
            });
        }

        /// <summary>
        /// Шифрование блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        private void EncryptBlock(byte[] block)
        {
            for (int i = 0; i < 9; i++)
            {
                ExclusiveOR(block, keys[i]);
                ReplaceBytes(block, replaceBytes);
                MultiTransform(block, TransformBlock);
            }

            ExclusiveOR(block, keys[9]);
        }

        /// <summary>
        /// Расшифровка блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        private void DecryptBlock(byte[] block)
        {
            ExclusiveOR(block, keys[9]);

            for (int i = 8; i >= 0; i--)
            {
                MultiTransform(block, ReversTransformBlock);
                ReplaceBytes(block, reversReplaceBytes);
                ExclusiveOR(block, keys[i]);
            }
        }

        /// <summary>
        /// Исключающее ИЛИ для блоков.
        /// </summary>
        /// <param name="source">Источник.</param>
        /// <param name="key">Поправка.</param>
        private static void ExclusiveOR(byte[] source, byte[] key)
        {
            for (int i = 0; i < BlockSize; i++)
            {
                source[i] ^= key[i];
            }
        }

        /// <summary>
        /// Замена байт блока на байты из указанной таблицы.
        /// </summary>
        /// <param name="block">Блок данных.</param>
        /// <param name="replaceBytes">Таблица замены.</param>
        private static void ReplaceBytes(byte[] block, byte[] replaceBytes)
        {
            for (int i = 0; i < BlockSize; i++)
            {
                block[i] = replaceBytes[block[i]];
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

            // цикл для каждого бита (В байте 8 битов)
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
                    // x^8 + x^7 + x^6 + x + 1
                    origin ^= 195;
                }
            }

            return result;
        }

        /// <summary>
        /// Трансформация блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        private void TransformBlock(byte[] block)
        {
            byte sum = GaloisMultiplication(block[0], linearTransformation[0]);

            for (int i = 1; i < BlockSize; i++)
            {
                block[i - 1] = block[i];
                sum ^= GaloisMultiplication(block[i], linearTransformation[i]);
            }

            block[15] = sum;
        }

        /// <summary>
        /// Обратная трансформация блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        private void ReversTransformBlock(byte[] block)
        {
            byte sum = block[15];

            for (int i = BlockSize - 1; i > 0; i--)
            {
                block[i] = block[i - 1];
                sum ^= GaloisMultiplication(block[i], linearTransformation[i]);
            }

            block[0] = sum;
        }

        /// <summary>
        /// Шифрование блока.
        /// </summary>
        /// <param name="block">Блок.</param>
        /// <param name="transformBlock">Метод преобразования блока.</param>
        private static void MultiTransform(byte[] block, Action<byte[]> transformBlock)
        {
            for (int i = 0; i < BlockSize; i++)
            {
                transformBlock(block);
            }
        }

        /// <summary>
        /// Ячейка Фейстеля.
        /// </summary>
        /// <param name="firstKeys">Первый ключ.</param>
        /// <param name="secondKey">Второй ключ.</param>
        /// <param name="constants">Константы.</param>
        private void FeistelCell(byte[] firstKeys, byte[] secondKey, byte[] constants)
        {
            byte[] tmpKey = new byte[firstKeys.Length];
            Array.Copy(firstKeys, tmpKey, firstKeys.Length);

            ExclusiveOR(tmpKey, constants);
            ReplaceBytes(tmpKey, replaceBytes);
            MultiTransform(tmpKey, TransformBlock);
            ExclusiveOR(tmpKey, secondKey);

            Array.Copy(firstKeys, secondKey, firstKeys.Length);
            Array.Copy(tmpKey, firstKeys, tmpKey.Length);
        }

        /// <summary>
        /// Генерация раундовых ключей.
        /// </summary>
        private void GenerationRoundKeys()
        {
            for (int i = 0; i < 4; i++)
            {
                int firstPart = i * 2 + 2;
                int secondPart = i * 2 + 3;

                Array.Copy(keys[firstPart - 2], keys[firstPart], BlockSize);
                Array.Copy(keys[secondPart - 2], keys[secondPart], BlockSize);

                for (int j = 0; j < 8; j++)
                {
                    FeistelCell(keys[firstPart], keys[secondPart], constants[j + 8 * i]);
                }
            }
        }
    }
}
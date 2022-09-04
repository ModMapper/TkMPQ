using System.IO;
using System;
using TkMPQLib;
using TkLib;

namespace TkCompLib
{
    using Compression.Pkware;
    using Compression;

    /// <summary>Tk MPQ Compression Library</summary>
    public static class TkComp
    {
        private delegate int pCompress(FixedStream sIn, FixedStream sOut, WaveLevel Wave);
        private delegate int pDecompress(FixedStream sIn, FixedStream sOut);

        private struct CompressTable
        {
            public Compressions Compression;
            public pCompress Compress;
            public pDecompress Decompress;

            public CompressTable(Compressions comp, pCompress cFunc, pDecompress dFunc) {
                Compression = comp;
                Compress = cFunc;
                Decompress = dFunc;
            }
        }

        //MPQ compress and decompress order table
        private static CompressTable[] CompTable = {
            new CompressTable(Compressions.Sparse,          Compress_Sparse,        Decompress_Sparse),
            new CompressTable(Compressions.ADPCM_Mono,      Compress_ADPCM_Mono,    Decompress_ADPCM_Mono),
            new CompressTable(Compressions.ADPCM_Stereo,    Compress_ADPCM_Stereo,  Decompress_ADPCM_Stereo),
            new CompressTable(Compressions.Huffman,         Compress_Huffman,       Decompress_Huffman),
            new CompressTable(Compressions.Deflate,         Compress_Deflate,       Decompress_Deflate),
            new CompressTable(Compressions.Implode,         Compress_Implode,       Decompress_Implode),
            new CompressTable(Compressions.BZip2,           Compress_BZip2,         Decompress_BZip2)};

        /// <summary>MPQ Compress</summary>
        /// <param name="Buffer">Input Buffer</param>
        /// <param name="Compression">Compression Type</param>
        /// <param name="Wave">Wave Compression Level</param>
        /// <returns>Compress Data</returns>
        public static byte[] Compress(byte[] Buffer, Compressions Compression, WaveLevel Wave = WaveLevel.QualityHigh) {
            byte[] Buf = new byte[Buffer.Length];
            int Result = Compress(Buffer, 0, Buffer.Length, Buf, 0, Buf.Length, Compression, Wave);
            Array.Resize(ref Buf, Result);
            return Buf;
        }

        /// <summary>MPQ Compress</summary>
        /// <param name="bIn">Input Buffer</param>
        /// <param name="pIn">Input Offset</param>
        /// <param name="cIn">Input Size</param>
        /// <param name="bOut">Output Buffer</param>
        /// <param name="pOut">Output Offset</param>
        /// <param name="cOut">Output Size</param>
        /// <param name="Compression">Compression Type</param>
        /// <param name="Wave">Wave Compression Level</param>
        /// <returns>Compressed Size</returns>
        public static int Compress(byte[] bIn, int pIn, int cIn, byte[] bOut, int pOut, int cOut, Compressions Compression, WaveLevel Wave = WaveLevel.QualityHigh) {
            if(cIn == 0) return 0;
            if(Compression == Compressions.LZMA)
                return Compress_LZMA(bIn, pIn, cIn, bOut, pOut, cOut);
            byte[] Buf = CopyArray(bIn, pIn, cIn);
            var sOut = new FixedStream(bOut, pOut, cOut);
            var sIn = new FixedStream(Buf);
            Compressions Result = 0;
            bool Buffered = true;

            for(int i = 0; i < CompTable.Length; i++) {
                if((CompTable[i].Compression & Compression) == 0) continue;
                sOut.Write((byte)(Result | CompTable[i].Compression));
                if(CompTable[i].Compress(sIn, sOut, Wave) == -1) {
                    sIn.Position = 0;
                    sOut.Position = 0;
                    continue;
                }

                Buffered = !Buffered;
                Swap(ref sIn, ref sOut);
                sOut.Clear(sIn.Position);
                sIn.Clear(sIn.Position);
                sIn.Skip(1);
                Result |= CompTable[i].Compression;
            }
            if(cOut < cIn && Result == 0) return -1;
            if(Buffered) sOut.Write(Buf, 0, sIn.Length);
            return sIn.Length;
        }

        /// <summary>MPQ Decompress</summary>
        /// <param name="Buffer">Input Buffer</param>
        /// <param name="Result">Decompressed Size</param>
        /// <returns>Decompressed Data</returns>
        public static byte[] Decompress(byte[] Buffer, int Result) {
            byte[] Buf = new byte[Result];
            Decompress(Buffer, 0, Buffer.Length, Buf, 0, Result);
            return Buf;
        }

        /// <summary>MPQ Decompress</summary>
        /// <param name="bIn">Input Buffer</param>
        /// <param name="pIn">Input Offset</param>
        /// <param name="cIn">Input Size</param>
        /// <param name="bOut">Output Buffer</param>
        /// <param name="pOut">Output Offset</param>
        /// <param name="cOut">Output Size</param>
        /// <returns>Decompressed Size</returns>
        public static int Decompress(byte[] bIn, int pIn, int cIn, byte[] bOut, int pOut, int cOut) {
            if(cIn == 0) return 0;
            if(cIn == cOut) {
                Array.Copy(bIn, pIn, bOut, pOut, cIn);
                return cOut;
            }
            Compressions Compression = (Compressions)bIn[pIn];
            if(Compression == Compressions.LZMA)
                return Decompress_LZMA(bIn, pIn, cIn, bOut, pOut, cOut);
            byte[] Buf = new byte[cOut];
            var sOut = new FixedStream(bOut, pOut, cOut);
            var sIn = new FixedStream(Buf);
            bool Buffered = true;

            sIn.Write(bIn, pIn + 1, cIn - 1);
            sIn.Clear(sIn.Position);

            for(int i = CompTable.Length - 1; 0 <= i; i--) {
                if((CompTable[i].Compression & Compression) == 0) continue;
                if(CompTable[i].Decompress(sIn, sOut) == -1) break;
                Buffered = !Buffered;
                Swap(ref sIn, ref sOut);
                sIn.Clear(sIn.Position);
                sOut.Clear();
            }
            if(Buffered) sOut.Write(Buf, 0, sIn.Length);
            return sIn.Length;
        }

        /// <summary>Pkware Implode</summary>
        /// <param name="Buffer">Input Buffer</param>
        /// <returns>Compressed Data</returns>
        public static byte[] Implode(byte[] Buffer) {
            byte[] Buf = new byte[Buffer.Length];
            int Result = Implode(Buffer, 0, Buffer.Length, Buf, 0, Buffer.Length);
            Array.Resize(ref Buffer, Result);
            return Buf;
        }

        /// <summary>Pkware Implode</summary>
        /// <param name="bIn">Input Buffer</param>
        /// <param name="pIn">Input Offset</param>
        /// <param name="cIn">Input Size</param>
        /// <param name="bOut">Output Buffer</param>
        /// <param name="pOut">Output Offset</param>
        /// <param name="cOut">Output Size</param>
        /// <returns>Decompressed Size</returns>
        public static int Implode(byte[] bIn, int pIn, int cIn, byte[] bOut, int pOut, int cOut) {
            if(cIn == 0) return 0;
            var sIn = new FixedStream(bIn, pIn, cIn);
            var sOut = new FixedStream(bOut, pOut, cOut);
            if(Compress_Implode(sIn, sOut, 0) == -1) {
                if(cOut < cIn) return -1;
                Array.Copy(bIn, pIn, bOut, pOut, cIn);
                return cIn;
            }
            return sOut.Position;
        }

        /// <summary>Pkware Explode</summary>
        /// <param name="Buffer">Input Data</param>
        /// <param name="Result">Decompressed Size</param>
        /// <returns>Decompressed Data</returns>
        public static byte[] Explode(byte[] Buffer, int Result) {
            byte[] Buf = new byte[Result];
            Explode(Buffer, 0, Buffer.Length, Buf, 0, Result);
            return Buf;
        }

        /// <summary>Pkware Explode</summary>
        /// <param name="bIn">Input Buffer</param>
        /// <param name="pIn">Input Offset</param>
        /// <param name="cIn">Input Size</param>
        /// <param name="bOut">Output Buffer</param>
        /// <param name="pOut">Output Offset</param>
        /// <param name="cOut">Output Size</param>
        /// <returns>Decompressed Size</returns>
        public static int Explode(byte[] bIn, int pIn, int cIn, byte[] bOut, int pOut, int cOut) {
            if(cIn == 0) return 0;
            if(cIn == cOut) goto Failed;
            var sIn = new FixedStream(bIn, pIn, cIn);
            var sOut = new FixedStream(bOut, pOut, cOut);
            if(Decompress_Implode(sIn, sOut) == -1) goto Failed;
            return sOut.Position;
            Failed:;
            Array.Copy(bIn, pIn, bOut, pOut, cIn);
            return cIn;
        }

        //Swap Varable
        private static void Swap<VarType>(ref VarType x, ref VarType y) {
            VarType z;
            z = x;
            x = y;
            y = z;
        }
        
        private static byte[] CopyArray(byte[] Arr, int Offset, int Count) {
            byte[] Result = new byte[Count];
            Array.Copy(Arr, Offset, Result, 0, Count);
            return Result;
        }
        
        private static int Compress_LZMA(byte[] bIn, int pIn, int cIn, byte[] bOut, int pOut, int cOut) {
            int Read;
            using(MemoryStream
                sIn = new MemoryStream(bIn, pIn, cIn),
                sOut = new MemoryStream(bOut, pOut + 1, cOut - 1)) {
                if((Read = LZMA.Compress(sIn, sOut)) == -1) {
                    if(cOut < cIn) return -1;
                    Array.Copy(bIn, pIn, bOut, pOut, cIn);
                    return cIn;
                }
            }
            bOut[pOut] = (byte)Compressions.LZMA;
            return Read;
        }

        private static int Decompress_LZMA(byte[] bIn, int pIn, int cIn, byte[] bOut, int pOut, int cOut) {
            int Read;
            using(MemoryStream
                sIn = new MemoryStream(bIn, pIn + 1, cIn - 1),
                sOut = new MemoryStream(bOut, pOut, cOut)) {
                if((Read = LZMA.Decompress(sIn, sOut)) == -1) {
                    Array.Copy(bIn, pIn, bOut, pOut, cIn);
                    return cIn;
                }
            }
            return Read;
        }

        private static int Compress_Huffman(FixedStream sIn, FixedStream sOut, WaveLevel Wave)
            => Huffman.Compress(sIn, sOut, GetHuffmanType((int)Wave));

        private static int Decompress_Huffman(FixedStream sIn, FixedStream sOut)
            => Huffman.Decompress(sIn, sOut);

        private static int Compress_Deflate(FixedStream sIn, FixedStream sOut, WaveLevel Wave)
            => zlib.Compress(sIn, sOut);

        private static int Decompress_Deflate(FixedStream sIn, FixedStream sOut)
            => zlib.Decompress(sIn, sOut);

        private static int Compress_Implode(FixedStream sIn, FixedStream sOut, WaveLevel Wave)
            => PkLib.Compress(sIn, sOut, dSize: GetImplodeSize(sIn.Length));

        private static int Decompress_Implode(FixedStream sIn, FixedStream sOut)
            => PkLib.Decompress(sIn, sOut);

        private static int Compress_BZip2(FixedStream sIn, FixedStream sOut, WaveLevel Wave)
            => BZip2.Compress(sIn, sOut);

        private static int Decompress_BZip2(FixedStream sIn, FixedStream sOut)
            => BZip2.Decompress(sIn, sOut);

        private static int Compress_Sparse(FixedStream sIn, FixedStream sOut, WaveLevel Wave)
            => Sparse.Compress(sIn, sOut);

        private static int Decompress_Sparse(FixedStream sIn, FixedStream sOut)
            => Sparse.Decompress(sIn, sOut);

        private static int Compress_ADPCM_Mono(FixedStream sIn, FixedStream sOut, WaveLevel Wave)
            => ADPCM.Compress(sIn, sOut, 1, GetADPCMLevel((int)Wave));

        private static int Decompress_ADPCM_Mono(FixedStream sIn, FixedStream sOut)
            => ADPCM.Decompress(sIn, sOut, 1);

        private static int Compress_ADPCM_Stereo(FixedStream sIn, FixedStream sOut, WaveLevel Wave)
            => ADPCM.Compress(sIn, sOut, 2, GetADPCMLevel((int)Wave));

        private static int Decompress_ADPCM_Stereo(FixedStream sIn, FixedStream sOut)
            => ADPCM.Decompress(sIn, sOut, 2);

        private static DictionarySize GetImplodeSize(int InputSize) {
            if(InputSize < 0x600)
                return DictionarySize.Size1;
            else if(InputSize < 0xC00)
                return DictionarySize.Size2;
            else
                return DictionarySize.Size3;
        }

        private static int GetADPCMLevel(int WaveLevel) {
            if(0 < WaveLevel && WaveLevel <= 2)
                return 4;
            if(WaveLevel == 3)
                return 6;
            return 5;
        }

        private static int GetHuffmanType(int WaveLevel) {
            if(WaveLevel == 0) return 0;
            if(0 < WaveLevel && WaveLevel <= 2)
                return 6;
            if(WaveLevel == 3)
                return 8;
            return 7;
        }
    }
}

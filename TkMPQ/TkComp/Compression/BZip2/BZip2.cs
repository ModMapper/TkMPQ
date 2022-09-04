using TkLib;

namespace TkCompLib.Compression
{
	using SharpCompress.Compressors;
	using SharpCompress.Compressors.BZip2;

    /// <summary>BZip2 Data Compression</summary>
    public static class BZip2
    {
        private const int BufferSize = 4096;

        /// <summary>BZip2 Data Compress</summary>
        /// <param name="bIn">Input Buffer</param>
        /// <param name="bOut">Output Buffer</param>
        /// <param name="BlockSize">Compress Block Size</param>
        /// <returns>Output Size (Failed : -1)</returns>
        public static int Compress(byte[] bIn, byte[] bOut, int BlockSize = 9) {
            FixedStream sIn = new FixedStream(bIn), sOut = new FixedStream(bOut);
            return Compress(sIn, sOut, BlockSize);
        }

        /// <summary>BZip2 Data Decompress</summary>
        /// <param name="bIn">Input Buffer</param>
        /// <param name="bOut">Output Buffer</param>
        /// <returns>Output Size (Failed : -1)</returns>
        public static int Decompress(byte[] bIn, byte[] bOut) {
            FixedStream sIn = new FixedStream(bIn), sOut = new FixedStream(bOut);
            return Decompress(sIn, sOut);
        }

        internal static int Compress(FixedStream sIn, FixedStream sOut, int BlockSize = 9) {
            using(var Warp = new FixedWarpper(sOut))
            using(var Stream = new BZip2Stream(Warp, CompressionMode.Compress, false)) {
                byte[] Buffer = new byte[BufferSize];
                int Read;
                while(0 < (Read = sIn.Read(Buffer, 0, BufferSize))) {
                    Stream.Write(Buffer, 0, Read);
                    if(sOut.EndOfStream) return -1;
                }
                Stream.Flush();
            }
            return sOut.EndOfStream ? -1 : sOut.Position;
        }

        internal static int Decompress(FixedStream sIn, FixedStream sOut) {
            using(var Warp = new FixedWarpper(sIn))
            using(var Stream = new BZip2Stream(Warp, CompressionMode.Decompress, false)) {
                byte[] Buffer = new byte[BufferSize];
                int Read;
                while(0 < (Read = Stream.Read(Buffer, 0, BufferSize)))
                    if(sOut.Write(Buffer, 0, Read)) return -1;
            }
            return sOut.Position;
        }
    }
}

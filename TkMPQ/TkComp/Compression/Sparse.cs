using TkLib;

namespace TkCompLib.Compression
{
    /// <summary>Sparse MPQ Comprssion</summary>
    public static class Sparse
    {
        /// <summary>Sparse Data Compress</summary>
        /// <param name="bIn">Input Buffer</param>
        /// <param name="bOut">Output Buffer</param>
        /// <returns>Output Size (Failed : -1)</returns>
        public static int Compress(byte[] bIn, byte[] bOut) {
            FixedStream sIn = new FixedStream(bIn), sOut = new FixedStream(bOut);
            return Compress(sIn, sOut);
        }

        /// <summary>Sparse Data Decompress</summary>
        /// <param name="bIn">Input Buffer</param>
        /// <param name="bOut">Output Buffer</param>
        /// <returns>Output Size (Failed : -1)</returns>
        public static int Decompress(byte[] bIn, byte[] bOut) {
            FixedStream sIn = new FixedStream(bIn), sOut = new FixedStream(bOut);
            return Decompress(sIn, sOut);
        }

        internal static int Compress(FixedStream sIn, FixedStream sOut) {
            byte[] Buf = new byte[0x100], Read = new byte[3];
            int cSize = 0, zSize;
            if(sOut.Write32(sIn.Length)) return -1;
            int Buffered = sIn.Read(Read, 0, 3);
            if(Buffered == 0) return sOut.Position;
            do {
                if(Buffered == 3 &&
                    Read[0] == 0 && Read[1] == 0 && Read[2] == 0) {
                    if(0 < cSize) {
                        if(sOut.Write((byte)((cSize - 1) | 0x80))) return -1;
                        if(sOut.Write(Buf, 0, cSize)) return -1;
                        cSize = 0;
                    }
                    zSize = 1;
                    while(!ReadNext(sIn, Read, ref Buffered) && Read[0] == 0)
                        if(++zSize == 0x82) break;
                    if(sOut.Write((byte)(zSize - 3))) return -1;
                    if(Read[0] == 0) continue;
                }
                Buf[cSize++] = Read[0];
                if(cSize == 0x80) {
                    if(sOut.Write((byte)((cSize - 1) | 0x80))) return -1;
                    if(sOut.Write(Buf, 0, cSize)) return -1;
                    cSize = 0;
                }
            } while(!ReadNext(sIn, Read, ref Buffered));
            if(0 < cSize) {
                if(sOut.Write((byte)((cSize - 1) | 0x80))) return -1;
                if(sOut.Write(Buf, 0, cSize)) return -1;
            }
            return sOut.Position;
        }

        /// <summary>Sparse Data Decompress</summary>
        /// <param name="bIn">Input Buffer</param>
        /// <param name="bOut">Output Buffer</param>
        /// <returns>Output Size (Failed : -1)</returns>
        internal static int Decompress(FixedStream sIn, FixedStream sOut) {
            byte[] Buf = new byte[0x100];
            byte Data;
            int cSize;
            if(sIn.Skip(4) < 4) return 0;    //Uncompressed Size

            while(!sIn.Read(out Data)) {
                if((Data & 0x80) != 0) {
                    cSize = ((Data & 0x7F) + 1);
                    cSize = sIn.Read(Buf, 0, cSize);
                    if(sOut.Write(Buf, 0, cSize)) return -1;
                } else {
                    cSize = (byte)((Data & 0x7F) + 3);
                    for(int i = 0; i < cSize; i++)
                        if(sOut.Write(0)) return -1;
                }
            }
            return sOut.Position;
        }

        private static bool ReadNext(FixedStream Stream, byte[] Buffer, ref int Count) {
            Buffer[0] = Buffer[1];
            Buffer[1] = Buffer[2];
            if(Count < 3 || Stream.Read(out Buffer[2])) Count--;
            return Count == 0;
        }
    }
}

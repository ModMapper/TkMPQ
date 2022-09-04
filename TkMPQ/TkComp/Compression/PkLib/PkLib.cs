using System.IO;
using TkLib;

namespace TkCompLib.Compression
{
    using Pkware;

    /// <summary>Pkware Data Compression</summary>
    public class PkLib
    {
        /// <summary>Implode Data Compress</summary>
        /// <param name="bIn">Input Buffer</param>
        /// <param name="bOut">Output Buffer</param>
        /// <param name="cType">Compress Type</param>
        /// <param name="dSize">Dictionary Size</param>
        /// <returns>Output Size (Failed : -1)</returns>
        public static int Compress(byte[] bIn, byte[] bOut, CompressType cType = CompressType.Binary, DictionarySize dSize = DictionarySize.Size3) {
            FixedStream sIn = new FixedStream(bIn), sOut = new FixedStream(bOut);
            return Compress(sIn, sOut, cType, dSize);
        }

        /// <summary>Explode Data Decompress</summary>
        /// <param name="bIn">Input Buffer</param>
        /// <param name="bOut">Output Buffer</param>
        /// <returns>Output Size (Failed : -1)</returns>
        public static int Decompress(byte[] bIn, byte[] bOut) {
            FixedStream sIn = new FixedStream(bIn), sOut = new FixedStream(bOut);
            return Decompress(sIn, sOut);
        }


        internal static int Compress(FixedStream sIn, FixedStream sOut, CompressType cType = CompressType.Binary, DictionarySize dSize = DictionarySize.Size3) {
            Implode Imp = new Implode(sIn, sOut, cType, dSize);
            return Imp.Compress();
        }

        internal static int Decompress(FixedStream sIn, FixedStream sOut) {
            Explode Exp = new Explode(sIn, sOut);
            return Exp.Decompress();
        }
    }

    namespace Pkware
    {
        /// <summary>Implode Compress Type</summary>
        public enum CompressType : uint {
            Binary = 0,
            Ascii = 1
        }

        /// <summary>Implode Dictionary Size</summary>
        public enum DictionarySize : uint {
            Size3 = 0x1000,
            Size2 = 0x800,
            Size1 = 0x400
        }
    }
}

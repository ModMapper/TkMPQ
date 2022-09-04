using TkLib;

namespace TkCompLib.Compression
{
    /// <summary>Huffman MPQ Compression</summary>
    public static class Huffman
    {
        /// <summary>Huffman Data Compress</summary>
        /// <param name="bIn">Input Buffer</param>
        /// <param name="bOut">Output Buffer</param>
        /// <param name="CompressionType">Compression Type</param>
        /// <returns>Output Size (Failed : -1)</returns>
        public static int Compress(byte[] bIn, byte[] bOut, int CompressionType) {
            FixedStream sIn = new FixedStream(bIn), sOut = new FixedStream(bOut);
            return Compress(sIn, sOut, CompressionType);
        }

        /// <summary>Huffman Data Compress</summary>
        /// <param name="bIn">Input Buffer</param>
        /// <param name="bOut">Output Buffer</param>
        /// <returns>Output Size (Failed : -1)</returns>
        public static int Decompress(byte[] bIn, byte[] bOut) {
            FixedStream sIn = new FixedStream(bIn), sOut = new FixedStream(bOut);
            return Decompress(sIn, sOut);
        }

        internal static int Compress(FixedStream sIn, FixedStream sOut, int CompressionType) {
            var Tree = new HuffmanTree.HuffmanTree(sIn, sOut);
            return Tree.Compress(CompressionType);
        }

        internal static int Decompress(FixedStream sIn, FixedStream sOut) {
            var Tree = new HuffmanTree.HuffmanTree(sIn, sOut);
            return Tree.Decompress();
        }
    }
}
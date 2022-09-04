using System.IO;
using System;
using TkLib;

namespace TkCompLib.Compression
{
    using SevenZip.Compression.LZMA;
    
    /// <summary>LZMA Data Compression</summary>
    public static class LZMA
    {
        private const int LZMA_HEADER_SIZE = 1 + LZMA_PROPS_SIZE + 8;
        private const int LZMA_PROPS_SIZE = 5;
        /*
            LZMA Header
            1 Byte Filter
            5 Byte Props
            8 Byte Size
        */

        /// <summary>LZMA Data Compress</summary>
        /// <param name="bIn">Input Buffer</param>
        /// <param name="bOut">Output Buffer</param>
        /// <returns>Output Size (Failed : -1)</returns>
        public static int Compress(byte[] bIn, byte[] bOut) {
            if(bOut.Length < LZMA_HEADER_SIZE) return -1;
            using(MemoryStream
                sIn = new MemoryStream(bIn),
                sOut = new MemoryStream(bOut)) {
                Encoder coder = new Encoder();

                sOut.WriteByte(0);
                coder.WriteCoderProperties(sOut);
                sOut.Write(BitConverter.GetBytes(sIn.Length), 0, 8);
                coder.Code(sIn, sOut, sIn.Length, sOut.Length - LZMA_HEADER_SIZE, null);
                return (int)sOut.Position;
            }
        }

        /// <summary>LZMA Data Decompress</summary>
        /// <param name="bIn">Input Buffer</param>
        /// <param name="bOut">Output Buffer</param>
        /// <returns>Output Size (Failed : -1)</returns>
        public static int Decompress(byte[] bIn, byte[] bOut) {
            using(MemoryStream
                sIn = new MemoryStream(bIn),
                sOut = new MemoryStream(bOut)) {
                byte[] Properties = new byte[LZMA_PROPS_SIZE];
                byte[] Header = new byte[LZMA_HEADER_SIZE];
                Decoder coder = new Decoder();

                if(sIn.Read(Header, 0, LZMA_HEADER_SIZE) < LZMA_HEADER_SIZE) return 0;
                if(Header[0] != 0) return -1;  //non filter used
                Array.Copy(Header, 1, Properties, 0, 5);
                //BitConverter.ToInt64(Header, 6);    //Size

                coder.SetDecoderProperties(Properties);
                coder.Code(sIn, sOut, sIn.Length, bOut.Length, null);
                return (int)sOut.Position;
            }
        }

        internal static int Compress(Stream sIn, Stream sOut) {
            if(sOut.Length < LZMA_HEADER_SIZE) return -1;
            Encoder coder = new Encoder();

            sOut.WriteByte(0);
            coder.WriteCoderProperties(sOut);
            sOut.Write(BitConverter.GetBytes(sIn.Length), 0, 8);
            coder.Code(sIn, sOut, sIn.Length, sOut.Length - LZMA_HEADER_SIZE, null);
            return (int)sOut.Position;
        }

        internal static int Decompress(Stream sIn, Stream sOut) {
            byte[] Properties = new byte[LZMA_PROPS_SIZE];
            byte[] Header = new byte[LZMA_HEADER_SIZE];
            Decoder coder = new Decoder();

            if(sIn.Read(Header, 0, LZMA_HEADER_SIZE) < LZMA_HEADER_SIZE) return 0;
            if(Header[0] != 0) return -1;  //non filter used
            Array.Copy(Header, 1, Properties, 0, 5);
            //BitConverter.ToInt64(Header, 6); //Size

            coder.SetDecoderProperties(Properties);
            coder.Code(sIn, sOut, sIn.Length - LZMA_HEADER_SIZE, sOut.Length, null);
            return (int)sOut.Position;
        }
    }
}

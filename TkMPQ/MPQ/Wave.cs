using System.Text;
using System.IO;
using System;

namespace TkMPQLib.MPQ
{
    /// <summary>MPQ의 Wave작성에 사용되는 클래스입니다.</summary>
    public unsafe static class Wave
    {
        private static Encoding Encoder = Encoding.ASCII;

        /// <summary>Wave의 재생 데이터입니다.</summary>
        public struct WaveData {
            public Format Format;
            public byte[] Data;
        }

        /// <summary>Wave의 포멧 헤더입니다.</summary>
        public struct Format {
            public UInt16 Compression;
            public UInt16 Channels;
            public Int32 SampleRate;
            public Int32 BytePerSecond;
            public UInt16 BytePerSample;
            public UInt16 BitsPerSample;
        }

        /// <summary>ADPCM 압축이 가능한 WAVE인지 반환합니다.</summary>
        /// <param name="Format">Wave의 포멧 헤더입니다.</param>
        /// <returns>ADPCM 압축 가능 여부입니다.</returns>
        public static bool CheckCompress(Format Format) {
            if(Format.Compression != 1) return false;   //PCM
            if(Format.Channels == 0) return false;
            if(2 < Format.Channels) return false;       //Mono or Stereo
            return Format.BitsPerSample == 16;          //16 Bit
        }

        /// <summary>해당 Wave에 사용되는 ADPCM 압축을 가져옵니다.</summary>
        /// <param name="Format">Wave의 포멧 헤더입니다.</param>
        /// <returns>ADPCM 압축 방식입니다.</returns>
        public static Compressions GetCompression(Format Format) {
            if(!CheckCompress(Format)) return Compressions.None;
            return Format.Channels == 1 ?  Compressions.ADPCM_Mono : Compressions.ADPCM_Stereo;
        }

        /// <summary>MPQWriter에 Wave의 내용을 작성합니다.</summary>
        /// <param name="Writer">Wave의 내용을 작성할 Writer입니다.</param>
        /// <param name="Stream">작성할 Wave 스트림입니다.</param>
        /// <exception cref="InvalidDataException">올바른 Wave 파일이 아닐때 Throw되는 오류입니다.</exception>
        public static void WriteWave(MPQWriter Writer, Stream Stream) {
            WaveData Wave = new WaveData();
            if(!GetWave(Stream, ref Wave))
                throw new InvalidDataException();
            WriteWave(Writer, Wave);
        }

        /// <summary>MPQWriter에 Wave의 내용을 작성합니다.</summary>
        /// <param name="Writer">Wave의 내용을 작성할 Writer입니다.</param>
        /// <param name="Wave">작성할 Wave입니다.</param>
        public static void WriteWave(MPQWriter Writer, WaveData Wave) {
            int SectorSize = Writer.Data.SectorSize;
            Compressions Compression = GetCompression(Wave.Format);
            byte[] Head = Compression == Compressions.None ?
                CreateHeader(Wave.Format, Wave.Data.Length) :
                CreateHeader(Wave.Format, Wave.Data.Length, SectorSize);
            Writer.Write(Head, 0, Head.Length);
            Writer.Compression |= Compression;
            Writer.Write(Wave.Data, 0 , Wave.Data.Length);
        }

        /// <summary>스트림에서 WaveData를 가져옵니다.</summary>
        /// <param name="Stream">WaveData를 가져올 스트림입니다.</param>
        /// <param name="Wave">가져온 WaveData입니다.</param>
        /// <returns>Wave를 가져오는데 성공했는지 결과입니다.</returns>
        public unsafe static bool GetWave(Stream Stream, ref WaveData Wave) {
            using(var Reader = new BinaryReader(Stream, Encoder)) {
                bool FFormat = false, FData = false;
                if(new string(Reader.ReadChars(4)) != "RIFF") return false;
                int FileSize = Reader.ReadInt32() + 8; //RIFF Size
                if(new string(Reader.ReadChars(4)) != "WAVE") return false;
                while(Stream.Position < FileSize && !(FFormat && FData)) {
                    string Name = new string(Reader.ReadChars(4));
                    int Size = Reader.ReadInt32();
                    if(Name == "fmt " && !FFormat) {
                        byte[] Data = Reader.ReadBytes(Size);
                        if(Data.Length < sizeof(Format)) return false;
                        fixed (byte* pData = Data) Wave.Format = *(Format*)pData;
                        FFormat = true;
                    } else if((Name == "data" || Name == "wavl") && !FData) {
                        Wave.Data = Reader.ReadBytes(Size);
                        FData = true;
                    } else
                        Stream.Seek(Size, SeekOrigin.Current);
                }
            }
            return true;
        }
        
        private static byte[] CreateHeader(Format Format, int Size) {
            byte[] Buf = new byte[44];
            WriteHeader(Buf, Format, 0x10, Size);
            Array.Copy(Encoder.GetBytes("data"), 0, Buf, 36, 4);    //data
            Array.Copy(BitConverter.GetBytes(Size), 0, Buf, 40, 4); //Data Size
            return Buf;
        }

        private static byte[] CreateHeader(Format Format, int Size, int SectorSize) {
            byte[] Buf = new byte[SectorSize];
            WriteHeader(Buf, Format, SectorSize - 28, Size);
            Array.Copy(Encoder.GetBytes("data"), 0, Buf, SectorSize - 8, 4);    //data
            Array.Copy(BitConverter.GetBytes(Size), 0, Buf, SectorSize - 4, 4); //Data Size
            return Buf;
        }

        private static void WriteHeader(byte[] Buffer, Format Format, int FormatSize, int Size) {
            fixed (byte* pBuffer = Buffer) *(Format*)(pBuffer + 20) = Format;
            Array.Copy(Encoder.GetBytes("RIFF"), Buffer, 4);                            //RIFF
            Array.Copy(BitConverter.GetBytes(Size + Buffer.Length), 0, Buffer, 4, 4);   //Size
            Array.Copy(Encoder.GetBytes("WAVEfmt "), 0, Buffer, 8, 8);                  //WAVE + fmt
            Array.Copy(BitConverter.GetBytes(FormatSize), 0, Buffer, 16, 4);            //Header Size
        }
    }
}

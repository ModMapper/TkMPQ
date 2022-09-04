using System.Collections.Generic;
using System.IO;
using System;
using TkCompLib;
    
namespace TkMPQLib.MPQ.Data
{
    using DataTypes;

    #region "Implode"
    /// <summary>Implode된 파일을 읽거나 쓰는 MPQ 파일입니다.</summary>
    public class Implode : Sector
    {
        /// <summary>새로운 Implode된 MPQ 파일을 생성합니다.</summary>
        /// <param name="FilePath">파일의 경로입니다.</param>
        /// <param name="Flags">파일의 속성입니다.</param>
        /// <param name="Size">섹터의 크기입니다.</param>
        public Implode(string FilePath, FileFlags Flags, UInt16 Size)
            : base(FilePath, Flags, Size) { }

        /// <summary>스트림에서 Implode된 MPQ 파일을 읽어들입니다.</summary>
        /// <param name="Stream">읽어들일 스트림입니다.</param>
        /// <param name="pHeader">헤더의 위치입니다.</param>
        /// <param name="Size">섹터의 크기입니다.</param>
        /// <param name="Block">파일의 블록입니다.</param>
        /// <param name="FilePath">파일의 경로입니다.</param>
        public Implode(Stream Stream, long pHeader, UInt16 Size, Block Block, string FilePath = null)
            : base(Stream, pHeader, Size, Block, FilePath) { }

        protected override void ReadIndex(int Index, byte[] buffer, int offset, int count) {
            byte[] Buf = Sectors[Index];
            TkComp.Explode(Buf, 0, Buf.Length, buffer, offset, count);
        }

        protected override void WriteIndex(int Index, byte[] buffer, int offset, int count) {
            byte[] Buf = new byte[count];
            int Size = TkComp.Implode(buffer, offset, count, Buf, 0, count);
            Array.Resize(ref Buf, Size);
            Sectors[Index] = Buf;
        }

        /// <summary>사용된 모든 압축 유형을 가져옵니다. (Implode 반환)</summary>
        /// <returns>압축 유형의 열거입니다.</returns>
        public override IEnumerable<Compressions> GetCompressions() {
            for(int i = 0; i < Sectors.Count; i++)
                yield return Compressions.Implode;
        }

        /// <summary>현재 압축 방식을 가져오거나 설정합니다. (Implode 반환)</summary>
        public override Compressions Compression {
            get => Compressions.Implode;
            set { }
        }

        /// <summary>웨이브 압축 레벨을 가져옵니다. (0 반환)</summary>
        public override WaveLevel WaveLevel {
            get => 0;
            set { }
        }
    }
    #endregion

    #region "Compress"
    /// <summary>압축된 파일을 읽거나 쓰는 MPQ 파일입니다.</summary>
    public class Compress : Sector {
        private Compressions _Compression;
        private WaveLevel _WaveLevel;

        /// <summary>새로운 압축된 MPQ 파일을 생성합니다.</summary>
        /// <param name="FilePath">파일의 경로입니다.</param>
        /// <param name="Flags">파일의 속성입니다.</param>
        /// <param name="Size">섹터의 크기입니다.</param>
        public Compress(string FilePath, FileFlags Flags, UInt16 Size)
            : base(FilePath, Flags, Size) {
            Compression = Compressions.Implode;
        }

        /// <summary>스트림에서 압축된 MPQ 파일을 읽어들입니다.</summary>
        /// <param name="Stream">읽어들일 스트림입니다.</param>
        /// <param name="pHeader">헤더의 위치입니다.</param>
        /// <param name="Size">섹터의 크기입니다.</param>
        /// <param name="Block">파일의 블록입니다.</param>
        /// <param name="FilePath">파일의 경로입니다.</param>
        public Compress(Stream Stream, long pHeader, UInt16 Size, Block Block, string FilePath = null)
            : base(Stream, pHeader, Size, Block, FilePath) {}

        protected override void ReadIndex(int Index, byte[] buffer, int offset, int count) {
            byte[] Buf = Sectors[Index];
            TkComp.Decompress(Buf, 0, Buf.Length, buffer, offset, count);
        }

        protected override void WriteIndex(int Index, byte[] buffer, int offset, int count) {
            byte[] Buf = new byte[count];
            Compressions Comp = Compression;
            if(Index == 0) Comp = Comp & ~(Compressions.ADPCM_Mono | Compressions.ADPCM_Stereo);
            int Size = TkComp.Compress(buffer, offset, count, Buf, 0, count, Comp, WaveLevel);
            Array.Resize(ref Buf, Size);
            Sectors[Index] = Buf;
        }

        /// <summary>사용된 모든 압축 유형을 가져옵니다.</summary>
        /// <returns>압축 유형의 열거입니다.</returns>
        public override IEnumerable<Compressions> GetCompressions() {
            foreach(var Sector in Sectors)
                yield return Sector.Length == 0 ?
                    Compressions.None : (Compressions)Sector[0];
        }

        /// <summary>현재 압축 방식을 가져오거나 설정합니다.</summary>
        public override Compressions Compression {
            get => _Compression;
            set => _Compression = value;
        }

        /// <summary>웨이브 압축 레벨을 가저오거나 설정합니다.</summary>
        public override WaveLevel WaveLevel {
            get => _WaveLevel;
            set => _WaveLevel = value;
        }
    }
#endregion
}

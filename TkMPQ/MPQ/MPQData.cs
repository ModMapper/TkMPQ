using System.Collections.Generic;
using System.IO;
using System;

namespace TkMPQLib.MPQ
{
    using static Encryption;
    using DataTypes;
    using Data;

    /// <summary>MPQ 파일의 내용을 관리하는 클래스입니다.</summary>
    public abstract class MPQData : IDisposable
    {
        /// <summary>MPQ의 섹터 크기입니다.</summary>
        public readonly int SectorSize;
        protected internal bool Encrypted;
        protected internal bool ModKey;
        protected bool CanEncrypt;
        protected bool Opened;
        protected string Path;
        protected uint Key;
        
        public MPQData(string FilePath, FileFlags Flags, UInt16 Size) {
            Encrypted = Flags.HasFlag(FileFlags.Encrypted);
            ModKey = Flags.HasFlag(FileFlags.ModKey);
            SectorSize = 0x200 << Size;
            Path = FilePath;
        }

        /// <summary>새로운 MPQ 파일을 생성합니다.</summary>
        /// <param name="FilePath">파일의 경로입니다.</param>
        /// <param name="Flags">파일의 속성입니다.</param>
        /// <param name="Size">섹터의 크기입니다.</param>
        /// <returns>생성된 MPQ 파일입니다.</returns>
        public static MPQData Create(string FilePath, FileFlags Flags, UInt16 Size) {
            if(Flags.HasFlag(FileFlags.Imploded))
                return new Implode(FilePath, Flags, Size);
            if(Flags.HasFlag(FileFlags.Compressed))
                return new Compress(FilePath, Flags, Size);
            return new Raw(FilePath, Flags, Size);
        }

        /// <summary>스트림에서 MPQ 파일을 읽어들입니다.</summary>
        /// <param name="Stream">읽어들일 스트림입니다.</param>
        /// <param name="pHeader">헤더의 위치입니다.</param>
        /// <param name="Size">섹터의 크기입니다.</param>
        /// <param name="Block">파일의 블록입니다.</param>
        /// <param name="FilePath">파일의 경로입니다.</param>
        /// <returns>읽어들인 MPQ 파일입니다.</returns>
        public static MPQData Create(Stream Stream, long pHeader, UInt16 Size, Block Block, string FilePath = null) {
            if(Block.Flags.HasFlag(FileFlags.Imploded))
                return new Implode(Stream, pHeader, Size, Block, FilePath);
            if(Block.Flags.HasFlag(FileFlags.Compressed))
                return new Compress(Stream, pHeader, Size, Block, FilePath);
            return new Raw(Stream, pHeader, Size, Block, FilePath);
        }
        
        /// <summary>모든 리소스를 해제합니다.</summary>
        public abstract void Dispose();

        /// <summary>새로운 파일 경로를 설정합니다.</summary>
        /// <param name="MPQPath">설정할 새로운 경로입니다.</param>
        public void SetPath(string MPQPath) {
            Path = MPQPath;
            GetKey();
        }

        protected abstract bool ReadData(Stream Stream, long pHeader, int FileOffset, int FileSize);

        /// <summary>파일의 내용을 스트림에 작성합니다.</summary>
        /// <param name="Stream">내용을 작성할 스트림입니다.</param>
        /// <param name="pHeader">헤더의 위치입니다.</param>
        /// <param name="FileOffset">작성할 파일의 경로입니다.</param>
        public abstract int WriteData(Stream Stream, long pHeader, ref int FileOffset);

        /// <summary>파일의 크기를 재설정합니다.</summary>
        /// <param name="Length">재설정할 크기입니다.</param>
        public abstract void SetLength(int Length);

        /// <summary>해당 위치에 데이터를 읽어들입니다.</summary>
        /// <param name="Position">데이터를 읽어들일 위치입니다.</param>
        /// <param name="buffer">읽어들일 데이터 버퍼입니다.</param>
        /// <param name="offset">데이터 버퍼의 오프셋입니다.</param>
        /// <param name="count">데이터의 크기입니다.</param>
        /// <param name="count">읽어들인 데이터의 크기입니다.</param>
        public abstract int Read(int Position, byte[] buffer, int offset, int count);

        /// <summary>해당 위치에 데이터를 작성합니다.</summary>
        /// <param name="Position">데이터를 작성할 위치입니다.</param>
        /// <param name="buffer">작성할 데이터 버퍼입니다.</param>
        /// <param name="offset">데이터 버퍼의 오프셋입니다.</param>
        /// <param name="count">데이터의 크기입니다.</param>
        public abstract void Write(int Position, byte[] buffer, int offset, int count);
        
        /// <summary>사용된 모든 압축 유형을 가져옵니다.</summary>
        /// <returns>압축 유형의 열거입니다.</returns>
        public abstract IEnumerable<Compressions> GetCompressions();

        /// <summary>파일의 크기를 가져옵니다.</summary>
        public abstract int FileSize { get; }

        /// <summary>파일의 압축된 크기를 가져옵니다.</summary>
        public abstract int CompSize { get; }

        /// <summary>읽기가 가능한지 반환합니다.</summary>
        public virtual bool CanRead => Opened;

        /// <summary>현재 압축 방식을 가져오거나 설정합니다.</summary>
        public abstract Compressions Compression { get; set; }

        /// <summary>웨이브 압축 레벨을 가저오거나 설정합니다.</summary>
        public abstract WaveLevel WaveLevel { get; set; }

        protected virtual void GetKey() {
            var FileName = Path.Substring(Path.LastIndexOf('\\') + 1);
            Key = HashString(FileName, HashType.FileKey);
            CanEncrypt = true;
        }

        internal protected uint GetCryptKey(int FileOffset, int FileSize)
            => ModKey ? ModKey(Key, FileOffset, FileSize) : Key;
    }
}

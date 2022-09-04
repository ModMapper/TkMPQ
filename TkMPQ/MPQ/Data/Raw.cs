using System.IO;
using System.Collections.Generic;
using System;
using TkCompLib;

namespace TkMPQLib.MPQ.Data
{
    using DataTypes;

    /// <summary>압축되지 않은 파일을 읽거나 쓰는 MPQ 파일입니다.</summary>
    public class Raw : MPQData
    {
        protected MemoryStream Data;

        /// <summary>새로운 압축되지 않은 MPQ 파일을 생성합니다.</summary>
        /// <param name="FilePath">파일의 경로입니다.</param>
        /// <param name="Flags">파일의 속성입니다.</param>
        /// <param name="Size">섹터의 크기입니다.</param>
        public Raw(string FilePath, FileFlags Flags, UInt16 Size) : base(FilePath, Flags, Size) {
            Data = new MemoryStream();
            GetKey();
        }

        /// <summary>스트림에서 압축되지 않은 MPQ 파일을 읽어들입니다.</summary>
        /// <param name="Stream">읽어들일 스트림입니다.</param>
        /// <param name="pHeader">헤더의 위치입니다.</param>
        /// <param name="Size">섹터의 크기입니다.</param>
        /// <param name="Block">파일의 블록입니다.</param>
        /// <param name="FilePath">파일의 경로입니다.</param>
        public Raw(Stream Stream, long pHeader, UInt16 Size, Block Block, string FilePath = null) : base(FilePath, Block.Flags, Size) {
            if(FilePath == null) {
                CanEncrypt = false;
                Encrypted = false;
                Opened = false;
            } else GetKey();
            try {
                Opened |= ReadData(Stream, pHeader, Block.FileOffset, Block.FileSize);
            } catch {
                Opened = false;
            }
        }

        /// <summary>모든 리소소를 해제합니다.</summary>
        public override void Dispose() {
            Data.Dispose();
            Opened = false;
        }

        protected override bool ReadData(Stream Stream, long pHeader, int FileOffset, int FileSize) {
            byte[] Buf = new byte[FileSize];
            Stream.Position = pHeader + FileOffset;
            Stream.Read(Buf, 0, FileSize);
            if(Encrypted) {
                uint sKey = GetCryptKey(FileOffset, FileSize);
                Encryption.DecryptData(Buf, sKey);
            }
            Data = new MemoryStream();
            Data.Write(Buf, 0, FileSize);
            return true;
        }

        /// <summary>파일의 내용을 스트림에 작성합니다.</summary>
        /// <param name="Stream">내용을 작성할 스트림입니다.</param>
        /// <param name="pHeader">헤더의 위치입니다.</param>
        /// <param name="FileOffset">작성할 파일의 경로입니다.</param>
        public override int WriteData(Stream Stream, long pHeader, ref int FileOffset) {
            if(!Opened) throw new InvalidOperationException();
            var Buf = Data.ToArray();
            if(Encrypted) {
                uint sKey = GetCryptKey(FileOffset, FileSize);
                Encryption.EncryptData(Buf, sKey);
            }
            Stream.Position = pHeader + FileOffset;
            Stream.Write(Buf, 0, Buf.Length);
            return Buf.Length;
        }

        /// <summary>파일의 크기를 재설정합니다.</summary>
        /// <param name="Length">재설정할 크기입니다.</param>
        public override void SetLength(int Length) {
            Data.SetLength(Length);
        }

        /// <summary>해당 위치에 데이터를 읽어들입니다.</summary>
        /// <param name="Position">데이터를 읽어들일 위치입니다.</param>
        /// <param name="buffer">읽어들일 데이터 버퍼입니다.</param>
        /// <param name="offset">데이터 버퍼의 오프셋입니다.</param>
        /// <param name="count">데이터의 크기입니다.</param>
        public override int Read(int Position, byte[] buffer, int offset, int count) {
            if(!Opened) throw new InvalidOperationException();
            Data.Position = Position;
            return Data.Read(buffer, offset, count);
        }

        /// <summary>해당 위치에 데이터를 작성합니다.</summary>
        /// <param name="Position">데이터를 작성할 위치입니다.</param>
        /// <param name="buffer">작성할 데이터 버퍼입니다.</param>
        /// <param name="offset">데이터 버퍼의 오프셋입니다.</param>
        /// <param name="count">데이터의 크기입니다.</param>
        public override void Write(int Position, byte[] buffer, int offset, int count) {
            if(!Opened) throw new InvalidOperationException();
            Data.Position = Position;
            Data.Write(buffer, offset, count);
        }

        /// <summary>사용된 모든 압축 유형을 가져옵니다. (None 반환)</summary>
        /// <returns>압축 유형의 열거입니다.</returns>
        public override IEnumerable<Compressions> GetCompressions() {
            yield return Compressions.None;
        }

        /// <summary>파일의 크기를 가져옵니다.</summary>
        public override int FileSize => (int)Data.Length;

        /// <summary>파일의 압축된 크기를 가져옵니다.</summary>
        public override int CompSize => FileSize;

        /// <summary>현재 압축 방식을 가져오거나 설정합니다. (None 반환)</summary>
        public override Compressions Compression {
            get => Compressions.None;
            set { }
        }

        /// <summary>웨이브 압축 레벨을 가져옵니다. (0 반환)</summary>
        public override WaveLevel WaveLevel {
            get => 0;
            set { }
        }
    }
}

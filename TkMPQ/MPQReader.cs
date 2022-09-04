using System.IO;
using System;

namespace TkMPQLib
{
    using MPQ.DataTypes;
    using MPQ;

    /// <summary>MPQ에서 파일을 읽는 스트림입니다.</summary>
    public class MPQReader : Stream
    {
        protected internal Hash Hash;
        protected internal Block Block;
        protected internal MPQData Data;

        protected string Path;
        protected bool RealPath;

        /// <summary>MPQ로부터 MPQ리더를 생성합니다.</summary>
        /// <param name="MPQ">MPQ리더를 생성할 MPQ입니다.</param>
        /// <param name="Index">파일의 해시 인덱스입니다.</param>
        /// <param name="FilePath">파일의 경로입니다.</param>
        public MPQReader(TkMPQ MPQ, int Index, string FilePath = null) {
            Hash = MPQ.HashTable[Index];
            Block = MPQ.BlockTable[Hash.Block];
            MPQ.LockAction(() => Data = MPQData.Create(MPQ.FileData, MPQ.Header, MPQ.SectorSize, Block, FilePath));
            Path = FilePath ?? MPQ.UnknownNamer(Index);
            RealPath = FilePath == null;
        }

        protected override void Dispose(bool disposing)
            => Data.Dispose();

        public static explicit operator MPQData(MPQReader Reader)
            => Reader.Data;

        /// <summary>스트림의 위치에 해당 내용을 읽어들입니다.</summary>
        /// <param name="buffer">읽어들인 내용이 들어갈 버퍼입니다.</param>
        /// <param name="offset">버퍼의 오프셋입니다.</param>
        /// <param name="count">읽어들일 크기입니다.</param>
        /// <returns>읽어들인 크기입니다.</returns>
        public override int Read(byte[] buffer, int offset, int count) {
            if(!CanRead) throw new InvalidDataException();
            int Read = Data.Read((int)Position, buffer, offset, count);
            _Position += Read;
            return Read;
        }

        /// <summary>스트림의 내용을 바이트 배열에 작성합니다.</summary>
        /// <returns>스트림의 바이트 배열 복사본입니다.</returns>
        public byte[] ToArray() {
            if(!CanRead) throw new InvalidDataException();
            byte[] Buf = new byte[FileSize];
            Data.Read(0, Buf, 0, Buf.Length);
            return Buf;
        }

        /// <summary>해당 스트림의 내용을 다른 스트림에 작성합니다.</summary>
        /// <param name="Stream">내용을 작성할 스트림입니다.</param>
        public void WriteTo(Stream Stream) {
            if(!CanRead) throw new InvalidDataException();
            int SectorSize = Data.SectorSize;
            byte[] Buf = new byte[SectorSize];
            int Size = FileSize, Pos = 0;
            while(SectorSize < Size) {
                Data.Read(Pos, Buf, 0, SectorSize);
                Stream.Write(Buf, 0, SectorSize);
                Size -= SectorSize;
                Pos += SectorSize;
            }
            Data.Read(Pos, Buf, 0, Size);
            Stream.Write(Buf, 0, Size);
        }

        /// <summary>MPQ 작성 스트림을 생성합니다.</summary>
        /// <param name="FilePath">생성할 파일 경로입니다.</param>
        /// <returns>MPQ 작성 스트림입니다.</returns>
        public virtual MPQWriter CreateWriter(string FilePath) {
            if(!CanRead) throw new InvalidDataException();
            return new MPQWriter(Data, FilePath, Block.Flags, Locale);
        }

        /// <summary>버퍼링되는 스트림을 생성합니다.</summary>
        /// <returns>해당 스트림의 BufferedStream 입니다.</returns>
        public virtual BufferedStream CreateBuffered()
            => new BufferedStream(this, Data.SectorSize);
        
        #region "MPQ Info"
        /// <summary>파일의 경로를 가져옵니다.</summary>
        public string FileName => Path;

        /// <summary>파일의 크기를 가져옵니다.</summary>
        public int FileSize => Block.FileSize;

        /// <summary>파일의 압축된 크기를 가져옵니다.</summary>
        public int CompSize => Block.CompSize;

        /// <summary>파일의 실제 압축된 크기를 가져옵니다.</summary>
        public int RealSize => Data.CompSize;

        /// <summary>파일의 언어 코드를 가져옵니다.</summary>
        public Locale Locale => Hash.Locale;

        /// <summary>파일의 속성를 가져옵니다.</summary>
        public FileFlags Flags => Block.Flags;

        /// <summary>파일 데이터를 가져옵니다.</summary>
        public MPQData FileData => Data;
        #endregion

        #region "Stream"
        /// <summary>해당 스트림을 탐색이 가능한지 반환합니다. 현재 스트림은 탐색이 가능합니다.</summary>
        public override bool CanSeek => true;
        /// <summary>해당 스트림을 읽기가 가능한지 반환합니다. 현재 스트림은 탐색이 가능합니다.</summary>
        public override bool CanRead => Data.CanRead;
        /// <summary>해당 스트림을 쓰기가 가능한지 반환합니다. 현재 스트림은 탐색이 불가능합니다.</summary>
        public override bool CanWrite => false;
        /// <summary>스트림의 길이를 반환합니다. 파일의 크기와 같습니다.</summary>
        public override long Length => Block.FileSize;

        private int _Position = 0;
        /// <summary>현재 스트림의 위치를 가져오거나 설정합니다.</summary>
        public override long Position { get => _Position; set => _Position =  (int)value; }

        /// <summary>현재 스트림의 위치를 이동합니다.</summary>
        /// <param name="offset">origin 매개 변수에 상대적인 바이트 오프셋입니다.</param>
        /// <param name="origin">새 위치를 가져오는 데 사용되는 참조 위치를 나타내는 SeekOrigin 형식의 값입니다.</param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin) {
            switch(origin) {
            case SeekOrigin.Begin:
                return Position = offset;
            case SeekOrigin.Current:
                return Position += offset;
            case SeekOrigin.End:
                return Position = Length - offset;
            }
            return Position;
        }

        /// <summary>버퍼의 내용을 작성합니다. 사용되지 않습니다.</summary>
        /// <exception cref="NotSupportedException"></exception>
        public override void Flush()
            => throw new NotSupportedException();

        /// <summary>스트림의 크기를 재설정합니다. 사용되지 않습니다.</summary>
        /// <param name="value">재설정할 스트림 크기입니다.</param>
        /// <exception cref="NotSupportedException"></exception>
        public override void SetLength(long value)
            => throw new NotSupportedException();

        /// <summary>스트림의 위치에 해당 내용을 작성합니다. 사용되지 않습니다.</summary>
        /// <param name="buffer">작성할 내용이 들어있는 버퍼입니다.</param>
        /// <param name="offset">버퍼의 오프셋입니다.</param>
        /// <param name="count">작성할 크기입니다.</param>
        /// <exception cref="NotSupportedException"></exception>
        public override void Write(byte[] buffer, int offset, int count)
            => throw new NotSupportedException();
        #endregion
    }
}
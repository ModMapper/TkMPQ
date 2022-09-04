using System.IO;

namespace TkMPQLib
{
    using MPQ.DataTypes;
    using MPQ;

    /// <summary>MPQ 파일을 작성하는 스트림입니다.</summary>
    public class MPQWriter : Stream {
        protected internal MPQData Data;
        protected internal Locale FLocale;
        protected internal FileFlags FFlags;

        protected string Path;

        /// <summary>MPQ에 새로운 파일을 생성합니다.</summary>
        /// <param name="MPQ">새로운 파일을 생성할 MPQ입니다.</param>
        /// <param name="FilePath">파일을 생성할 경로입니다.</param>
        /// <param name="Flags">생성할 파일의 속성입니다.</param>
        /// <param name="Locale">생성할 파일의 언어 코드입니다.</param>
        public MPQWriter(TkMPQ MPQ, string FilePath, FileFlags Flags, Locale Locale = Locale.Neutral) {
            Data = MPQData.Create(FilePath, Flags, MPQ.SectorSize);
            Compression = Compressions.Implode;
            Path = FilePath;
            FLocale = Locale;
            FFlags = Flags | FileFlags.Exists;
        }

        /// <summary>MPQData로 부터 새로운 스트림을 생성합니다.</summary>
        /// <param name="FileData">새로운 스트림을 생성할 MPQData입니다.</param>
        /// <param name="FilePath">파일을 생성할 경로입니다.</param>
        /// <param name="Flags">생성할 파일의 속성입니다.</param>
        /// <param name="Locale">생성할 파일의 언어 코드입니다.</param>
        public MPQWriter(MPQData FileData, string FilePath, FileFlags Flags, Locale Locale = Locale.Neutral) {
            FileData.SetPath(FilePath);
            Data = FileData;
            Path = FilePath;
            FLocale = Locale;
            FFlags = Flags | FileFlags.Exists;
        }

        protected MPQWriter(string FilePath, Locale Locale = Locale.Neutral) {
            Path = FilePath;
            FLocale = Locale;
        }

        protected override void Dispose(bool disposing)
            => Data.Dispose();


        public static explicit operator MPQData(MPQWriter Writer)
            => Writer.Data;

        protected internal virtual Hash GetHash(int BlockIndex) {
            Hash Hash = new Hash();
            Hash.Locale = Locale;
            Hash.BlockIndex = BlockIndex;
            return Hash;
        }

        protected internal virtual Block GetBlock(int FileOffset) {
            Block Block = new Block();
            Block.FileOffset = FileOffset;
            Block.FileSize = FileSize;
            Block.CompSize = CompSize;
            Block.Flags = Flags;
            return Block;
        }

        /// <summary>스트림의 크기를 재설정합니다.</summary>
        /// <param name="value">재설정할 스트림 크기입니다.</param>
        public override void SetLength(long value)
            => Data.SetLength((int)value);

        /// <summary>스트림의 위치에 해당 내용을 읽어들입니다.</summary>
        /// <param name="buffer">읽어들인 내용이 들어갈 버퍼입니다.</param>
        /// <param name="offset">버퍼의 오프셋입니다.</param>
        /// <param name="count">읽어들일 크기입니다.</param>
        /// <returns>읽어들인 크기입니다.</returns>
        public override int Read(byte[] buffer, int offset, int count) {
            int Read = Data.Read((int)Position, buffer, offset, count);
            _Position += Read;
            return Read;
        }

        /// <summary>스트림의 위치에 해당 내용을 작성합니다.</summary>
        /// <param name="buffer">작성할 내용이 들어있는 버퍼입니다.</param>
        /// <param name="offset">버퍼의 오프셋입니다.</param>
        /// <param name="count">작성할 크기입니다.</param>
        public override void Write(byte[] buffer, int offset, int count) {
            Data.Write(_Position, buffer, offset, count);
            _Position += count;
        }

        /// <summary>스트림의 내용을 바이트 배열에 작성합니다.</summary>
        /// <returns>스트림의 바이트 배열 복사본입니다.</returns>
        public byte[] ToArray() {
            byte[] Buf = new byte[FileSize];
            Data.Read(0, Buf, 0, Buf.Length);
            return Buf;
        }

        /// <summary>해당 스트림의 내용을 다른 스트림에 작성합니다.</summary>
        /// <param name="Stream">내용을 작성할 스트림입니다.</param>
        public void WriteTo(Stream Stream) {
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

        /// <summary>버퍼링되는 스트림을 생성합니다.</summary>
        /// <returns>해당 스트림의 BufferedStream 입니다.</returns>
        public virtual BufferedStream CreateBuffered()
            => new BufferedStream(this, Data.SectorSize);

        /// <summary>현재 압축 방식을 가져오거나 설정합니다.</summary>
        public Compressions Compression {
            get => Data.Compression;
            set => Data.Compression = value;
        }

        /// <summary>웨이브 압축 레벨을 가저오거나 설정합니다.</summary>
        public WaveLevel WaveLevel {
            get => Data.WaveLevel;
            set => Data.WaveLevel = value;
        }

        /// <summary>파일의 암호화 속성을 가져오거나 설정합니다.</summary>
        public bool Encrypted {
            get => FFlags.HasFlag(FileFlags.Encrypted);
            set {
                if(value)
                    FFlags |= FileFlags.Encrypted;
                else
                    FFlags &= ~FileFlags.Encrypted;
                Data.Encrypted = value;
            }
        }

        /// <summary>파일의 키 변조 속성을 가져오거나 설정합니다.</summary>
        public bool ModKey {
            get => FFlags.HasFlag(FileFlags.ModKey);
            set {
                if(value)
                    FFlags |= FileFlags.ModKey;
                else
                    FFlags &= ~FileFlags.ModKey;
                Data.ModKey = value;
            }
        }

        #region "MPQ Info"
        /// <summary>파일의 경로를 가져옵니다.</summary>
        public string FileName => Path;

        /// <summary>파일의 크기를 가져옵니다.</summary>
        public int FileSize => Data.FileSize;

        /// <summary>파일의 압축된 크기를 가져옵니다.</summary>
        public int CompSize => Data.CompSize;

        /// <summary>파일의 언어 코드를 가져옵니다.</summary>
        public Locale Locale => FLocale;

        /// <summary>파일의 속성를 가져옵니다.</summary>
        public FileFlags Flags => FFlags;

        /// <summary>파일 데이터를 가져옵니다.</summary>
        public MPQData FileData => Data;
        #endregion

        #region "Stream"
        /// <summary>해당 스트림을 탐색이 가능한지 반환합니다. 현재 스트림은 탐색이 가능합니다.</summary>
        public override bool CanSeek => true;
        /// <summary>해당 스트림을 읽기가 가능한지 반환합니다. 현재 스트림은 탐색이 가능합니다.</summary>
        public override bool CanRead => true;
        /// <summary>해당 스트림을 쓰기가 가능한지 반환합니다. 현재 스트림은 탐색이 가능합니다.</summary>
        public override bool CanWrite => true;
        /// <summary>스트림의 길이를 반환합니다. 파일의 크기와 같습니다.</summary>
        public override long Length => Data.FileSize;

        private int _Position = 0;
        /// <summary>현재 스트림의 위치를 가져오거나 설정합니다.</summary>
        public override long Position { get => _Position; set => _Position = (int)value; }

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

        /// <summary>버퍼의 내용을 작성합니다.</summary>
        public override void Flush() {}
        #endregion
    }
}

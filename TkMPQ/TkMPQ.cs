using System.IO;
using System;

namespace TkMPQLib
{
    using MPQ;

    /// <summary>MPQ를 읽거나 작성하는 클래스입니다.</summary>
    public partial class TkMPQ : IDisposable {
        protected internal Stream FileData;
        protected internal long Header;
        protected internal int Last;

        protected internal HashTable HashTable;
        protected internal BlockTable BlockTable;

        protected internal ushort MPQVersion;
        protected internal ushort MPQSectorSize;

        protected internal bool Created;
        protected object ThreadLock = new object();

        /// <summary>해당 MPQ의 리스트파일 입니다.</summary>
        public readonly Listfiles Listfile;

        /// <summary>새로운 TkMPQ를 생성합니다.</summary>
        public TkMPQ() {
            Listfile = new Listfiles();
            Created = false;
        }
        
        /// <summary>새 비어있는 TkMPQ를 생성합니다.</summary>
        /// <param name="SectorSize">생성할 MPQ의 섹터 크기입니다.</param>
        /// <param name="HashSize">생성할 MPQ의 해시 테이블 크기입니다.</param>
        public TkMPQ(UInt16 SectorSize, Int32 HashSize) : this() {
            CreateMPQ(SectorSize, HashSize);
        }

        /// <summary>파일 경로로부터 MPQ를 읽어들여 새로운 TkMPQ를 생성합니다.</summary>
        /// <param name="FileName">MPQ를 읽어들일 파일 경로입니다.</param>
        public TkMPQ(string FileName) : this() {
            OpenMPQ(FileName);
        }

        /// <summary>스트림으로부터 MPQ를 읽어들여 새로운 TkMPQ를 생성합니다.</summary>
        /// <param name="Stream">MPQ를 읽어들일 스트림입니다.</param>
        public TkMPQ(Stream Stream) : this() {
            OpenMPQ(Stream);
        }

        /// <summary>MPQ의 모든 리소스를 해제합니다.</summary>
        public void Dispose() {
            if(!Created) return;
            lock(ThreadLock) {
                FileData.Dispose();
                Header = 0;
                Last = 0;
                Created = false;
            }
        }

        /// <summary>이름을 찾지 못한 파일을 작명하는 함수입니다.</summary>
        /// <param name="Index">파일의 해시 인덱스입니다.</param>
        /// <returns>작성된 파일의 이름입니다.</returns>
        public virtual string UnknownNamer(int Index)
            => $@"Unknowns\File {Index.ToString("X2")}";

        protected internal void LockAction(Action Func) {
            lock(ThreadLock) Func();
        }

        /// <summary>해당 경로의 파일을 가져옵니다.</summary>
        /// <param name="FilePath">가져올 파일 경로입니다.</param>
        /// <param name="Locale">가져올 파일의 언어 코드입니다.</param>
        /// <returns>해당 파일의 읽기 스트림입니다.</returns>
        public MPQReader this[string FilePath, Locale Locale = Locale.Neutral]
            => GetFile(FilePath, Locale);

        /// <summary>해당 인덱스의 파일을 가져옵니다.</summary>
        /// <param name="Index">가져올 파일의 인덱스입니다.</param>
        /// <param name="FilePath">가져올 파일 경로입니다.</param>
        /// <returns>해당 파일의 읽기 스트림입니다.</returns>
        public MPQReader this[Int32 Index, string FilePath = null]
            => GetFile(Index, FilePath);

        /// <summary>MPQ의 버전을 가져옵니다.</summary>
        public UInt16 Version {
            get {
                if(!Created) throw new InvalidOperationException();
                return MPQVersion;
            }
        }

        /// <summary>MPQ의 섹터 크기를 가져옵니다.</summary>
        public UInt16 SectorSize {
            get {
                if(!Created) throw new InvalidOperationException();
                return MPQSectorSize;
            }
        }

        /// <summary>MPQ의 크기를 가져옵니다.</summary>
        public Int32 ArchiveSize {
            get {
                if(!Created) throw new InvalidOperationException();
                return (int)FileData.Length + ((HashTable.Length + BlockTable.Length) << 4);
            }
        }

        /// <summary>MPQ의 파일 갯수를 가져옵니다.</summary>
        public Int32 FileCount {
            get {
                if(!Created) throw new InvalidOperationException();
                return BlockTable.Length;
            }
        }

        /// <summary>MPQ의 해시 크기를 가져옵니다.</summary>
        public Int32 HashSize {
            get {
                if(!Created) throw new InvalidOperationException();
                return HashTable.Length;
            }
        }
    }
}

using System.IO;
using System;

namespace TkMPQLib
{
    using MPQ.DataTypes;
    using MPQ;
    
    public partial class TkMPQ
    {
        #region "Create MPQ"
        /// <summary>새로운 MPQ를 생성합니다.</summary>
        /// <param name="SectorSize">생성할 MPQ의 섹터 크기입니다.</param>
        /// <param name="HashSize">생성할 MPQ의 해시 테이블 크기입니다.</param>
        public virtual void CreateMPQ(UInt16 SectorSize = 3, Int32 HashSize = 0x400) {
            lock(ThreadLock) {
                HashTable = new HashTable(HashSize);
                Listfile.SetHashTable(HashTable);
                BlockTable = new BlockTable();
                FileData = new MemoryStream();
                Header = 0;
                Last = 0x20;
                MPQVersion = 0;
                MPQSectorSize = SectorSize;
                Created = true;
            }
        }
        #endregion

        #region "Open MPQ"
        /// <summary>파일 경로로부터 MPQ를 읽어들입니다.</summary>
        /// <param name="FileName">MPQ를 읽어들일 파일 경로입니다.</param>
        /// <exception cref="InvalidDataException">MPQ 파일이 아닐경우 Throw 되는 오류입니다.</exception>
        public void OpenMPQ(string FileName) {
            using(var FS = new FileStream(FileName, FileMode.Open, FileAccess.Read))
                OpenMPQ(FS);
        }

        /// <summary>스트림으로부터 MPQ를 읽어들입니다.</summary>
        /// <param name="Stream">MPQ를 읽어들일 스트림입니다.</param>
        /// <exception cref="NotSupportedException">스트림이 탐색이 불가능 할 경우 Throw 되는 오류입니다.</exception>
        /// <exception cref="InvalidDataException">MPQ 파일이 아닐경우 Throw 되는 오류입니다.</exception>
        public virtual void OpenMPQ(Stream Stream) {
            if(!Stream.CanSeek) throw new NotSupportedException();
            MPQHeader Header = new MPQHeader();
            long pHeader = Header.Find(Stream);
            if(Stream.Length <= pHeader) throw new InvalidDataException();
            lock(ThreadLock) { //For MultiThreading
                //Read Header
                MPQVersion = Header.Version;
                MPQSectorSize = Header.SectorSize;

                //Read Tables
                Stream.Position = pHeader + Header.pHash;
                HashTable = new HashTable(Stream, Header.sHash);
                Stream.Position = pHeader + Header.pBlock;
                BlockTable = new BlockTable(Stream, Header.sBlock);
                Listfile.SetHashTable(HashTable);

                //Read File Data
                int Offset = GetFirst();
                int Size = (int)Math.Min(Stream.Length - pHeader, Int32.MaxValue);
                FileData = new MemoryStream();
                if(0 < Offset) {
                    if(Offset < Header.pHash && Header.pHash < Size)
                        Size = Header.pHash;
                    if(Offset < Header.pBlock && Header.pBlock < Size)
                        Size = Header.pBlock;
                    Stream.Position = pHeader + Offset;
                    FileData.Position = Offset;
                    TkLib.TkStream.Copy(Stream, FileData, Size);
                    this.Header = 0;
                } else {
                    if(0 < Header.pHash && Header.pHash < Size)
                        Size = Header.pHash;
                    if(0 < Header.pBlock && Header.pBlock < Size)
                        Size = Header.pBlock;
                    //Buffering All
                    Stream.Position = 0;
                    Stream.CopyTo(FileData);
                    this.Header = pHeader;
                }
                Last = Size;
                Created = true;
                GetListfile();
            }
        }
        #endregion

        #region "Save MPQ"
        /// <summary>파일 경로에 MPQ를 작성합니다.</summary>
        /// <param name="FileName">MPQ를 작성할 파일 경로입니다.</param>
        public void SaveMPQ(string FileName) {
            using(FileStream FS = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite))
                SaveMPQ(FS);
        }

        /// <summary>스트림에 MPQ를 작성합니다.</summary>
        /// <param name="Stream">MPQ를 작성할 스트림입니다.</param>
        /// <exception cref="NotSupportedException">스트림이 탐색이 불가능 할 경우 Throw 되는 오류입니다.</exception>
        public virtual void SaveMPQ(Stream Stream) {
            if(!Created) throw new InvalidOperationException();
            if(!Stream.CanSeek) throw new NotSupportedException();
            MPQHeader Header = new MPQHeader();
            long pHeader = Header.Find(Stream);
            lock(ThreadLock) {
                //Write File Data
                Stream.Position = pHeader;
                FileData.Position = 0;
                pHeader += this.Header;
                FileData.CopyTo(Stream);

                //Write Tables
                Header.pHash = (int)(Stream.Position - pHeader);
                Header.sHash = HashTable.Length;
                HashTable.Write(Stream);
                Header.pBlock = (int)(Stream.Position - pHeader);
                Header.sBlock = BlockTable.Length;
                BlockTable.Write(Stream);

                //Write Header
                Stream.SetLength(Stream.Position);
                if(Header.HeaderSize < 0x20) Header.HeaderSize = 0x20;
                Header.ArchiveSize = (int)(Stream.Position - pHeader);
                Header.SectorSize = SectorSize;
                Header.Version = Version;
                Stream.Position = pHeader;
                Header.Write(Stream);
            }
        }
        #endregion

        protected virtual void GetListfile() {
            foreach(var Stream in GetFiles("(listfile)")) {
                try {
                    using(var Reader = new StreamReader(Stream))
                        while(!Reader.EndOfStream) Listfile.Add(Reader.ReadLine());
                } catch { }
            }
        }

        protected virtual int GetFirst() {
            int Min = System.Linq.Enumerable.Min(
                BlockTable.GetBlocks(HashTable),
                (i) => BlockTable[i].FileOffset);
            return Min == 0 ? 0x20 : Min;
        }
    }
}

using System.Collections.Generic;
using System.IO;
using System;

namespace TkMPQLib.MPQ.Data
{
    using static Encryption;
    using DataTypes;
    
    public abstract unsafe class Sector : MPQData
    {
        protected internal List<byte[]> Sectors;
        protected int LastSize;

        public Sector(string FilePath, FileFlags Flags, UInt16 Size) : base(FilePath, Flags, Size) {
            Sectors = new List<byte[]>();
            Sectors.Add(new byte[0]);
            GetKey();
        }

        public Sector(Stream Stream, long pHeader, UInt16 Size, Block Block, string FilePath = null) : base(FilePath, Block.Flags, Size) {
            Sectors = new List<byte[]>();
            try {
                Opened |= ReadData(Stream, pHeader, Block.FileOffset, Block.FileSize);
            } catch {
                Opened = false;
            }
        }

        /// <summary>모든 리소소를 해제합니다.</summary>
        public override void Dispose() {
            Sectors.Clear();
            Opened = false;
        }

        protected override bool ReadData(Stream Stream, long pHeader, int FileOffset, int FileSize) {
            //Read Offsets
            int Size = GetIndex(FileSize);
            byte[] Buf = new byte[(Size + 1) << 2];
            Stream.Position = pHeader + FileOffset;
            Stream.Read(Buf, 0, Buf.Length);
            LastSize = GetLast(FileSize);

            //Decrypt Offset
            if(!GetKey(Buf, FileOffset, FileSize)) return false;
            uint sKey = GetCryptKey(FileOffset, FileSize);
            if(Encrypted) DecryptData(Buf, sKey - 1);

            //Read Data
            fixed (byte* pBuf = Buf) {
                int* pOffset = (int*)pBuf;
                if(pHeader + FileOffset + *pOffset < 0) return false;
                Stream.Position = pHeader + FileOffset + *pOffset;
                for(uint i = 0; i < Size; i++) {
                    int cSector = pOffset[1] - *pOffset++;
                    if(cSector < 0 || SectorSize < cSector) return false;
                    byte[] Sector = new byte[cSector];
                    Stream.Read(Sector, 0, cSector);
                    if(Encrypted) DecryptData(Sector, sKey + i);
                    Sectors.Add(Sector);
                }
            }
            return true;
        }

        /// <summary>파일의 내용을 스트림에 작성합니다.</summary>
        /// <param name="Stream">내용을 작성할 스트림입니다.</param>
        /// <param name="pHeader">헤더의 위치입니다.</param>
        /// <param name="FileOffset">작성할 파일의 경로입니다.</param>
        public override int WriteData(Stream Stream, long pHeader, ref int FileOffset) {
            if(!Opened) throw new InvalidOperationException();
            uint sKey = GetCryptKey(FileOffset, FileSize);
            byte[][] Data = Sectors.ToArray();
            int Offset = (Data.Length + 1) << 2;

            //Calculate Offset
            byte[] Buf = new byte[Offset];
            fixed (byte* pBuf = Buf) {
                int* pOffset = (int*)pBuf;
                *pOffset = Offset;
                for(uint i = 0; i < Data.Length; i++)
                    *++pOffset = Offset += Data[i].Length;
            }

            //Write Offsets
            if(Encrypted) EncryptData(Buf, Buf.Length, sKey - 1);
            Stream.Position = pHeader + FileOffset;
            Stream.Write(Buf, 0, Buf.Length);

            //Write Data
            Buf = new byte[SectorSize];
            for(uint i = 0; i < Data.Length; i++) {
                Array.Copy(Data[i], Buf, Data[i].Length);
                if(Encrypted) EncryptData(Buf, Data[i].Length, sKey + i);
                Stream.Write(Buf, 0, Data[i].Length);
            }

            //Return Size
            return Offset;
        }

        protected virtual bool GetKey(byte[] Buffer, int FileOffset, int FileSize) {
            if(Path != null) {
                GetKey();
                return true;
            } else if(Encrypted) {  //Failed to get path
                try {
                    Key = FindKey(Buffer, FileSize, SectorSize);
                    if(ModKey) Key = (Key ^ (uint)FileSize) - (uint)FileOffset; //Modfly Key
                    CanEncrypt = true;
                    return true;
                } catch {}
            }
            return false;
        }

        /// <summary>파일의 크기를 재설정합니다.</summary>
        /// <param name="Length">재설정할 크기입니다.</param>
        public override void SetLength(int Length) {
            if(!Opened) throw new InvalidOperationException();
            int RIndex = GetIndex(Length);
            int CIndex = Sectors.Count;
            int Last = GetLast(Length);
            if(CIndex < RIndex) {
                byte[] Buf = new byte[SectorSize];
                ResizeIndex(CIndex - 1, SectorSize);
                for(int i = CIndex; i < RIndex - 1; i++) {
                    Sectors.Add(null);
                    WriteIndex(i, Buf, 0, SectorSize);
                }
                Sectors.Add(null);
                WriteIndex(RIndex - 1, Buf, 0, Last);
            } else if(CIndex > RIndex) {
                for(int i = CIndex; i < RIndex; i++)
                    Sectors.RemoveAt(CIndex);
                ResizeIndex(CIndex - 1, Last);
            } else
                ResizeIndex(RIndex - 1, Last);
            LastSize = Last;
        }

        /// <summary>해당 위치에 데이터를 읽어들입니다.</summary>
        /// <param name="Position">데이터를 읽어들일 위치입니다.</param>
        /// <param name="buffer">읽어들일 데이터 버퍼입니다.</param>
        /// <param name="offset">데이터 버퍼의 오프셋입니다.</param>
        /// <param name="count">데이터의 크기입니다.</param>
        public override int Read(int Position, byte[] buffer, int offset, int count) {
            if(!Opened) throw new InvalidOperationException();
            if(FileSize < Position + count) count = FileSize - Position;
            if(count == 0) return 0;
            int Size = count;
            int SIndex = Position / SectorSize;
            int EIndex = GetIndex(Position + count) - 1;
            int Last = Position % SectorSize;
            byte[] Buf = new byte[SectorSize];
            ReadIndex(SIndex, Buf, 0, GetIndexSize(SIndex));
            if(SIndex < EIndex) {
                int Spare = SectorSize - Last;
                Array.Copy(Buf, Last, buffer, offset, Spare);
                offset += Spare; count -= Spare;
                for(int i = SIndex + 1; i < EIndex; i++) {
                    ReadIndex(i, buffer, offset, SectorSize);
                    offset += SectorSize; count -= SectorSize;
                }
                ReadIndex(EIndex, Buf, 0, GetIndexSize(EIndex));
                Last = 0;
            }
            Array.Copy(Buf, Last, buffer, offset, count);
            return Size;
        }

        /// <summary>해당 위치에 데이터를 작성합니다.</summary>
        /// <param name="Position">데이터를 작성할 위치입니다.</param>
        /// <param name="buffer">작성할 데이터 버퍼입니다.</param>
        /// <param name="offset">데이터 버퍼의 오프셋입니다.</param>
        /// <param name="count">데이터의 크기입니다.</param>
        public override void Write(int Position, byte[] buffer, int offset, int count) {
            if(!Opened) throw new InvalidOperationException();
            if(count == 0) return;
            if(FileSize < Position + count) SetLength(Position + count);
            int SIndex = Position / SectorSize;
            int EIndex = GetIndex(Position + count) - 1;
            int Last = Position % SectorSize;
            byte[] Buf = new byte[SectorSize];
            if(0 < Last || count < SectorSize)
                ReadIndex(SIndex, Buf, 0, GetIndexSize(SIndex));
            if(SIndex < EIndex) {
                int Spare = SectorSize - Last;
                Array.Copy(buffer, offset, Buf, Last, Spare);
                WriteIndex(SIndex, Buf, 0, SectorSize);
                offset += Spare; count -= Spare;
                for(int i = SIndex + 1; i < EIndex; i++) {
                    WriteIndex(i, buffer, offset, SectorSize);
                    offset += SectorSize; count -= SectorSize;
                }
                ReadIndex(EIndex, Buf, 0, GetIndexSize(EIndex));
                Last = 0;
            }
            Array.Copy(buffer, offset, Buf, Last, count);
            WriteIndex(EIndex, Buf, 0, GetIndexSize(EIndex));
        }
        
        protected virtual void ResizeIndex(int Index, int Size) {
            byte[] Buf = new byte[SectorSize];
            ReadIndex(Index, Buf, 0, GetIndexSize(Index));
            WriteIndex(Index, Buf, 0, Size);
        }

        protected abstract void ReadIndex(int Index, byte[] buffer, int offset, int count);
        
        protected abstract void WriteIndex(int Index, byte[] buffer, int offset, int count);

        protected int GetLast(int Size) {
            int Last = Size % SectorSize;
            return Last == 0 ? SectorSize : Last;
        }

        protected int GetIndex(int Size)
            => (Size + SectorSize - 1) / SectorSize;

        protected int GetIndexSize(int Index)
            => Index == Sectors.Count - 1 ? LastSize : SectorSize;

        /// <summary>파일의 크기를 가져옵니다.</summary>
        public override int FileSize
            => ((Sectors.Count - 1) * SectorSize) + LastSize;

        /// <summary>파일의 압축된 크기를 가져옵니다.</summary>
        public override int CompSize {
            get {
                int Size = (Sectors.Count + 1) << 2;
                foreach(var s in Sectors) Size += s.Length;
                return Size;
            }
        }
    }
}

using System;

namespace TkMPQLib
{
    using MPQ;

    public partial class TkMPQ
    {

        /// <summary>파일의 암호화를 해제합니다.</summary>
        /// <param name="Stream">암호화를 해제할 <c>MPQReader</c>스트림입니다.</param>
        public virtual void DecryptFile(int Index, string FilePath = null) {
            if(!Created) throw new InvalidOperationException();
            lock(ThreadLock) {
                if(!Exists(Index)) return;
                var h = HashTable[Index];
                if(BlockTable.Length <= h.Block) return;
                var b = BlockTable[h.Block];
                var Data = MPQData.Create(FileData, Header, SectorSize, b, FilePath);
                Data.Encrypted = false;
                Data.WriteData(FileData, Header, ref b.FileOffset);
                b.Flags &= ~(FileFlags.Encrypted | FileFlags.ModKey);
                BlockTable[h.Block] = b;
            }
        }

        /// <summary>파일의 암호화 키를 변조합니다.</summary>
        /// <param name="Index">변조할 파일의 인덱스입니다.</param>
        /// <param name="FilePath">파일의 경로입니다.</param>
        public virtual void ModflyKey(int Index, string FilePath = null) {
            if(!Created) throw new InvalidOperationException();
            lock(ThreadLock) {
                if(!Exists(Index)) return;
                var h = HashTable[Index];
                if(BlockTable.Length <= h.Block) return;
                var b = BlockTable[h.Block];
                var Data = MPQData.Create(FileData, Header, SectorSize, b, FilePath);
                Data.ModKey = true;
                Data.WriteData(FileData, Header, ref b.FileOffset);
                b.Flags |= FileFlags.ModKey;
                BlockTable[h.Block] = b;
            }
        }

        /// <summary>파일의 암호화 키의 변조를 해제합니다.</summary>
        /// <param name="Index">변조를 해제할 파일의 인덱스입니다.</param>
        /// <param name="FilePath">파일의 경로입니다.</param>
        public virtual void DeModflyKey(int Index, string FilePath = null) {
            if(!Created) throw new InvalidOperationException();
            lock(ThreadLock) {
                if(!Exists(Index)) return;
                var h = HashTable[Index];
                if(BlockTable.Length <= h.Block) return;
                var b = BlockTable[h.Block];
                var Data = MPQData.Create(FileData, Header, SectorSize, b, FilePath);
                Data.ModKey = false;
                Data.WriteData(FileData, Header, ref b.FileOffset);
                b.Flags &= ~FileFlags.ModKey;
                BlockTable[h.Block] = b;
            }
        }

        /// <summary>해당 파일의 경로를 변경합니다.</summary>
        /// <param name="Index">파일의 인덱스입니다.</param>
        /// <param name="Path">변경할 경로입니다.</param>
        public virtual void Rename(ref int Index, string Path) {
            if(!Created) throw new InvalidOperationException();
            if(!Exists(Index)) return;
            lock(ThreadLock) {
                var d = MPQ.DataTypes.Hash.Null;
                var h = HashTable[Index];
                d.BlockIndex = HashTable.File_Deleted;
                HashTable[Index] = d;
                var b = BlockTable[h.Block];
                if(b.Flags.HasFlag(FileFlags.Encrypted)) {
                    var Reader = GetFile(Index);
                    Reader.Data.WriteData(FileData, Header, ref b.FileOffset);
                    BlockTable[h.Block] = b;
                }
                Listfile.Check(Index);
                Listfile.Add(Path);
            }
        }

        /// <summary>파일을 삭제합니다.</summary>
        /// <param name="Index">삭제할 파일의 인덱스입니다.</param>
        public virtual void Delete(int Index) {
            if(!Created) throw new InvalidOperationException();
            if(!Exists(Index)) return;
            var h = HashTable[Index];
            if(BlockTable.Length <= h.Block) return;
            var b = BlockTable[h.Block];
            b.Flags &= ~FileFlags.Exists;
            BlockTable[h.Block] = b;
            Listfile.Check(Index);
        }

        /// <summary>MPQ내부의 해시를 삭제합니다.</summary>
        /// <param name="Index">삭제할 파일의 인덱스입니다.</param>
        public virtual void DeleteHash(int Index) {
            if(!Created) throw new InvalidOperationException();
            if(!Exists(Index)) return;
            var h = HashTable[Index];
            h.BlockIndex = HashTable.File_Deleted;
            HashTable[Index] = h;
            Listfile.Check(Index);
        }

        /// <summary>해시 테이블을 두 배로 확장합니다. (사용되지 않습니다)</summary>
        /// <param name="Size">늘릴 해쉬의 크기입니다.</param>
        [Obsolete] public virtual void ExpendHash() {
            if(!Created) throw new InvalidOperationException();
            HashTable.Expend();
        }
    }
}

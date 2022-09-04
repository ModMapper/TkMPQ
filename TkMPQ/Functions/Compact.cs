using System.IO;
using System;

namespace TkMPQLib
{
    using MPQ.DataTypes;
    using MPQ;

    public partial class TkMPQ
    {

        /// <summary>MPQ의 사용하지 않는 부분을 제거합니다.</summary>
        public virtual void Compact() {
            if(!Created) throw new InvalidOperationException();
            lock(ThreadLock) {
                foreach(int i in HashTable.GetHashes()) {
                    if(!Deleted(i)) continue;
                    Hash h = HashTable[i];
                    h.BlockIndex = HashTable.File_Deleted;
                    HashTable[i] = h;
                }
            }
            Recreate();
        }

        /// <summary>MPQ를 재생성합니다.</summary>
        public virtual void Recreate() {
            if(!Created) throw new InvalidOperationException();
            Recreate_Block();   //Clear Unused Block
            Recreate_Hash();    //Clear Deleted Hash
            Recreate_Data();    //Clear Unused Data
        }
        
        protected virtual void Recreate_Hash() {
            lock(ThreadLock) {
                if(HashTable.Length == 0) return;
                Hash NullHash = MPQ.DataTypes.Hash.Null;
                int Last = HashTable.Length;
                if(HashTable[0].BlockIndex == HashTable.File_None && 
                        HashTable[Last].BlockIndex == HashTable.File_Deleted)
                    HashTable[Last] = NullHash;
                for(int i = Last - 2; 0 <= i; i--) {
                    if(HashTable[i + 1].BlockIndex != HashTable.File_None && 
                            HashTable[i].BlockIndex != HashTable.File_Deleted)
                        HashTable[i] = NullHash;
                }
            }
        }

        protected virtual void Recreate_Block() {
            var Relocate = new int[BlockTable.Length];
            long DataSize = FileData.Length;
            for(int i = 0; i < Relocate.Length; i++) Relocate[i] = -2;
            lock(ThreadLock) {
                var NewBlock = new BlockTable();
                foreach(int i in BlockTable.GetBlocks(HashTable, DataSize, Header)) {
                    Relocate[i] = NewBlock.Length;
                    NewBlock.Add(BlockTable[i]);
                }
                for(int i = 0; i < HashTable.Length; i++) {
                    var h = HashTable[i];
                    var b = h.Block;
                    h.BlockIndex = Relocate.Length <= b ? -2 : Relocate[h.Block];
                    HashTable[i] = h;
                }
                BlockTable = NewBlock;
            }
        }

        protected virtual void Recreate_Data() {
            MemoryStream Rewrite = new MemoryStream();
            long DataSize = FileData.Length;
            int LastWrite = 0x20;
            lock(ThreadLock) {
                foreach(int i in BlockTable.GetBlocks(HashTable, DataSize, Header)) {
                    Block b = BlockTable[i];
                    try {
                        using(MPQData Data = MPQData.Create(
                                FileData, Header, SectorSize, b, Listfile.FindName(i))) {
                            b.FileOffset = LastWrite;
                            LastWrite += Data.WriteData(Rewrite, 0, ref b.FileOffset);
                        }
                    BlockTable[i] = b;
                    } catch {}
                }
                FileData.Dispose();
                FileData = Rewrite;
                Last = LastWrite;
                Header = 0;
            }
        }
    }
}
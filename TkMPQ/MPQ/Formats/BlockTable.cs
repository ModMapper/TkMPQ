using System.Collections.Generic;
using System.Collections;
using System.IO;

namespace TkMPQLib.MPQ
{
    using static Encryption;
    using DataTypes;

    /// <summary>MPQ 블록 테이블</summary>
    public unsafe class BlockTable : IEnumerable<Block> {
        protected List<Block> Table;

        /// <summary>확장 가능한 빈 블록 테이블을 생성합니다.</summary>
        public BlockTable() {
            Table = new List<Block>();
        }

        /// <summary>스트림으로부터 블록 테이블을 읽어들입니다.</summary>
        /// <param name="Stream">읽어들일 스트림입니다.</param>
        /// <param name="Size">블록 테이블의 크기입니다.</param>
        public BlockTable(Stream Stream, int Size) {
            int Length = Size << 4;
            Table = new List<Block>();
            byte[] Buf = new byte[Length];
            Size = Length >> 4;                 //Cut 0xF-
            Stream.Read(Buf, 0, Length);        //Read Table
            DecryptData(Buf, BlockTableKey);    //Decrypt Table
            fixed (byte* pBuf = Buf) {
                Block* pTable = (Block*)pBuf;
                for(int i = 0; i < Size; i++) Table.Add(*(pTable++));
            }
        }

        /// <summary>블록 테이블을 스트림에 작성합니다.</summary>
        /// <param name="Stream">작성할 스트림입니다.</param>
        public void Write(Stream Stream) {
            int Length = this.Length << 4;
            byte[] Buf = new byte[Length];
            fixed (byte* pBuf = Buf) {
                Block* pTable = (Block*)pBuf;
                for(int i = 0; i < this.Length; i++) *(pTable++) = Table[i];
            }
            EncryptData(Buf, BlockTableKey);    //Encrypt Table
            Stream.Write(Buf, 0, Length);       //Write Table
        }

        /// <summary>블록 테이블의 크기입니다.</summary>
        public int Length => Table.Count;

        /// <summary>해당 인덱스의 블록을 가져오거나 설정합니다.</summary>
        /// <param name="Index">가져올 블록 인덱스입니다.</param>
        /// <returns>가져온 블록입니다.</returns>
        public Block this[int Index] {
            get => Table[Index];
            set => Table[Index] = value;
        }

        /// <summary>블록 테이블에 해당 블록을 추가합니다.</summary>
        /// <param name="Block">추가할 블록입니다.</param>
        /// <returns>추가한 블록 인덱스입니다.</returns>
        public int Add(Block Block) {
            int Index = Table.Count;
            Table.Add(Block);
            return Index;
        }

        /// <summary>유효한 파일인지 검증합니다.</summary>
        /// <param name="DataSize">데이터의 크기입니다.</param>
        /// <param name="pHeader">헤더의 위치입니다.</param>
        /// <param name="Index">검증할 블록 인덱스입니다.</param>
        /// <returns>사용 가능한지 여부입니다.</returns>
        public bool Check(long DataSize, long pHeader, int Index) {
            if(Index < 0) return false;             //Index < 0 
            if(Length <= Index) return false;       //Size <= Index 
            long Offset = pHeader + Table[Index].FileOffset;
            if(Offset < 0) return false;            //FileOffset < 0
            if(DataSize <= Offset) return false;    //Length <= FileOffset
            return true;
        }

        /// <summary>해시 테이블로 부터 블록 인덱스를 가져옵니다.</summary>
        /// <param name="Hash">블록 인덱스를 가져올 해시 테이블입니다.</param>
        /// <returns>정렬된 블록 인덱스입니다.</returns>
        public IEnumerable<int> GetBlocks(HashTable Hash) {
            var Blocks = new List<int>();
            foreach(int i in Hash.GetHashes()) {
                int Index = Hash[i].Block;
                if(Length <= Index) continue;
                if(!Blocks.Contains(Index))
                    Blocks.Add(Index);
            }
            Blocks.Sort();
            return Blocks;
        }

        /// <summary>해시 테이블로 부터 블록 인덱스를 가져옵니다.</summary>
        /// <param name="Hash">블록 인덱스를 가져올 해시 테이블입니다.</param>
        /// <param name="DataSize">데이터의 크기입니다.</param>
        /// <param name="pHeader">헤더의 위치입니다.</param>
        /// <returns>정렬된 블록 인덱스입니다.</returns>
        public IEnumerable<int> GetBlocks(HashTable Hash, long DataSize, long pHeader) {
            foreach(var i in GetBlocks(Hash)) {
                if(!Check(DataSize, pHeader, i)) continue;
                yield return i;
            }
        }

        public IEnumerator<Block> GetEnumerator()
            => Table.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => Table.GetEnumerator();
    }
}

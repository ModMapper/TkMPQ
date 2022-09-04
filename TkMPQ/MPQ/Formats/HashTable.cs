using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using System;

namespace TkMPQLib.MPQ
{
    using static Encryption;
    using DataTypes;

    /// <summary>MPQ 해시 테이블</summary>
    public unsafe class HashTable : IEnumerable<Hash>
    {
        /// <summary>존재하지 않는 해시의 블록 인덱스입니다.</summary>
        public const int File_None = -1;
        /// <summary>삭제된 해시의 블록 인덱스입니다.</summary>
        public const int File_Deleted = -2;
        protected Hash[] Table;

        /// <summary>지정된 크기의 해시 테이블을 생성합니다.</summary>
        /// <param name="Size">생성할 해시 테이블 크기입니다. 2의 제곱이여야 합니다.</param>
        public HashTable(int Size) {
            Size = NearestSize(Size);
            Table = new Hash[Size];
            for(int i = 0; i < Size; i++)
                Table[i] = Hash.Null;
        }

        /// <summary>스트림으로부터 해시 테이블을 읽어들입니다.</summary>
        /// <param name="Stream">읽어들일 스트림입니다.</param>
        /// <param name="Size">해시 테이블의 크기입니다.</param>
        public HashTable(Stream Stream, int Size) {
            int Length = Size << 4;
            byte[] Buf = new byte[Length];
            Size = Length >> 4;                 //Cut 0xF-
            Table = new Hash[Size];
            Stream.Read(Buf, 0, Length);        //Read Table
            DecryptData(Buf, HashTableKey);     //Decrypt Table
            fixed (Hash* pTable = Table)
                Marshal.Copy(Buf, 0, (IntPtr)pTable, Length);
        }

        /// <summary>해시 테이블을 스트림에 작성합니다.</summary>
        /// <param name="Stream">작성할 스트림입니다.</param>
        public void Write(Stream Stream) {
            int Length = this.Length << 4;
            byte[] Buf = new byte[Length];
            fixed (Hash* pTable = Table)
                Marshal.Copy((IntPtr)pTable, Buf, 0, Length);
            EncryptData(Buf, HashTableKey);     //Encrypt Table
            Stream.Write(Buf, 0, Length);       //Write Table
        }

        /// <summary>해시 테이블의 크기입니다.</summary>
        public int Length => Table.Length;

        /// <summary>해당 인덱스의 해시를 가져오거나 설정합니다..</summary>
        /// <param name="Index">가져올 해시 인덱스입니다.</param>
        /// <returns>가져온 해시입니다.</returns>
        public Hash this[int Index] {
            get => Table[Index];
            set => Table[Index] = value;
        }

        protected virtual int NearestSize(int Size) {
            //Find 2^n Size
            for(int i = 0; i < 32; i++)
                if(Size <= (1 << i)) return 1 << i;
            return 0;
        }

        /// <summary>해시 테이블에 해시를 추가합니다.</summary>
        /// <param name="Path">추가할 해시의 경로입니다.</param>
        /// <param name="Hash">추가할 해시입니다.</param>
        /// <returns>추가한 해시 인덱스입니다. 실패시 -1을 반환합니다.</returns>
        public virtual int Add(string Path, Hash Hash) {
            int Offset = (int)(HashString(Path, HashType.TableOffset) % Length);
            Hash.Name1 = HashString(Path, HashType.Name1);
            Hash.Name2 = HashString(Path, HashType.Name2);
            for(int i = 0; i < Length; i++) {
                var Index = (Offset + i) % Length;
                //if Hash is Empty
                if(Table[Index].BlockIndex == File_None ||
                    Table[Index].BlockIndex == File_Deleted) {
                    //Write Hash Data
                    Table[Index] = Hash;
                    return Index;
                }
            }
            return -1;  //Hash is full
        }

        /// <summary>해당 경로와 언어의 해시를 찾습니다.</summary>
        /// <param name="Path">찾을 경로입니다.</param>
        /// <param name="Locale">찾을 언어 코드 입니다.</param>
        /// <returns>찾아낸 해시입니다. 실패시 -1을 리턴합니다.</returns>
        public int Find(string Path, Locale Locale = Locale.Neutral) {
            int Offset = (int)(HashString(Path, HashType.TableOffset) % Length);
            uint Name1 = HashString(Path, HashType.Name1);
            uint Name2 = HashString(Path, HashType.Name2);
            int Result = -1;
            for(int i = 0; i < Length; i++) {
                var Index = (Offset + i) % Length;
                if(Table[Index].BlockIndex == File_None) break; //Hash is Null
                if(Table[Index].BlockIndex == File_Deleted) continue;
                if(Table[Index].Name1 == Name1 && Table[Index].Name2 == Name2)
                    if(Table[Index].Locale == Locale) Result = Index;
            }
            if(Result == -1 && Locale != Locale.Neutral) return Find(Path);
            return Result;
        }

        /// <summary>해당 경로의 해시를 모두 찾습니다.</summary>
        /// <param name="Path">찾을 경로입니다.</param>
        /// <returns>찾아낸 해시 열거입니다.</returns>
        public IEnumerable<int> FindAll(string Path) {
            int Offset = (int)(HashString(Path, HashType.TableOffset) % Length);
            uint Name1 = HashString(Path, HashType.Name1);
            uint Name2 = HashString(Path, HashType.Name2);
            var Hashes = new Dictionary<Locale, int>();
            for(int i = 0; i < Length; i++) {
                var Index = (Offset + i) % Length;
                if(Table[Index].BlockIndex == File_None) break; //Hash is Null
                if(Table[Index].BlockIndex == File_Deleted) continue;
                if(Table[Index].Name1 == Name1 && Table[Index].Name2 == Name2)
                    Hashes[Table[Index].Locale] = Index;
            }
            return Hashes.Values;
        }

        /// <summary>해시의 존재 여부를 가져옵니다.</summary>
        /// <param name="Index">가져올 해시의 인덱스입니다.</param>
        /// <returns>해시의 존재 여부입니다.</returns>
        public bool Exists(int Index) {
            if(Index < 0) return false;
            if(Length <= Index) return false;
            if(Table[Index].BlockIndex == File_None) return false;
            return Table[Index].BlockIndex != File_Deleted;
        }

        /// <summary>파일의 존재 여부를 가져옵니다.</summary>
        /// <param name="Path">가져올 파일의 경로입니다.</param>
        /// <returns>해시의 존재 여부입니다.</returns>
        public bool Exists(string Path) {
            int Offset = (int)(HashString(Path, HashType.TableOffset) % Length);
            uint Name1 = HashString(Path, HashType.Name1);
            uint Name2 = HashString(Path, HashType.Name2);
            for(int i = 0; i < Length; i++) {
                var Index = (Offset + i) % Length;
                if(Table[Index].BlockIndex == File_None) break; //Hash is Null
                if(Table[Index].BlockIndex == File_Deleted) continue;
                if(Table[Index].Name1 == Name1 && Table[Index].Name2 == Name2)
                    return true;
            }
            return false;
        }

        /// <summary>해시의 삭제 여부를 가져옵니다.</summary>
        /// <param name="Index">가져올 해시의 인덱스입니다.</param>
        /// <returns>해시의 삭제 여부입니다.</returns>
        public bool Deleted(int Index) {
            if(Index < 0) return false;
            if(Length <= Index) return false;
            if(Table[Index].BlockIndex == File_None) return false;
            return Table[Index].BlockIndex == File_Deleted;
        }

        /// <summary>사용 가능한 해시를 열거합니다.</summary>
        /// <returns>사용 가능한 해시의 인덱스입니다.</returns>
        public IEnumerable<int> GetHashes() {
            for(int i = 0; i < Table.Length; i++) {
                if(Table[i].BlockIndex == File_None) continue;
                if(Table[i].BlockIndex == File_Deleted) continue;
                yield return i;
            }
        }

        /// <summary>해시 테이블 크기를 두배로 확장합니다.</summary>
        [Obsolete] public void Expend() {
            int Original = Table.Length;
            Array.Resize(ref Table, Original * 2);
            for(int i = Original; i < Table.Length; i++) Table[i] = Hash.Null;
            for(int i = 0; i < Table.Length; i++)   //Fill None to Deleted
                if(Table[i].BlockIndex == File_None) Table[i].BlockIndex = File_Deleted;
        }
        
        public IEnumerator<Hash> GetEnumerator()
            => (IEnumerator<Hash>)Table.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => Table.GetEnumerator();
    }
}

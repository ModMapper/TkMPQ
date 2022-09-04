using System.Collections.Generic;
using System;

namespace TkMPQLib
{
    public partial class TkMPQ
    {
        /// <summary>해당 경로와 언어의 파일의 인덱스를 가져옵니다.</summary>
        /// <param name="FilePath">가져올 파일의 경로입니다.</param>
        /// <param name="Locale">가져올 파일의 언어 코드입니다.</param>
        /// <returns>파일의 인덱스입니다.</returns>
        public virtual int FindFile(string FilePath, Locale Locale = Locale.Neutral) {
            if(!Created) throw new InvalidOperationException();
            int Index = HashTable.Find(FilePath, Locale);
            if(Index != -1) Listfile.Add(FilePath);
            return Index;
        }

        /// <summary>해당 경로의 파일의 인덱스를 전부 가져옵니다.</summary>
        /// <param name="FilePath">가져올 파일의 경로입니다.</param>
        /// <returns>파일의 인덱스의 열거입니다.</returns>
        public virtual IEnumerable<int> FindFiles(string FilePath) {
            if(!Created) throw new InvalidOperationException();
            if(HashTable.Exists(FilePath)) Listfile.Add(FilePath);
            return HashTable.FindAll(FilePath);
        }

        /// <summary>MPQ내의 모든 파일의 인덱스를 가져옵니다</summary>
        /// <returns>파일의 인덱스의 열거입니다.</returns>
        public virtual IEnumerable<int> FindFiles() {
            if(!Created) throw new InvalidOperationException();
            long DataSize = FileData.Length;
            foreach(int i in HashTable.GetHashes()) {
                int Index = HashTable[i].Block;
                if(!BlockTable.Check(DataSize, Header, Index)) continue;
                yield return i;
            }
        }

        /// <summary>파일의 인덱스에서 파일의 이름을 추정합니다.</summary>
        /// <param name="Index">파일의 인덱스입니다.</param>
        /// <param name="Unknown">알수 없을때 파일의 이름을 생성할지 여부입니다.</param>
        /// <returns>파일의 이름입니다.</returns>
        public virtual string FindName(int Index, bool Unknown = false) {
            if(!Created) throw new InvalidOperationException();
            var FilePath = Listfile.FindName(Index);
            if(FilePath == null && Unknown) FilePath = UnknownNamer(Index);
            return FilePath;
        }

        /// <summary>파일의 존재 여부를 가져옵니다.</summary>
        /// <param name="Index">파일의 인덱스입니다.</param>
        /// <returns>파일의 존재 여부입니다..</returns>
        public virtual bool Exists(int Index) {
            if(!Created) throw new InvalidOperationException();
            if(!HashTable.Exists(Index)) return false;
            return HashTable[Index].Block < BlockTable.Length;
        }

        /// <summary>파일의 삭제 여부를 가져옵니다.</summary>
        /// <param name="Index">파일의 인덱스입니다.</param>
        /// <returns>파일의 삭제 여부입니다..</returns>
        public virtual bool Deleted(int Index) {
            if(!Created) throw new InvalidOperationException();
            if(!HashTable.Exists(Index)) return HashTable.Deleted(Index);
            int b = HashTable[Index].Block;
            if(BlockTable.Length <= b) return false;
            FileFlags Flags = BlockTable[b].Flags;
            return !Flags.HasFlag(FileFlags.Exists);
        }
    }
}

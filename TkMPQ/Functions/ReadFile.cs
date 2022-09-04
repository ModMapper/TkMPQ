using System.Collections.Generic;
using System;

namespace TkMPQLib
{
    public partial class TkMPQ
    {
        /// <summary>해당 경로와 언어의 파일을 가져옵니다.</summary>
        /// <param name="FilePath">가져올 파일의 경로입니다.</param>
        /// <param name="Locale">가져올 파일의 언어 코드입니다.</param>
        /// <returns>MPQ 읽기 스트림입니다.</returns>
        public virtual MPQReader GetFile(string FilePath, Locale Locale = Locale.Neutral) {
            if(!Created) throw new InvalidOperationException();
            lock(ThreadLock) {
                int Index = HashTable.Find(FilePath, Locale);
                if(Index == -1) return null;
                Listfile.Add(FilePath);
                return new MPQReader(this, Index, FilePath);
            }
        }

        /// <summary>해당 인덱스의 파일을 가져옵니다.</summary>
        /// <param name="Index">가져올 파일의 인덱스입니다.</param>
        /// <param name="FilePath">가져올 파일 경로입니다.</param>
        /// <returns>해당 파일의 읽기 스트림입니다.</returns>
        public virtual MPQReader GetFile(int Index, string FilePath = null) {
            if(!Created) throw new InvalidOperationException();
            return new MPQReader(this, Index, FilePath ?? Listfile.FindName(Index));
        }

        /// <summary>해당 경로의 파일을 전부 가져옵니다.</summary>
        /// <param name="FilePath">가져올 파일의 경로입니다.</param>
        /// <returns>MPQ 읽기 스트림의 열거입니다.</returns>
        public virtual IEnumerable<MPQReader> GetFiles(string FilePath) {
            if(!Created) throw new InvalidOperationException();
            if(!HashTable.Exists(FilePath)) yield break;
            Listfile.Add(FilePath);
            foreach(int i in HashTable.FindAll(FilePath))
                yield return new MPQReader(this, i, FilePath);
        }
    }
}

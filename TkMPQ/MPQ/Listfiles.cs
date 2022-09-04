using System.Collections.Generic;
using System.Linq;
using System;

namespace TkMPQLib.MPQ
{
    /// <summary>파일 목록을 관리합니다.</summary>
    public class Listfiles {
        protected Dictionary<int, string> Files;
        protected HashSet<string> FileList;

        protected object ThreadLock = new object();
        protected HashTable Table;

        /// <summary>관리되는 파일 목록을 생성합니다.</summary>
        public Listfiles() {
            FileList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Files = new Dictionary<int, string>();
        }

        /// <summary>해시 테이블을 설정합니다.</summary>
        /// <param name="Table">설정할 해시 테이블입니다.</param>
        public virtual void SetHashTable(HashTable Table) {
            lock(ThreadLock) {
                Files.Clear();
                this.Table = Table;
                foreach(var f in FileList) FindFile(f);
            }
        }

        /// <summary>해시 테이블 인덱스로부터 파일 경로를 찾습니다.</summary>
        /// <param name="Index">해시 테이블 인덱스입니다.</param>
        /// <returns>찾아낸 경로입니다. 실패시 null을 반환합니다.</returns>
        public virtual string FindName(int Index)
            => Files.ContainsKey(Index) ? Files[Index] : null;

        /// <summary>파일 목록에 해당 경로를 추가합니다.</summary>
        /// <param name="Path">리스트에 추가할 경로입니다.</param>
        public virtual void Add(string Path) {
            lock(ThreadLock) {
                if(FileList.Contains(Path)) return;
                FileList.Add(Path);
                FindFile(Path);
            }
        }

        /// <summary>파일 목록에 해당 경로들을 추가합니다.</summary>
        /// <param name="Paths">리스트에 추가할 경로들입니다.</param>
        public virtual void AddRange(IEnumerable<string> Paths) {
            lock(ThreadLock) {
                foreach(var Path in Paths) {
                    if(FileList.Contains(Path)) continue;
                    FileList.Add(Path);
                    FindFile(Path);
                }
            }
        }

        protected virtual void FindFile(string Path) {
            if(Table == null) return;
            foreach(int i in Table.FindAll(Path))
                Files[i] = Path;
        }

        /// <summary>파일 목록에서 해당 경로를 제거합니다.</summary>
        /// <param name="Path">리스트에서 제거할 경로입니다.</param>
        public virtual void Remove(string Path) {
            lock(ThreadLock) FileList.Remove(Path);
        }

       /// <summary>파일 리스트를 초기화합니다.</summary>
        public virtual void Clear() {
            lock(ThreadLock) FileList.Clear();
        }

        /// <summary>해당 파일이 존재하는지 확인합니다.</summary>
        /// <param name="Index">확인할 파일의 인덱스입니다.</param>
        public virtual void Check(int Index) {
            lock(ThreadLock)
                if(Files.ContainsKey(Index) && !Table.FindAll(Files[Index]).Contains(Index))
                    Files.Remove(Index);
        }

        /// <summary>모든 경로를 가져옵니다.</summary>
        /// <returns>가져온 경로 목록입니다.</returns>
        public virtual string[] ToArray()
            => FileList.ToArray();

        /// <summary>존재하는 모든 파일 경로을 가져옵니다.</summary>
        /// <returns>가져온 파일 경로 목록입니다.</returns>
        public virtual string[] GetPaths()
            => Files.Values.Distinct().ToArray();

        /// <summary>경로가 확인된 모든 파일의 인덱스를 가져옵니다.</summary>
        /// <returns>경로가 확인된 파일의 인덱스 목록입니다.</returns>
        public virtual int[] GetFiles()
            => Files.Keys.ToArray();
    }
}

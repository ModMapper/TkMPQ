using System.Collections.Generic;
using System;

namespace TkMPQLib.MPQ.Data
{
    using static Encryption;

    class Cluster
    {
        protected List<MPQWriter> FileData;
        protected TkMPQ MPQ;

        public Cluster(TkMPQ MPQ) {
            FileData = new List<MPQWriter>();
            this.MPQ = MPQ;
        }

        /// <summary>클러스터에 섹터를 추가합니다.</summary>
        /// <param name="Writer">추가할 섹터가 포함된 MPQWriter 입니다</param>
        /// <exception cref="InvalidOperationException">릴리스 된 MPQWriter일때 Throw 되는 예외입니다.</exception>
        /// <exception cref="NotSupportedException">Sector이 아닌 MPQWriter 일때 Throw 되는 예외입니다.</exception>
        public void WriteFile(MPQWriter Writer) {
            if(!Writer.CanRead) throw new InvalidOperationException();
            if(!(Writer.Data is Sector)) throw new NotSupportedException();
            FileData.Add(Writer);
        }

        public unsafe void Flush() {
            var Files = FileData.ToArray();
            int Count = Files.Length;
            MPQ.LockAction(() => {
                var Sectors = new byte[Count][][];
                var Keys = new uint[Count];
                var Stream = MPQ.FileData;
                int Offset = MPQ.Last;
                byte[] Buf;

                //Get Sectors
                Stream.Position = MPQ.Header + MPQ.Last;
                for(int i = 0; i < Count; i++) {
                    Sector Sector = (Sector)Files[i].Data;
                    Sectors[i] = Sector.Sectors.ToArray();
                    Keys[i] = Sector.GetCryptKey(Offset, Sector.FileSize);
                    AddFile(Files[i], Offset);
                    Offset += (Sectors[i].Length + 1) << 2;
                }

                //Write Offset
                Offset -= MPQ.Last;
                MPQ.Last += Offset;
                for(int i = 0; i < Count; i++) {
                    Buf = new byte[(Sectors[i].Length + 1) << 2];
                    fixed(byte* pBuf = Buf) {
                        int* pOffset = (int*)pBuf;
                        *pOffset = Offset;
                        for(uint s = 0; s < Sectors[i].Length; s++)
                            *++pOffset = (Offset += Sectors[i][s].Length);
                    }
                    if(Files[i].Data.Encrypted) EncryptData(Buf, Keys[i] - 1);
                    Stream.Write(Buf, 0, Buf.Length);
                    Offset -= Buf.Length;
                }

                //Write Data
                Offset = 0;
                Buf = new byte[0x200 << MPQ.SectorSize];
                for(int i = 0; i < Count; i++) {
                    for(uint s = 0; s < Sectors[s].Length; s++) {
                        int Size = Sectors[i][s].Length;
                        Array.Copy(Sectors[i][s], Buf, Size);
                        if(Files[i].Data.Encrypted) EncryptData(Buf, Size, Keys[i] + s);
                        Stream.Write(Buf, 0, Size);
                        Offset += Size;
                    }
                }
                MPQ.Last += Offset;
            });
        }

        protected void AddFile(MPQWriter Writer, int FileOffset) {
            var Index = MPQ.BlockTable.Add(Writer.GetBlock(FileOffset));
            MPQ.HashTable.Add(Writer.FileName, Writer.GetHash(Index));
        }


    }
}

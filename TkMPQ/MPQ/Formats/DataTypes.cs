using System.IO;
using System;
using TkLib;

namespace TkMPQLib.MPQ
{
    namespace DataTypes
    {
        /// <summary>MPQ의 헤더입니다.</summary>
        public unsafe struct MPQHeader
        {
            private const int MPQInfo = 0x1A51504D;     //MPQ 0x1A
            private const int Step = 0x200;             //Step Bytes
            public Int32 MPQ;                  //4Byte.  MPQ 0x1A (Int 32)
            public UInt32 HeaderSize;          //UInt32. Header Size
            public Int32 ArchiveSize;          //Int32.  Archive Size
            public UInt16 Version;             //UInt16. Version
            public UInt16 SectorSize;          //UInt16. Sector Size
            public Int32 pHash;                //Int32.  Hash Table Offset
            public Int32 pBlock;               //Int32.  Block Table Offset
            public Int32 sHash;                //Int32.  Hash Table Size
            public Int32 sBlock;               //Int32.  Block Table Size

            /// <summary>스트림에서 헤더를 탐색합니다.</summary>
            /// <param name="Stream">헤더를 탐색할 스트림입니다.</param>
            /// <returns>탐색 또는 생성된 헤더의 위치압니다.</returns>
            public unsafe long Find(Stream Stream) {
                int BufferSize = sizeof(MPQHeader);
                byte[] Buf = new byte[BufferSize];
                long Pos = Stream.Position;
                fixed (byte* pBuf = Buf) {
                    MPQHeader* pHeader = (MPQHeader*)pBuf;
                    while(Stream.Read(Buf, 0, BufferSize) == BufferSize) {
                        if(pHeader->MPQ == MPQInfo && 0x20 <= pHeader->HeaderSize) {
                            this = *pHeader;
                            return Pos;
                        }
                        Stream.Position = (Pos += Step);
                    }
                    if(Stream.Position != Pos) Pos += Step;
                    Stream.Position = Pos + BufferSize;
                    return Pos;
                }
            }

            /// <summary>스트림에 헤더를 작성합니다.</summary>
            /// <param name="Stream">헤더를 작성할 스트림입니다.</param>
            public unsafe void Write(Stream Stream) {
                int BufferSize = sizeof(MPQHeader);
                byte[] Buf = new byte[BufferSize];
                fixed (byte* pBuf = Buf) {
                    *(MPQHeader*)pBuf = this;
                    ((MPQHeader*)pBuf)->MPQ = MPQInfo;
                }
                Stream.Write(Buf, 0, BufferSize);
            }
        }


        /// <summary>MPQ의 해시 테이블을 구성하는 해시입니다.</summary>
        public unsafe struct Hash {
            public UInt32 Name1, Name2;
            public Locale Locale;
            public Byte Version, Platform;
            public Int32 BlockIndex;

            /// <summary>필터링된 블록 테이블 값입니다.</summary>
            public Int32 Block => BlockIndex & 0x0FFFFFFF;

            /// <summary>빈 해시 값입니다.</summary>
            public static Hash Null {
                get {
                    byte* Buf = stackalloc byte[sizeof(Hash)];
                    Memory.memset(Buf, 0xFF, sizeof(Hash));
                    return *(Hash*)Buf;
                }
            }
        }

        /// <summary>MPQ의 블록 테이블을 구성하는 블록입니다.</summary>
        public unsafe struct Block {
            public Int32 FileOffset;
            public Int32 CompSize;
            public Int32 FileSize;
            public FileFlags Flags;

            /// <summary>빈 블록 값입니다.</summary>
            public static Block Null {
                get {
                    Block Data = new Block();
                    Data.Flags = FileFlags.Exists;
                    return Data;
                }
            }
        }
    }
}

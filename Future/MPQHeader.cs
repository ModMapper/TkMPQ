using System;
using System.Runtime.InteropServices;

namespace TkMPQ.MPQ
{
    [StructLayout(LayoutKind.Sequential)]
    unsafe internal struct MPQ {
        fixed Byte MPQ[4];          //4Byte.  MPQ 0x1A
        UInt32 HeaderSize;          //UInt32. Header Size
        UInt32 ArchiveSize;         //UInt32. Archive Size
        UInt16 Version;             //UInt16. Version
        UInt16 BlockSize;           //UInt16. Block Size
        UInt32 pHash;               //UInt32. Hash Table Offset
        UInt32 pBlock;              //UInt32. Block Table Offset
        UInt32 sHash;               //UInt32. Hash Table Size
        UInt32 sBlock;              //UInt32. Block Table Size

        //Other Version Header
        MPQHeaderV2 HeaderV2;
        MPQHeaderV3 HeaderV3;
        MPQHeaderV4 HeaderV4;


        [StructLayout(LayoutKind.Sequential)]
        internal struct MPQHeaderV2 {
            UInt64 pEBlock;             //UInt64. Extended Block Table Offset
            UInt16 phHash;              //UInt16. Hash Table Offset
            UInt16 phBlock;             //UInt16. Block Table Offset
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MPQHeaderV3 {
            UInt64 ArchiveSize;         //UInt64. Archive Size 64
            UInt64 pBET;                //UInt64. BET Table Offset
            UInt64 pHET;                //UInt64. HET Table Offset
        }

        private const int sMD5 = 0x10;

        [StructLayout(LayoutKind.Sequential)]
        internal struct MPQHeaderV4 {
            UInt64 sHash;               //UInt64. Compressed Hash Table Size
            UInt64 sBlock;              //UInt64. Compressed Block Table Size
            UInt64 sEBlock;             //UInt64. Compressed Extended Block Table Size
            UInt64 sHET;                //UInt64. Compressed HET Table Size
            UInt64 sBET;                //UInt64. Compressed BET Table Size
            UInt16 sRawChunk;           //UInt16. Raw Chunk Size
            fixed byte hBlock[sMD5];    //16Byte. Block Table Hash
            fixed byte hHash[sMD5];     //16Byte. Hash Table Hash
            fixed byte hEBlock[sMD5];   //16Byte. Extended Block Table Hash
            fixed byte hBET[sMD5];      //16Byte. BET Table Hash
            fixed byte hHET[sMD5];      //16Byte. HET Table Hash
            fixed byte hHeader[sMD5];   //16Byte. MPQ Header Hash
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MPQHeaderC {
            MPQ Header;           //32 Bytes
            MPQHeaderV2 HeaderV2;       //44 Bytes (+ 12 Bytes) 
            MPQHeaderV3 HeaderV3;       //68 Bytes (+ 24 Bytes)
            MPQHeaderV4 HeaderV4;       //206 Bytes (+ 138 Bytes)
        }
    }
}

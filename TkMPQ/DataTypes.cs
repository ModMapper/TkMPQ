using System;

namespace TkMPQLib
{
    /// <summary>Compression Types</summary>
    [Flags] public enum Compressions : byte {
        None = 0x00,
        Huffman = 0x01,
        Deflate = 0x02,
        Implode = 0x08,
        BZip2 = 0x10,
        LZMA = 0x12,
        Sparse = 0x20,
        ADPCM_Mono = 0x40,
        ADPCM_Stereo = 0x80
    }

    /// <summary>Wave Compression Quilty</summary>
    public enum WaveLevel : int {
        /// <summary>Best Quality - Low Compression</summary>
        QualityHigh = 0,
        /// <summary>Medium Quality - Medium Compression</summary>
        QualityMedium = 4,
        /// <summary>Low Quality - Best Compression</summary>
        QualityLow = 2
    }

    /// <summary>파일의 속성입니다.</summary>
    [Flags] public enum FileFlags : UInt32 {
        None = 0,
        /// <summary>Implode 압축됨</summary>
        Imploded = 0x100,
        /// <summary>압축됨</summary>
        Compressed = 0x200,
        /// <summary>암호화됨</summary>
        Encrypted = 0x10000,
        /// <summary>키 변조됨</summary>
        ModKey = 0x20000,
        /// <summary>존재하는 파일</summary>
        Exists = 0x80000000,
        
        /// <summary>삭제됨</summary>
        [Obsolete] Deleted = 0x2000000,
        /// <summary>섹션을 분할하지 않음</summary>
        [Obsolete] SingleUnit = 0x1000000,
        /// <summary>CRC 값 포함</summary>
        [Obsolete] CRC = 0x4000000
    }

    /// <summary>파일의 언어 코드입니다</summary>
    public enum Locale : UInt16 {
        Neutral = 0,
        Chinese = 0x404,
        Czech = 0x405,
        German = 0x407,
        English = 0x409,
        Spanish = 0x40A,
        French = 0x40C,
        Italian = 0x410,
        Japanese = 0x411,
        Korean = 0x412,
        Polish = 0x415,
        Portuguese = 0x416,
        Russsuan = 0x419,
        EnglishUK = 0x809
    }
}

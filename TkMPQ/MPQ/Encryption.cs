using System.Text;
using System.IO;

namespace TkMPQLib.MPQ
{
    /// <summary>MPQ 암호화 모듈</summary>
    public unsafe static class Encryption
    {
        private static uint[] CryptTable = new uint[0x500];
        /// <summary>블록 테이블의 키값입니다.</summary>
        public const uint HashTableKey = 0xC3AF3770;
        /// <summary>해시 테이블의 키값입니다.</summary>
        public const uint BlockTableKey = 0xEC83B3A3;
        
        /// <summary>문자열 해시 종류입니다.</summary>
        public enum HashType : uint {
            TableOffset = 0,
            Name1 = 1,
            Name2 = 2,
            FileKey = 3}

        static Encryption() {
            //Initlize Crypt Table
            uint seed = 0x00100001;
            uint Seed1, Seed2;
            int x, y, i;

            for(x = 0; x < 0x100; x++) {
                y = x;
                for(i = 0; i < 5; i++) {
                    seed = (seed * 125 + 3) % 0x2AAAAB;
                    Seed1 = (seed & 0xFFFF) << 0x10;

                    seed = (seed * 125 + 3) % 0x2AAAAB;
                    Seed2 = (seed & 0xFFFF);

                    CryptTable[y] = (Seed1 | Seed2);
                    y += 0x100;
                }
            }
        }
        
        /// <summary>버퍼를 해당 키로 암호화합니다.</summary>
        /// <param name="Buffer">암호화 할 내용입니다.</param>
        /// <param name="Key">암호화 할 키값입니다.</param>
        public static void EncryptData(byte[] Buffer, uint Key)
            => EncryptData(Buffer, Buffer.Length, Key);

        /// <summary>버퍼를 해당 키로 암호화합니다.</summary>
        /// <param name="Buffer">암호화 할 내용입니다.</param>
        /// <param name="Size">버퍼의 크기입니다.</param>
        /// <param name="Key">암호화 할 키값입니다.</param>
        public static void EncryptData(byte[] Buffer, int Size, uint Key) {
            fixed (byte* pBuffer = Buffer) {
                int Length = Size / 4;
                uint* pBuf = (uint*)pBuffer;
                uint Seed = 0xEEEEEEEE, Data;

                while(Length-- > 0) {
                    Seed += CryptTable[0x400 + (Key & 0xFF)];
                    Data = *pBuf ^ (Key + Seed);

                    Key = ((~Key << 0x15) + 0x11111111) | (Key >> 0x0B);
                    Seed = *pBuf + Seed + (Seed << 5) + 3;

                    *pBuf++ = Data;
                }
            }
        }

        /// <summary>버퍼를 해당 키로 복호화합니다.</summary>
        /// <param name="Buffer">복호화 할 내용입니다.</param>
        /// <param name="Key">복호화 할 키값입니다.</param>
        public static void DecryptData(byte[] Buffer, uint Key)
            => DecryptData(Buffer, Buffer.Length, Key);

        /// <summary>버퍼를 해당 키로 복호화합니다.</summary>
        /// <param name="Buffer">복호화 할 내용입니다.</param>
        /// <param name="Size">버퍼의 크기입니다.</param>
        /// <param name="Key">복호화 할 키값입니다.</param>
        public static void DecryptData(byte[] Buffer, int Size, uint Key) {
            fixed (byte* pBuffer = Buffer) {
                int Length = Size / 4;
                uint* pBuf = (uint*)pBuffer;
                uint Seed = 0xEEEEEEEE, Data;

                while(Length-- > 0) {
                    Seed += CryptTable[0x400 + (Key & 0xFF)];
                    Data = *pBuf ^ (Key + Seed);

                    Key = ((~Key << 0x15) + 0x11111111) | (Key >> 0x0B);
                    Seed = Data + Seed + (Seed << 5) + 3;

                    *pBuf++ = Data;
                }
            }
        }

        /// <summary>문자열의 해시 값을 생성합니다.</summary>
        /// <param name="Str">해시 값을 생성할 문자열입니다.</param>
        /// <param name="Type">생성할 해시 종류입니다.</param>
        /// <returns>생성된 해시 값입니다.</returns>
        public static uint HashString(string Str,  HashType Type) {
            var Buf = Encoding.GetEncoding(0).GetBytes(Str.ToUpper());
            uint Seed1 = 0x7FED7FED, Seed2 = 0xEEEEEEEE;
            var dwType = (uint)Type;
            for(int i = 0; i < Buf.Length; i++) {
                Seed1 = CryptTable[(dwType * 0x100) + Buf[i]] ^ (Seed1 + Seed2);
                Seed2 = Buf[i] + Seed1 + Seed2 + (Seed2 << 5) + 3;
            }
            return Seed1;
        }

        /// <summary>암호화된 오프셋의 키 값을 찾습니다.</summary>
        /// <param name="Offsets">암호화된 오프셋 목록입니다.</param>
        /// <param name="SectorSize">MPQ 섹터 크기입니다.</param>
        /// <returns>탐지된 검색된 키 값입니다.</returns>
        /// <exception cref="InvalidDataException">키 값을 찾지 못할때 Throw 되는 예외입니다.</exception>
        public static uint FindKey(byte[] Buffer, int FileSize, int SectorSize) {
            uint First = (uint)((FileSize + SectorSize - 1) / SectorSize + 1) << 2;
            uint Seed, sKey, Key, Find;
            uint Offset;

            fixed (byte* pBuffer = Buffer) {
                uint* Offsets = (uint*)pBuffer;
                sKey = (Offsets[0] ^ First) - 0xEEEEEEEE;
                for(int i = 0; i < 0x100; i++) {
                    Seed = 0xEEEEEEEE;
                    Key = sKey - CryptTable[0x400 + i];
                    Seed += CryptTable[0x400 + (Key & 0xFF)];
                    Offset = Offsets[0] ^ (Key + Seed);

                    if(First != Offset) continue;
                    Find = Key + 1;

                    Key = ((~Key << 0x15) + 0x11111111) | (Key >> 0x0B);
                    Seed = Offset + Seed + (Seed << 5) + 3;

                    Seed += CryptTable[0x400 + (Key & 0xFF)];
                    Offset = Offsets[1] ^ (Key + Seed);

                    if(Offset <= First + SectorSize) return Find;
                }
                throw new InvalidDataException();
            }
        }

        public static uint ModKey(uint Key, int FileOffset, int FileSize)
            => (Key + (uint)FileOffset) ^ (uint)FileSize;
    }
}

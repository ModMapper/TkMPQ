using System.Runtime.InteropServices;
using System.IO;
using System;
using TkLib;

namespace TkCompLib.Compression
{
    using static TkLib.Memory;
    using static Pkware.Constants;

    namespace Pkware
    {
        /// <summary>Pkware Explode Decompression</summary>
        public unsafe class Explode
        {
            TDcmpStruct Work = new TDcmpStruct();
            FixedStream sIn, sOut;

            /// <summary>Create Explode Decompress</summary>
            /// <param name="bIn">Input Buffer</param>
            /// <param name="bOut">Output Buffer</param>
            public Explode(byte[] bIn, byte[] bOut) {
                sIn = new FixedStream(bIn);
                sOut = new FixedStream(bOut);
            }

            internal Explode(FixedStream In, FixedStream Out) {
                sIn = In;
                sOut = Out;
            }

            /// <summary>Decompress</summary>
            public int Decompress() {
                // 포인터 사용을 위해 고정
                fixed (TDcmpStruct* pWork = &Work) explode(pWork);
                return sOut.EndOfStream ? -1 : sOut.Position;
            }

            private void explode(TDcmpStruct* pWork) {
                pWork->in_bytes = (uint)Read(pWork->in_buff, TDcmpStruct.BufferSize);
                if(pWork->in_bytes <= 4) throw new ArgumentNullException();

                pWork->ctype = pWork->in_buff[0];
                pWork->dsize_bits = pWork->in_buff[1];
                pWork->bit_buff = pWork->in_buff[2];
                pWork->extra_bits = 0;
                pWork->in_pos = 3;

                if(4 > pWork->dsize_bits || pWork->dsize_bits > 6)
                    throw new IndexOutOfRangeException();

                pWork->dsize_mask = 0xFFFFu >> (int)(0x10 - pWork->dsize_bits);

                switch(pWork->ctype) {
                case 0:
                    break;
                case 1:
                    ArrayCopy(ChBitsAsc, pWork->ChBitsAsc);
                    fixed (ushort* pChCodeAsc = ChCodeAsc) GenAscTabs(pWork);
                    break;
                default:
                    throw new NotSupportedException();
                }

                ArrayCopy(LenBits, pWork->LenBits);
                GenDecodeTabs(pWork->LengthCodes, LenCode, pWork->LenBits, LenBits.Length);
                ArrayCopy(ExLenBits, pWork->ExLenBits);
                ArrayCopy(LenBase, pWork->LenBase);
                ArrayCopy(DistBits, pWork->DistBits);
                GenDecodeTabs(pWork->DistPosCodes, DistCode, pWork->DistBits, DistBits.Length);
                if(Expand(pWork) == 0x306)
                    throw new EndOfStreamException();
            }

            uint Expand(TDcmpStruct* pWork) {
                uint next_literal, result;

                pWork->outputPos = 0x1000;

                while((result = next_literal = DecodeLit(pWork)) < 0x305) {
                    if(0x100 <= next_literal) {
                        byte* source, target;
                        uint minus_dist;

                        uint rep_length = next_literal - 0xFE;

                        if((minus_dist = DecodeDist(pWork, rep_length)) == 0) {
                            result = 0x306;
                            break;
                        }

                        target = &pWork->out_buff[pWork->outputPos];
                        source = target - minus_dist;

                        pWork->outputPos += rep_length;

                        while(rep_length-- > 0) *target++ = *source++;
                    } else
                        pWork->out_buff[pWork->outputPos++] = (byte)next_literal;

                    if(0x2000 <= pWork->outputPos) {
                        if(Write(&pWork->out_buff[0x1000], 0x1000)) return 0;

                        memmove(pWork->out_buff, &pWork->out_buff[0x1000], (int)pWork->outputPos - 0x1000);
                        pWork->outputPos -= 0x1000;
                    }
                }

                Write(&pWork->out_buff[0x1000], (int)pWork->outputPos - 0x1000);
                return result;
            }

            void GenAscTabs(TDcmpStruct* pWork) {
                int acc, add;
                for(byte count = 0xFF; 0 <= count; count--) {
                    byte* pChBitsAsc = pWork->ChBitsAsc + count;
                    byte bits_asc = *pChBitsAsc;
                    add = (1 << bits_asc);
                    if(bits_asc <= 8) {
                        acc = ChCodeAsc[count];

                        do {
                            pWork->offs2C34[acc] = count;
                            acc += add;
                        } while(acc < 0x100);
                    } else if((acc = (ChCodeAsc[count] & 0xFF)) != 0) {
                        pWork->offs2C34[acc] = 0xFF;

                        if((ChCodeAsc[count] & 0x3F) != 0) {
                            bits_asc -= 4;
                            *pChBitsAsc = bits_asc;

                            acc = ChCodeAsc[count] >> 4;
                            do {
                                pWork->offs2D34[acc] = count;
                                acc += add;
                            } while(acc < 0x100);
                        } else {
                            bits_asc -= 6;
                            *pChBitsAsc = bits_asc;

                            acc = ChCodeAsc[count] >> 6;
                            do {
                                pWork->offs2E34[acc] = count;
                                acc += add;
                            } while(acc < 0x80);
                        }
                    } else {
                        bits_asc -= 8;
                        *pChBitsAsc = bits_asc;

                        acc = ChCodeAsc[count] >> 8;
                        do {
                            pWork->offs2EB4[acc] = count;
                            acc += add;
                        } while(acc < 0x100);
                    }
                }
            }

            void GenDecodeTabs(byte* positions, byte[] start_indexes, byte* length_bits, int elements) {
                for(int i = 0; i < elements; i++) {
                    int length = 1 << length_bits[i];

                    for(int index = start_indexes[i]; index < 0x100; index += length)
                        positions[index] = (byte)i;
                }
            }

            uint DecodeLit(TDcmpStruct* pWork) {
                uint extra_length_bits;
                uint length_code;
                uint value;

                if((pWork->bit_buff & 1) != 0) {
                    if(WasteBits(pWork, 1)) return 0x306;

                    length_code = pWork->LengthCodes[pWork->bit_buff & 0xFF];

                    if(WasteBits(pWork, pWork->LenBits[length_code])) return 0x306;

                    if((extra_length_bits = pWork->ExLenBits[length_code]) != 0) {
                        uint extra_length = pWork->bit_buff & (uint)((1 << (int)extra_length_bits) - 1);

                        if(WasteBits(pWork, extra_length_bits))
                            if((length_code + extra_length) != 0x10E) return 0x306;
                        length_code = pWork->LenBase[length_code] + extra_length;
                    }

                    return length_code + 0x100;
                }

                if(WasteBits(pWork, 1)) return 0x306;

                if(pWork->ctype == (uint)CompressType.Binary) {
                    uint uncompressed_byte = pWork->bit_buff & 0xFF;
                    return WasteBits(pWork, 8) ? 0x306 : uncompressed_byte;
                }

                if((pWork->bit_buff & 0xFF) != 0) {
                    value = pWork->offs2C34[pWork->bit_buff & 0xFF];

                    if(value == 0xFF) {
                        if((pWork->bit_buff & 0x3F) != 0) {
                            if(WasteBits(pWork, 4)) return 0x306;

                            value = pWork->offs2D34[pWork->bit_buff & 0xFF];
                        } else {
                            if(WasteBits(pWork, 6)) return 0x306;

                            value = pWork->offs2E34[pWork->bit_buff & 0x7F];
                        }
                    }
                } else {
                    if(WasteBits(pWork, 8)) return 0x306;

                    value = pWork->offs2EB4[pWork->bit_buff & 0xFF];
                }

                return WasteBits(pWork, pWork->ChBitsAsc[value]) ? 0x306 : value;
            }

            uint DecodeDist(TDcmpStruct* pWork, uint rep_length) {
                uint dist_pos_code, dist_pos_bits, distance;

                dist_pos_code = pWork->DistPosCodes[pWork->bit_buff & 0xFF];
                dist_pos_bits = pWork->DistBits[dist_pos_code];
                if(WasteBits(pWork, dist_pos_bits)) return 0;

                if(rep_length == 2) {
                    distance = (dist_pos_code << 2) | (pWork->bit_buff & 0x03);
                    if(WasteBits(pWork, 2)) return 0;
                } else {
                    distance = (dist_pos_code << (int)pWork->dsize_bits) | (pWork->bit_buff & pWork->dsize_mask);
                    if(WasteBits(pWork, pWork->dsize_bits)) return 0;
                }
                return distance + 1;
            }

            bool WasteBits(TDcmpStruct* pWork, uint nBits) {
                if(nBits <= pWork->extra_bits) {
                    pWork->extra_bits -= nBits;
                    pWork->bit_buff >>= (int)nBits;
                    return false;
                }

                pWork->bit_buff >>= (int)pWork->extra_bits;
                if(pWork->in_pos == pWork->in_bytes) {
                    if((pWork->in_bytes = (uint)Read(pWork->in_buff, TDcmpStruct.BufferSize)) == 0)
                        return true;
                    pWork->in_pos = 0;
                }

                pWork->bit_buff |= (uint)(pWork->in_buff[pWork->in_pos++] << 8);
                pWork->bit_buff >>= (int)(nBits - pWork->extra_bits);
                pWork->extra_bits = (pWork->extra_bits - nBits) + 8;
                return false;
            }

            //Read stream to pointer
            private int Read(byte* buffer, int count) {
                byte[] buf = new byte[count];
                var read = sIn.Read(buf, 0, count);
                ArrayCopy(buf, buffer);
                return read;
            }

            //Write stream from pointer
            public bool Write(byte* buffer, int count) {
                byte[] buf = new byte[count];
                ArrayCopy(buffer, buf);
                return sOut.Write(buf, 0, count);
            }
        }
    }
}
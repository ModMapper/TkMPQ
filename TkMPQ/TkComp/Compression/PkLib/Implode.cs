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
        /// <summary>Pkware Implode Compression</summary>
        public unsafe class Implode
        {
            TCmpStruct Work = new TCmpStruct();
            FixedStream sIn, sOut;

            /// <summary>Create Implode Compress</summary>
            /// <param name="bIn">Input Buffer</param>
            /// <param name="bOut">Output Buffer</param>
            /// <param name="cType">Compress Type</param>
            /// <param name="dSize">Dictionary Size</param>
            public Implode(byte[] bIn, byte[] bOut, CompressType cType = CompressType.Binary, DictionarySize dSize = DictionarySize.Size3) {
                sIn = new FixedStream(bIn);
                sOut = new FixedStream(bOut);
                Init(dSize, cType);
            }

            internal Implode(FixedStream In, FixedStream Out, CompressType cType = CompressType.Binary, DictionarySize dSize = DictionarySize.Size3) {
                sIn = In;
                sOut = Out;
                Init(dSize, cType);
            }

            private void Init(DictionarySize dSize, CompressType cType) {
                Work.dsize_bytes = (uint)dSize;
                Work.ctype = (uint)cType;

                switch(dSize) {
                case DictionarySize.Size3:
                    Work.dsize_bits = 6;
                    Work.dsize_mask = 0x3F;
                    break;
                case DictionarySize.Size2:
                    Work.dsize_bits = 5;
                    Work.dsize_mask = 0x1F;
                    break;
                case DictionarySize.Size1:
                    Work.dsize_bits = 4;
                    Work.dsize_mask = 0x0F;
                    break;
                default:
                    throw new IndexOutOfRangeException();
                }

                // 포인터 사용을 위해 고정
                fixed (TCmpStruct* pWork = &Work) {
                    uint nCount;
                    switch(cType) {
                    case CompressType.Binary:
                        ushort nChCode = 0;
                        for(nCount = 0; nCount < 0x100; nCount++) {
                            pWork->nChBits[nCount] = 9;
                            pWork->nChCodes[nCount] = nChCode;
                            nChCode += 2;
                        }
                        break;
                    case CompressType.Ascii:
                        for(nCount = 0; nCount < 0x100; nCount++) {
                            pWork->nChBits[nCount] = (byte)(ChBitsAsc[nCount] + 1);
                            pWork->nChCodes[nCount] = (ushort)(ChCodeAsc[nCount] << 1);
                        }
                        break;
                    default:
                        throw new NotSupportedException();
                    }

                    for(int i = 0; i < 0x10; i++) for(uint nCount2 = 0; nCount2 < (1 << ExLenBits[i]); nCount2++) {
                            pWork->nChBits[nCount] = (byte)(ExLenBits[i] + LenBits[i] + 1);
                            pWork->nChCodes[nCount] = (ushort)((nCount2 << (LenBits[i] + 1)) | ((LenCode[i] & 0xFFFF00FF) * 2) | 1);
                            nCount++;
                        }

                    ArrayCopy(DistCode, pWork->dist_codes);
                    ArrayCopy(DistBits, pWork->dist_bits);
                }
            }

            /// <summary>Compress</summary>
            public int Compress() {
                // 포인터 사용을 위해 고정
                fixed (TCmpStruct* pWork = &Work) WriteCmpData(pWork);
                return sOut.EndOfStream ? -1 : sOut.Position;
            }

            private void WriteCmpData(TCmpStruct* pWork) {
                byte* input_data = pWork->work_buff + pWork->dsize_bytes + 0x204;
                bool input_data_ended = false;
                byte* input_data_end;
                uint phase = 0;

                pWork->out_buff[0] = (byte)pWork->ctype;
                pWork->out_buff[1] = (byte)pWork->dsize_bits;
                pWork->out_bytes = 2;

                //출력 버퍼 초기화
                memset(&pWork->out_buff[2], 0, TCmpStruct.BufferSize - 2);
                pWork->out_bits = 0;

                while(!input_data_ended) {
                    // 스트림에서 0x1000크기만큼 읽기
                    int total_loaded = Read(pWork->work_buff + pWork->dsize_bytes + 0x204, 0x1000);
                    if(total_loaded == 0 && phase == 0) break;          //Length = 0
                    if(total_loaded < 0x1000) input_data_ended = true;  //End of Stream

                    input_data_end = pWork->work_buff + pWork->dsize_bytes + total_loaded;
                    if(input_data_ended) input_data_end += 0x204;

                    switch(phase) {
                    case 0:
                        SortBuffer(pWork, input_data, input_data_end + 1);
                        phase++;
                        if(pWork->dsize_bytes != 0x1000)
                            phase++;
                        break;

                    case 1:
                        SortBuffer(pWork, input_data - pWork->dsize_bytes + 0x204, input_data_end + 1);
                        phase++;
                        break;

                    default:
                        SortBuffer(pWork, input_data - pWork->dsize_bytes, input_data_end + 1);
                        break;
                    }

                    while(input_data < input_data_end) {
                        uint rep_length = FindRep(pWork, input_data);
                        while(rep_length != 0) {
                            if(rep_length == 2 && 0x100 <= pWork->distance) break;

                            if(input_data_ended && input_data + rep_length > input_data_end) {
                                rep_length = (uint)(input_data_end - input_data);
                                if(rep_length < 2) break;

                                if(rep_length == 2 && 0x100 <= pWork->distance) break;
                                goto __FlushRepetition;
                            }

                            if(rep_length >= 8 || input_data + 1 >= input_data_end) goto __FlushRepetition;

                            uint save_rep_length = rep_length;
                            uint save_distance = pWork->distance;
                            rep_length = FindRep(pWork, input_data + 1);

                            if(rep_length > save_rep_length) {
                                if(rep_length > save_rep_length + 1 || save_distance > 0x80) {
                                    OutputBits(pWork, pWork->nChBits[*input_data], pWork->nChCodes[*input_data]);
                                    input_data++;
                                    continue;
                                }
                            }

                            rep_length = save_rep_length;
                            pWork->distance = save_distance;

                            __FlushRepetition:

                            OutputBits(pWork, pWork->nChBits[rep_length + 0xFE], pWork->nChCodes[rep_length + 0xFE]);
                            if(rep_length == 2) {
                                OutputBits(pWork, pWork->dist_bits[pWork->distance >> 2], pWork->dist_codes[pWork->distance >> 2]);
                                OutputBits(pWork, 2, pWork->distance & 3);
                            } else {
                                OutputBits(pWork, pWork->dist_bits[pWork->distance >> (int)pWork->dsize_bits], pWork->dist_codes[pWork->distance >> (int)pWork->dsize_bits]);
                                OutputBits(pWork, pWork->dsize_bits, pWork->dsize_mask & pWork->distance);
                            }

                            input_data += rep_length;
                            goto nonout;
                        }

                        OutputBits(pWork, pWork->nChBits[*input_data], pWork->nChCodes[*input_data]);
                        input_data++;
                        nonout:;
                    }

                    if(sOut.EndOfStream) return;
                    if(!input_data_ended) {
                        input_data -= 0x1000;
                        memmove(pWork->work_buff, pWork->work_buff + 0x1000, (int)pWork->dsize_bytes + 0x204);
                    }
                }

                OutputBits(pWork, pWork->nChBits[0x305], pWork->nChCodes[0x305]);
                if(pWork->out_bits != 0) pWork->out_bytes++;
                Write(pWork->out_buff, (int)pWork->out_bytes);
                return;
            }

            private void SortBuffer(TCmpStruct* pWork, byte* buffer_begin, byte* buffer_end) {
                ushort* phash_to_index;
                byte* buffer_ptr;
                ushort total_sum = 0;

                void* phash_to_index_end = (byte*)pWork->phash_to_index + TCmpStruct.IndexSize;

                // 버퍼 초기화
                memset(pWork->phash_to_index, 0, TCmpStruct.IndexSize);

                for(buffer_ptr = buffer_begin; buffer_ptr < buffer_end; buffer_ptr++)
                    pWork->phash_to_index[BYTE_PAIR_HASH(buffer_ptr)]++;

                for(phash_to_index = pWork->phash_to_index; phash_to_index < phash_to_index_end; phash_to_index++) {
                    total_sum += phash_to_index[0];
                    phash_to_index[0] = total_sum;
                }

                for(buffer_end--; buffer_end >= buffer_begin; buffer_end--) {
                    int byte_pair_hash = BYTE_PAIR_HASH(buffer_end);
                    pWork->phash_to_index[byte_pair_hash]--;
                    pWork->phash_offs[pWork->phash_to_index[byte_pair_hash]] = (ushort)(buffer_end - pWork->work_buff);
                }
            }

            private uint FindRep(TCmpStruct* pWork, byte* input_data) {
                const uint MAX_REP_LENGTH = 0x204;  //최대 반복 길이
                byte* repetition_limit, prev_repetition, prev_rep_end, input_data_ptr;
                uint equal_byte_count, rep_length, rep_length2;
                ushort offs_in_rep, di_val;
                ushort* phash_offs;

                ushort* phash_to_index = pWork->phash_to_index + BYTE_PAIR_HASH(input_data);
                ushort min_phash_offs = (ushort)((input_data - pWork->work_buff) - pWork->dsize_bytes + 1);
                ushort phash_offs_index = phash_to_index[0];

                phash_offs = pWork->phash_offs + phash_offs_index;
                if(*phash_offs < min_phash_offs) {
                    while(*phash_offs < min_phash_offs) {
                        phash_offs_index++;
                        phash_offs++;
                    }
                    *phash_to_index = phash_offs_index;
                }

                phash_offs = pWork->phash_offs + phash_offs_index;
                prev_repetition = pWork->work_buff + phash_offs[0];
                repetition_limit = input_data - 1;

                if(prev_repetition >= repetition_limit) return 0;

                input_data_ptr = input_data;
                rep_length = 1;
                for(;;) {
                    if(*input_data_ptr == *prev_repetition && input_data_ptr[rep_length - 1] == prev_repetition[rep_length - 1]) {
                        prev_repetition++;
                        input_data_ptr++;
                        equal_byte_count = 2;

                        while(equal_byte_count < MAX_REP_LENGTH) {
                            prev_repetition++;
                            input_data_ptr++;

                            if(*prev_repetition != *input_data_ptr) break;

                            equal_byte_count++;
                        }

                        input_data_ptr = input_data;
                        if(equal_byte_count >= rep_length) {
                            pWork->distance = (uint)(input_data - prev_repetition + equal_byte_count - 1);
                            if((rep_length = equal_byte_count) > 10) break;
                        }
                    }

                    phash_offs_index++;
                    phash_offs++;
                    prev_repetition = pWork->work_buff + phash_offs[0];

                    if(prev_repetition >= repetition_limit) return (rep_length >= 2) ? rep_length : 0;
                }

                if(equal_byte_count == MAX_REP_LENGTH) {
                    pWork->distance--;
                    return equal_byte_count;
                }

                phash_offs = pWork->phash_offs + phash_offs_index;
                if(pWork->work_buff + phash_offs[1] >= repetition_limit) return rep_length;

                pWork->offs09BC[0] = 0xFFFF;
                pWork->offs09BC[1] = 0x0000;
                di_val = 0;

                for(offs_in_rep = 1; offs_in_rep < rep_length;) {
                    if(input_data[offs_in_rep] != input_data[di_val]) {
                        di_val = pWork->offs09BC[di_val];
                        if(di_val != 0xFFFF)
                            continue;
                    }
                    pWork->offs09BC[++offs_in_rep] = ++di_val;
                }

                prev_repetition = pWork->work_buff + phash_offs[0];
                prev_rep_end = prev_repetition + rep_length;
                rep_length2 = rep_length;

                for(;;) {
                    rep_length2 = pWork->offs09BC[rep_length2];
                    if(rep_length2 == 0xFFFF) rep_length2 = 0;

                    phash_offs = pWork->phash_offs + phash_offs_index;

                    do {
                        phash_offs++;
                        phash_offs_index++;
                        prev_repetition = pWork->work_buff + *phash_offs;
                        if(prev_repetition >= repetition_limit)
                            return rep_length;
                    }
                    while(prev_repetition + rep_length2 < prev_rep_end);

                    byte pre_last_byte = input_data[rep_length - 2];
                    if(pre_last_byte == prev_repetition[rep_length - 2]) {
                        if(prev_repetition + rep_length2 != prev_rep_end) {
                            prev_rep_end = prev_repetition;
                            rep_length2 = 0;
                        }
                    } else {
                        phash_offs = pWork->phash_offs + phash_offs_index;
                        do {
                            phash_offs++;
                            phash_offs_index++;
                            prev_repetition = pWork->work_buff + *phash_offs;
                            if(prev_repetition >= repetition_limit)
                                return rep_length;
                        }
                        while(prev_repetition[rep_length - 2] != pre_last_byte || prev_repetition[0] != input_data[0]);

                        prev_rep_end = prev_repetition + 2;
                        rep_length2 = 2;
                    }

                    while(*prev_rep_end == input_data[rep_length2]) {
                        if(++rep_length2 >= 0x204) break;
                        prev_rep_end++;
                    }

                    if(rep_length2 >= rep_length) {
                        pWork->distance = (uint)(input_data - prev_repetition - 1);
                        if((rep_length = rep_length2) == 0x204) return rep_length;

                        while(offs_in_rep < rep_length2) {
                            if(input_data[offs_in_rep] != input_data[di_val]) {
                                di_val = pWork->offs09BC[di_val];
                                if(di_val != 0xFFFF)
                                    continue;
                            }
                            pWork->offs09BC[++offs_in_rep] = ++di_val;
                        }
                    }
                }
            }

            private void OutputBits(TCmpStruct* pWork, uint nbits, uint bit_buff) {
                if(nbits > 8) {
                    OutputBits(pWork, 8, bit_buff);
                    bit_buff >>= 8;
                    nbits -= 8;
                }

                uint out_bits = pWork->out_bits;
                pWork->out_buff[pWork->out_bytes] |= (byte)(bit_buff << (int)out_bits);
                pWork->out_bits += nbits;

                if(pWork->out_bits > 8) {
                    pWork->out_bytes++;
                    bit_buff >>= (int)(8 - out_bits);

                    pWork->out_buff[pWork->out_bytes] = (byte)bit_buff;
                    pWork->out_bits &= 7;
                } else {
                    pWork->out_bits &= 7;
                    if(pWork->out_bits == 0) pWork->out_bytes++;
                }

                if(pWork->out_bytes >= 0x800) FlushBuf(pWork);
            }

            private void FlushBuf(TCmpStruct* pWork) {
                byte save_ch1, save_ch2;

                Write(pWork->out_buff, 0x800);

                save_ch1 = pWork->out_buff[0x800];
                save_ch2 = pWork->out_buff[pWork->out_bytes];
                pWork->out_bytes -= 0x800;

                memset(pWork->out_buff, 0, TCmpStruct.BufferSize);

                if(pWork->out_bytes != 0) pWork->out_buff[0] = save_ch1;
                if(pWork->out_bits != 0) pWork->out_buff[pWork->out_bytes] = save_ch2;
            }

            int BYTE_PAIR_HASH(byte* buffer) {  //macro -> function
                return ((buffer[0] * 4) + (buffer[1] * 5));
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
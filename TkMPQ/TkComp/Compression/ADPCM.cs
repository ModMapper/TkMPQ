using TkLib;

namespace TkCompLib.Compression
{
    /// <summary>ADPCM Wave Comprssion</summary>
    public class ADPCM
    {
        private static int[] NextStepTable = {
            -1, 0, -1, 4, -1, 2, -1, 6,
            -1, 1, -1, 5, -1, 3, -1, 7,
            -1, 1, -1, 5, -1, 3, -1, 7,
            -1, 2, -1, 4, -1, 6, -1, 8};

        private static int[] StepSizeTable = {
                7,     8,     9,    10,     11,    12,    13,    14,
               16,    17,    19,    21,     23,    25,    28,    31,
               34,    37,    41,    45,     50,    55,    60,    66,
               73,    80,    88,    97,    107,   118,   130,   143,
              157,   173,   190,   209,    230,   253,   279,   307,
              337,   371,   408,   449,    494,   544,   598,   658,
              724,   796,   876,   963,   1060,  1166,  1282,  1411,
             1552,  1707,  1878,  2066,   2272,  2499,  2749,  3024,
             3327,  3660,  4026,  4428,   4871,  5358,  5894,  6484,
             7132,  7845,  8630,  9493,  10442, 11487, 12635, 13899,
             15289, 16818, 18500, 20350, 22385, 24623, 27086, 29794, 32767};

        private const int MAX_ADPCM_CHANNEL_COUNT = 2;
        private const int INITIAL_ADPCM_STEP_INDEX = 0x2C;

        /// <summary>ADPCM Wave Compress</summary>
        /// <param name="bIn">Input Buffer</param>
        /// <param name="bOut">Output Buffer</param>
        /// <param name="ChannelCount">Channel Count</param>
        /// <param name="Level">Compression Level</param>
        /// <returns>Output Size (Failed : -1)</returns>
        public static int Compress(byte[] bIn, byte[] bOut, int ChannelCount, int Level) {
            FixedStream sIn = new FixedStream(bIn), sOut = new FixedStream(bOut);
            return Compress(sIn, sOut, ChannelCount, Level);
        }

        /// <summary>ADPCM Wave Decompress</summary>
        /// <param name="bIn">Input Buffer</param>
        /// <param name="bOut">Output Buffer</param>
        /// <param name="ChannelCount">Channel Count</param>
        /// <returns>Output Size (Failed : -1)</returns>
        public static int Decompress(byte[] bIn, byte[] bOut, int ChannelCount) {
            FixedStream sIn = new FixedStream(bIn), sOut = new FixedStream(bOut);
            return Decompress(sIn, sOut, ChannelCount);
        }

        internal static int Compress(FixedStream sIn, FixedStream sOut, int ChannelCount, int Level) {
            int TotalStepSize, ChannelIndex, AbsDifference, Difference, MaxBitMask, StepSize;
            short[] PredictedSamples = new short[MAX_ADPCM_CHANNEL_COUNT];
            short[] StepIndexes = new short[MAX_ADPCM_CHANNEL_COUNT];
            byte BitShift = (byte)(Level - 1);
            short Input;

            sOut.Write(0);
            if(sOut.Write(BitShift)) return -1;

            PredictedSamples[0] = PredictedSamples[1] = 0;
            StepIndexes[0] = StepIndexes[1] = INITIAL_ADPCM_STEP_INDEX;

            for(int i = 0; i < ChannelCount; i++) {
                if(sIn.Read16(out PredictedSamples[i])) return sOut.Position;
                if(sOut.Write16(PredictedSamples[i])) return -1;
            }

            ChannelIndex = ChannelCount - 1;
            while(!sIn.Read16(out Input)) {
                int EncodedSample = 0;
                ChannelIndex = (ChannelIndex + 1) % ChannelCount;
                
                AbsDifference = Input - PredictedSamples[ChannelIndex];
                if(AbsDifference < 0) {
                    AbsDifference = -AbsDifference;
                    EncodedSample |= 0x40;
                }

                StepSize = StepSizeTable[StepIndexes[ChannelIndex]];
                if(AbsDifference < (StepSize >> Level)) {
                    if(StepIndexes[ChannelIndex] != 0)
                        StepIndexes[ChannelIndex]--;

                    if(sOut.Write(0x80)) return -1;
                } else {
                    while(AbsDifference > (StepSize << 1)) {
                        if(StepIndexes[ChannelIndex] >= 0x58) break;

                        StepIndexes[ChannelIndex] += 8;
                        if(StepIndexes[ChannelIndex] > 0x58)
                            StepIndexes[ChannelIndex] = 0x58;

                        StepSize = StepSizeTable[StepIndexes[ChannelIndex]];
                        if(sOut.Write(0x81)) return -1;
                    }

                    MaxBitMask = (1 << (BitShift - 1));
                    MaxBitMask = (MaxBitMask > 0x20) ? 0x20 : MaxBitMask;
                    Difference = StepSize >> BitShift;
                    TotalStepSize = 0;

                    for(int BitVal = 0x01; BitVal <= MaxBitMask; BitVal <<= 1) {
                        if((TotalStepSize + StepSize) <= AbsDifference) {
                            TotalStepSize += StepSize;
                            EncodedSample |= BitVal;
                        }
                        StepSize >>= 1;
                    }

                    PredictedSamples[ChannelIndex] = (short)UpdatePredictedSample(
                        PredictedSamples[ChannelIndex], EncodedSample, Difference + TotalStepSize);
                    if(sOut.Write((byte)EncodedSample)) return -1;

                    StepIndexes[ChannelIndex] = GetNextStepIndex(StepIndexes[ChannelIndex], EncodedSample);
                }
            }
            return sOut.Position;
        }
        
        internal static int Decompress(FixedStream sIn, FixedStream sOut, int ChannelCount) {
            short[] PredictedSamples = new short[MAX_ADPCM_CHANNEL_COUNT];
            short[] StepIndexes = new short[MAX_ADPCM_CHANNEL_COUNT];
            byte EncodedSample, BitShift;
            int ChannelIndex;

            PredictedSamples[0] = PredictedSamples[1] = 0;
            StepIndexes[0] = StepIndexes[1] = INITIAL_ADPCM_STEP_INDEX;

            sIn.Skip(1);
            if(sIn.Read(out BitShift)) return sOut.Position;

            for(int i = 0; i < ChannelCount; i++) {
                if(sIn.Read16(out PredictedSamples[i])) return sOut.Position;
                if(sOut.Write16(PredictedSamples[i])) return -1;
            }

            ChannelIndex = ChannelCount - 1;
            while(!sIn.Read(out EncodedSample)) {
                ChannelIndex = (ChannelIndex + 1) % ChannelCount;

                if(EncodedSample == 0x80) {
                    if(StepIndexes[ChannelIndex] != 0)
                        StepIndexes[ChannelIndex]--;

                    if(sOut.Write16(PredictedSamples[ChannelIndex])) return -1;
                } else if(EncodedSample == 0x81) {
                    StepIndexes[ChannelIndex] += 8;
                    if(StepIndexes[ChannelIndex] > 0x58)
                        StepIndexes[ChannelIndex] = 0x58;

                    ChannelIndex = (ChannelIndex + 1) % ChannelCount;
                } else {
                    int StepIndex = StepIndexes[ChannelIndex];
                    int StepSize = StepSizeTable[StepIndex];

                    PredictedSamples[ChannelIndex] = (short)DecodeSample(
                        PredictedSamples[ChannelIndex], EncodedSample, StepSize, StepSize >> BitShift);

                    if(sOut.Write16(PredictedSamples[ChannelIndex])) return -1;

                    StepIndexes[ChannelIndex] = GetNextStepIndex(StepIndex, EncodedSample);
                }
            }
            return sOut.Position;
        }

        private static short GetNextStepIndex(int StepIndex, int EncodedSample) {
            StepIndex += NextStepTable[EncodedSample & 0x1F];
            if(StepIndex < 0) return 0;
            else if(StepIndex > 88) return 88;
            return (short)StepIndex;
        }

        private static int UpdatePredictedSample(int PredictedSample, int EncodedSample, int Difference) {
            if((EncodedSample & 0x40) != 0) {
                PredictedSample -= Difference;
                if(PredictedSample < -32768) return -32768;
            } else {
                PredictedSample += Difference;
                if(PredictedSample > 32767) return 32767;
            }
            return PredictedSample;
        }

        private static int DecodeSample(int PredictedSample, int EncodedSample, int StepSize, int Difference) {
            if((EncodedSample & 0x01) != 0) Difference += (StepSize >> 0);
            if((EncodedSample & 0x02) != 0) Difference += (StepSize >> 1);
            if((EncodedSample & 0x04) != 0) Difference += (StepSize >> 2);
            if((EncodedSample & 0x08) != 0) Difference += (StepSize >> 3);
            if((EncodedSample & 0x10) != 0) Difference += (StepSize >> 4);
            if((EncodedSample & 0x20) != 0) Difference += (StepSize >> 5);

            return UpdatePredictedSample(PredictedSample, EncodedSample, Difference);
        }
    }
}

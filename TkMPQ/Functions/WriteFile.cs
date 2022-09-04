using System.IO;
using System;

namespace TkMPQLib
{
    using MPQ.DataTypes;
    using MPQ;

    public partial class TkMPQ
    {
        /// <summary>해당 경로에 새로운 파일을 생성합니다.</summary>
        /// <param name="FilePath">생성할 파일의 경로입니다.</param>
        /// <param name="Flags">파일의 속성입니다.</param>
        /// <param name="Locale">작성할 파일의 언어 코드입니다.</param>
        /// <returns>파일의 작성 스트림입니다.</returns>
        public virtual MPQWriter CreateFile(string FilePath, FileFlags Flags = FileFlags.Compressed, Locale Locale = Locale.Neutral) {
            if(!Created) throw new InvalidOperationException();
            return new MPQWriter(this, FilePath, Flags, Locale);
        }

        /// <summary>해당 작성 스트림의 내용을 MPQ에 작성합니다.</summary>
        /// <param name="Writer">파일의 작성 스트림입니다.</param>
        public virtual void WriteFile(MPQWriter Writer) {
            if(!Created) throw new InvalidOperationException();
            lock(ThreadLock) {
                int FileOffset = Last;
                Last += Writer.Data.WriteData(FileData, Header, ref FileOffset);
                int Index = BlockTable.Add(Writer.GetBlock(FileOffset));
                HashTable.Add(Writer.FileName, Writer.GetHash(Index));
                Listfile.Add(Writer.FileName);
            }
        }

        /// <summary>해당 스트림의 내용을 전부 MPQ에 작성합니다.</summary>
        /// <param name="Stream">MPQ에 작성할 스트림입니다.</param>
        /// <param name="FilePath">작성할 파일의 경로입니다.</param>
        /// <param name="Flags">작성할 파일의 속성입니다.</param>
        /// <param name="Locale">작성할 파일의 언어 코드입니다.</param>
        /// <param name="Compression">작성할 파일의 압축 방식입니다.</param>
        /// <param name="WaveLevel">작성할 파일의 웨이브 압축 레벨입니다.</param>
        public virtual void WriteFile(Stream Stream, string FilePath, FileFlags Flags = FileFlags.Compressed, Locale Locale = Locale.Neutral,
                Compressions Compression = Compressions.Implode, WaveLevel WaveLevel = WaveLevel.QualityHigh) {
            if(!Created) throw new InvalidOperationException();
            var Writer = new MPQWriter(this, FilePath, Flags, Locale);
            Writer.Compression = Compression;
            Writer.WaveLevel = WaveLevel;
            Stream.CopyTo(Writer, 0x200 << SectorSize);
            WriteFile(Writer);
            Writer.Dispose();
        }

        /// <summary>해당 스트림의 웨이브 파일을 전부 MPQ에 작성합니다.</summary>
        /// <param name="Stream">MPQ에 작성할 스트림입니다.</param>
        /// <param name="FilePath">작성할 파일의 경로입니다.</param>
        /// <param name="Flags">작성할 파일의 속성입니다.</param>
        /// <param name="Locale">작성할 파일의 언어 코드입니다.</param>
        /// <param name="Compression">작성할 파일의 압축 방식입니다.</param>
        /// <param name="WaveLevel">작성할 파일의 웨이브 압축 레벨입니다.</param>
        public unsafe virtual void WriteWaveFile(Stream Stream, string FilePath, FileFlags Flags = FileFlags.Compressed, Locale Locale = Locale.Neutral,
                Compressions Compression = Compressions.Huffman, WaveLevel WaveLevel = WaveLevel.QualityHigh) {
            if(!Created) throw new InvalidOperationException();
            var Writer = new MPQWriter(this, FilePath, Flags, Locale);
            Writer.Compression = Compression;
            Writer.WaveLevel = WaveLevel;
            Wave.WriteWave(Writer, Stream);
            WriteFile(Writer);
            Writer.Dispose();
        }

        /// <summary>해당 작성 스트림의 내용을 재작성합니다.</summary>
        /// <param name="Writer">파일의 작성 스트림입니다.</param>
        public unsafe virtual void RewriteFile(MPQWriter Writer)
            => RewriteFile(Writer, HashTable.Find(Writer.FileName, Writer.Locale));

        /// <summary>해당 작성 스트림의 내용을 해당 인덱스에 재작성합니다.</summary>
        /// <param name="Writer">파일의 작성 스트림입니다.</param>
        public unsafe virtual void RewriteFile(MPQWriter Writer, int Index) {
            if(!Created) throw new InvalidOperationException();
            if(!Exists(Index)) return;
            lock(ThreadLock) {
                Block b = BlockTable[HashTable[Index].Block];
                var Data = MPQData.Create(FileData, Header, SectorSize, b, Listfile.FindName(Index));
                if(Writer.Data.CompSize < Data.CompSize) {
                    Data.Dispose();     //Overwrite File
                    Writer.Data.WriteData(FileData, Header, ref b.FileOffset);
                } else {
                    Data.Dispose();     //Append File
                    b.FileOffset = Last;
                    Last += Writer.Data.WriteData(FileData, Header, ref b.FileOffset);
                }
                BlockTable[HashTable[Index].Block] = b;
            }
        }
    }
}

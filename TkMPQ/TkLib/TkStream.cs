using System.IO;

namespace TkLib
{
    internal static class TkStream {
        /// <summary>Copy Stream with size.</summary>
        /// <param name="Source">Source Stream.</param>
        /// <param name="Target">Target Stream.</param>
        /// <param name="Count">Copy Count</param>
        public static void Copy(this Stream Source, Stream Target, int Count) {
            const int Size = 4096;
            byte[] Buf = new byte[Size];
            int Read;
            while(Size < Count) {
                Read = Source.Read(Buf, 0, Size);
                if(Read == 0) return;
                Target.Write(Buf, 0, Read);
                Count -= Size;
            }
            Read = Source.Read(Buf, 0, Count);
            Target.Write(Buf, 0, Read);
        }
    }
}

using System;
using System.IO;

namespace TkLib
{
    internal class FixedWarpper : Stream
    {
        public readonly FixedStream BaseStream;

        public FixedWarpper(FixedStream Stream) {
            BaseStream = Stream;
        }

        public override bool CanRead => true;
        public override bool CanWrite => true;
        public override bool CanSeek => false;
        
        public override long Length => BaseStream.Length;

        public override long Position {
            get => BaseStream.Position;
            set => BaseStream.Position = (int)value;
        }

        public override void Flush() {}

        public override void SetLength(long value)
            => throw new NotSupportedException();

        public override int Read(byte[] buffer, int offset, int count)
            => BaseStream.Read(buffer, offset, count);

        public override void Write(byte[] buffer, int offset, int count)
            => BaseStream.Write(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin)
            => throw new NotSupportedException();
    }
}

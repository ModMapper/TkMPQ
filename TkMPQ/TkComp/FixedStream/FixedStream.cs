using System.IO;
using System;

namespace TkLib
{
    internal class FixedStream
    {
        private bool StreamEnd;
        private byte[] Buffer;
        private int _Position;
        private int _Length;
        private int Offset;

        public FixedStream(byte[] Buffer) {
            this.Buffer = Buffer;
            _Length = Buffer.Length;
            Position = 0;
            Offset = 0;
        }

        public FixedStream(byte[] Buffer, int Count) {
            if(Buffer.Length < Count) Count = Buffer.Length;
            this.Buffer = Buffer;
            _Length = Count;
            Position = 0;
            Offset = 0;
        }

        public FixedStream(byte[] Buffer, int Offset, int Count) {
            if(Buffer.Length < Count) Count = Buffer.Length;
            this.Buffer = Buffer;
            this.Offset = Offset;
            _Length = Count;
            Position = 0;
        }

        public void Clear() {
            _Length = Buffer.Length;
            Position = 0;
        }

        public void Clear(int Count) {
            if(Buffer.Length < Count) Count = Buffer.Length;
            _Length = Count;
            Position = 0;
        }

        public bool EndOfStream => StreamEnd;
        public int Length => _Length;

        public int Position {
            get => _Position;
            set {
                if(Length < value) value = Length;
                _Position = value;
                StreamEnd = false;
            }
        }

        private int GetSize(int count) {
            int Last = Length - Position;
            if(count <= Last) return count;
            StreamEnd = true;
            return Last;
        }

        public int Skip(int count) {
            int Size = GetSize(count);
            _Position += Size;
            return Size;
        }

        public int Read(byte[] buffer, int offset, int count) {
            int Size = GetSize(count);
            Array.Copy(Buffer, Offset + Position, buffer, offset, Size);
            _Position += Size;
            return Size;
        }

        public bool Write(byte[] buffer, int offset, int count) {
            int Size = GetSize(count);
            Array.Copy(buffer, offset, Buffer, Offset + Position, Size);
            _Position += Size;
            return EndOfStream;
        }
        
        public bool Read(out byte data) {
            if(Position == Length) {
                data = 0;
                return StreamEnd = true;
            } else {
                data = Buffer[Offset + Position];
                _Position += 1;
                return false;
            }
        }

        public bool Write(byte data) {
            if(Position == Length) return StreamEnd = true;
            Buffer[Offset + Position] = data;
            _Position += 1;
            return false;
        }

        public bool Read16(out short data) {
            byte[] Buf = new byte[2];
            bool R = Read(out Buf[0]) || Read(out Buf[1]);
            data = BitConverter.ToInt16(Buf, 0);
            return R; 
        }

        public bool Write16(short data)
            => Write(BitConverter.GetBytes(data), 0, 2);

        public bool Write32(int data)
            => Write(BitConverter.GetBytes(data), 0, 4);
    }
}

using System.Runtime.InteropServices;
using System;

namespace TkLib
{
    internal static unsafe class Memory
    {
        /// <summary>memchr</summary>
        public static void* memchr(void* ptr, byte ch, int length) {
            byte* bptr = (byte*)ptr, eptr = (byte*)ptr + length;
            for(; bptr < eptr; bptr++)
                if(*bptr == ch) break;
            return bptr;
        }

        /// <summary>memcmp</summary>
        public static int memcmp(void* x, void* y, int length) {
            byte* px = (byte*)x, py = (byte*)y;
            int cmp = 0;
            for(int i = 0; i < length && cmp == 0; i++)
                cmp = (*px++).CompareTo((*py++));
            return 0;
        }
        
        /// <summary>memcpy (Copy Memory)</summary>
        public static void* memcpy(void* dest, void* src, int length)
            => memmove(dest, src, length); //memmove == memcpy

        /// <summary>memmove (Move Memory)</summary>
        public static void* memmove(void* dest, void* src, int length) {
            if(length == 0) return dest;
            byte* pdest, psrc;
            if(dest < src) {
                pdest = (byte*)dest;
                psrc = (byte*)src;
                while(0 < length--) *pdest++ = *psrc++;
            } else {
                pdest = (byte*)dest + length - 1;
                psrc = (byte*)src + length - 1;
                while(0 < length--) *pdest-- = *psrc--;
            }
            return dest;
        }

        /// <summary>memset (Fill Memory)</summary>
        public static void* memset(void* ptr, byte fill, int length) {
            byte* bptr = (byte*)ptr;
            for(void* end = bptr + length; bptr != end;)
                *bptr++ = fill;
            return ptr;
        }

        /// <summary>Copy Array to Pointer</summary>
        /// <param name="src">Source Array</param>
        /// <param name="dest">Destination Pointer</param>
        /// <param name="length">Copy Length (Default : Array Length)</param>
        public static void ArrayCopy(Array src, void* dest, int length = -1) {
            if(length == -1) length = Buffer.ByteLength(src);
            GCHandle hGC = GCHandle.Alloc(src, GCHandleType.Pinned);
            memcpy(dest, hGC.AddrOfPinnedObject().ToPointer(), length);
            hGC.Free();
        }

        /// <summary>Copy Pointer to Array</summary>
        /// <param name="src">Source Array</param>
        /// <param name="dest">Destination Pointer</param>
        /// <param name="length">Copy Length (Default : Array Length)</param>
        public static void ArrayCopy(void* src, Array dest, int length = -1) {
            if(length == -1) length = Buffer.ByteLength(dest);
            GCHandle hGC = GCHandle.Alloc(dest, GCHandleType.Pinned);
            memcpy( hGC.AddrOfPinnedObject().ToPointer(), src, length);
            hGC.Free();
        }


    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace ReisUnpack
{
    static class Util
    {
        static readonly sbyte[] LogTable256 =
        {
            -1, 0, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3,
             4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4, 4,
             5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
             5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5, 5,
             6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
             6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
             6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
             6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6,
             7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
             7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
             7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
             7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
             7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
             7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
             7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
             7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7, 7,
        };
        internal static int GetLog(uint v)
        {
            uint t, tt;
            int result;

            if ((tt = (v >> 16)) != 0)
            {
                result = (t = (tt >> 8)) != 0 ? 24 + LogTable256[t] : 16 + LogTable256[tt];
            }
            else
            {
                result = (t = v >> 8) != 0 ? 8 + LogTable256[t] : LogTable256[v];
            }
            return result;
        }

        internal static byte GetDictLength(uint v)
        {
            return (byte)Math.Min(Math.Max(GetLog(v), 15), 25);
        }

        internal static byte[] StructureArrayToByteArray<T>(IEnumerable<T> arr) where T : struct
        {
            MemoryStream stream = new MemoryStream();
            foreach (T entry in arr)
            {
                byte[] data = StructureToByteArray(entry);
                stream.Write(data, 0, data.Length);
            }
            return stream.ToArray();
        }

        internal static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        {
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            T result = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(),typeof(T));
            handle.Free();
            return result;
        }

        internal static T ByteArrayToStructure<T>(byte[] bytes, int offset, int size)
        {
            IntPtr i = Marshal.AllocHGlobal(size);
            Marshal.Copy(bytes, offset, i, size);
            T result = (T)Marshal.PtrToStructure(i, typeof(T));
            Marshal.FreeHGlobal(i);
            return result;
        }

        internal static byte[] StructureToByteArray<T>(T structure) where T : struct
        {
            byte[] result = new byte[Marshal.SizeOf(structure)];
            GCHandle handle = GCHandle.Alloc(result, GCHandleType.Pinned);
            Marshal.StructureToPtr(structure, handle.AddrOfPinnedObject(), true);
            handle.Free();
            return result;
        }

        internal static uint Murmur2(byte[] str, uint seed)
        {
            uint l = (uint)str.Length;
            uint u = seed ^ l;
            int i = 0;

            while (l >= 4)
            {
                uint k = (uint)(((str[i] & 0xff)) |
                                ((str[++i] & 0xff) << 8) |
                                ((str[++i] & 0xff) << 16) |
                                ((str[++i] & 0xff) << 24));

                k = (((k & 0xffff) * 0x5bd1e995) + ((((k >> 16) * 0x5bd1e995) & 0xffff) << 16));
                k ^= k >> 24;
                k = (((k & 0xffff) * 0x5bd1e995) + ((((k >> 16) * 0x5bd1e995) & 0xffff) << 16));
                u = (((u & 0xffff) * 0x5bd1e995) + ((((u >> 16) * 0x5bd1e995) & 0xffff) << 16)) ^ k;
                l -= 4;
                ++i;
            }

            switch (l)
            {
                case 3:
                    u ^= (uint)((str[i + 2] & 0xff) << 16);
                    u ^= (uint)((str[i + 1] & 0xff) << 8);
                    u ^= (uint)((str[i] & 0xff));
                    u = (((u & 0xffff) * 0x5bd1e995) + ((((u >> 16) * 0x5bd1e995) & 0xffff) << 16));
                    break;
                case 2:
                    u ^= (uint)((str[i + 1] & 0xff) << 8);
                    u ^= (uint)((str[i] & 0xff));
                    u = (((u & 0xffff) * 0x5bd1e995) + ((((u >> 16) * 0x5bd1e995) & 0xffff) << 16));
                    break;
                case 1:
                    u ^= (uint)((str[i] & 0xff));
                    u = (((u & 0xffff) * 0x5bd1e995) + ((((u >> 16) * 0x5bd1e995) & 0xffff) << 16));
                    break;
            }

            u ^= u >> 13;
            u = (((u & 0xffff) * 0x5bd1e995) + ((((u >> 16) * 0x5bd1e995) & 0xffff) << 16));
            u ^= u >> 15;

            return u >> 0;
        }
    }
}

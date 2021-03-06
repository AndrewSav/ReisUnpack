﻿using System;
using System.Runtime.InteropServices;

namespace ReisUnpack.DataStructures
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct FileRegistryEntry
    {
        public UInt64 fileOffset;
        public UInt32 uncompressed;
        public UInt32 compressed;
        public byte flag;
    }
}

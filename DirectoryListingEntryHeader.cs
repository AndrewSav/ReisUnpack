using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace ReisUnpack
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct DirectoryListingEntryHeader
    {
        public ushort size;
        public ushort nameLength;
        public ushort numDirectories;
        public ushort numFiles;
    }
}

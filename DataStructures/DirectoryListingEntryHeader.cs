using System.Runtime.InteropServices;

namespace ReisUnpack.DataStructures
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

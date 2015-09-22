using System.Runtime.InteropServices;

namespace ReisUnpack
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Bucket
    {
        public uint IndexHash { get; set; }
        public uint BucketHash { get; set; }
        public int Index { get; set; }
    }
}

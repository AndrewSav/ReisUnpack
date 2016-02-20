using System;
using System.Runtime.InteropServices;

namespace ReisUnpack.DataStructures
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct TimHeader
    {
        public static uint signature = 1498763849;
        public static ushort currentVersion = 2;
        // Archive id 'YURI'
        public UInt32 id;
        // Size of the archive header
        public UInt32 headerSize;
        // Version format of the archive
        public UInt16 version;
        // Number of files contained in the archive
        public UInt32 fileCount;
        // Offset from the beginning of the archive to the directory listing
        public UInt64 directoryListingOffset;
        // The uncompressed size of the directory
        public UInt32 directoryListngSize;
        // The compressed size of the directory listing
        public UInt32 directoryListingCompressedSize;
        // Offset from the beginning of the archive to the file registry
        public UInt64 fileRegistryOffset;
        // Compressed size of the file registry
        public UInt32 fileRegistryCompressedSize;
        // Offset from the beginning of the archive to the hashtable
        public UInt64 hashtableOffset;
        // Size of the hashtable when compressed
        public UInt32 hashtableCompressedSize;
        // Offset from the beginning of the archive to the bucket table
        public UInt64 bucketTableOffset;
        // Size of the bucket table when compressed
        public UInt32 bucketTableCompressedSize;
        // Directory listing MD5 before compression
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] directoryListingDigest;
        // File registry MD5 before compression
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] fileRegistryDigest;
        // Hash table MD5 before compression
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] hashtableDigest;
        // Bucket table MD5 before compression
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] bucketTableDigest;
    }
}

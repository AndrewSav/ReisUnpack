﻿using System;
using System.Runtime.InteropServices;

namespace ReisUnpack
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct TimHeader
    {
        // Archive id 'YURI'
        public UInt32 id;
        // Size of the archive header
        public UInt32 headerSize;
        // Version format of the archive
        public UInt16 version;
        public UInt16 unknown1;
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
        public UInt32 unknown2;
        // Offset from the beginning of the archive to the hashtable
        public UInt64 hashtableOffset;
        // Size of the hashtable when compressed
        public UInt32 hashtableCompressedSize;
        public UInt32 unknown3;
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
        public UInt32 unknown4;
    }
}
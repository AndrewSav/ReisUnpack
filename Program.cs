using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using CommandLine;
using LzhamWrapper;
using LzhamWrapper.Enums;
using ReisUnpack.DataStructures;
using ReisUnpack.Engine;
using ReisUnpack.Options;

namespace ReisUnpack
{
    static class Program
    {
        static int Main(string[] args)
        {
            try
            {
                return Parser.Default.ParseArguments<UnpackOptions, RepackOptions>(args).MapResult(
                    (UnpackOptions u) => Unpack(u), 
                    (RepackOptions r) => Repack(r), 
                    errs => 1);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected error:");
                Console.WriteLine(ex.ToString());
            }
            return 2;
        }

        private static int Repack(RepackOptions options)
        {
            if (options.Level < 0 || options.Level > CompressionLevel.Uber)
            {
                Console.WriteLine("Compresson level should be between 0 and 4 (inclusive)");
                return 9;
            }
            CompressionLevel level = options.Level;
            string inputFolder = options.InputFolder ?? Environment.CurrentDirectory;
            File.Delete(options.OutputFile);
            using (FileStream fs = File.OpenWrite(options.OutputFile))
            {
                int headerSize = Marshal.SizeOf(typeof (TimHeader));
                byte[] headerBytes = new byte[headerSize];
                fs.Write(headerBytes,0, headerSize);                                
                Packer packer = new Packer(inputFolder,fs, level);
                Packer.PackerTables tables = packer.Pack();
                MD5 md5 = MD5.Create();
                TimHeader header = new TimHeader
                {
                    headerSize = (uint) headerSize,
                    version = TimHeader.currentVersion,
                    id = TimHeader.signature,
                    directoryListingDigest = md5.ComputeHash(tables.Directory),
                    fileRegistryDigest = md5.ComputeHash(tables.FileRegistry),
                    bucketTableDigest = md5.ComputeHash(tables.BucketTable),
                    hashtableDigest = md5.ComputeHash(tables.HashTable),
                    directoryListngSize = (uint) tables.Directory.Length,
                    fileCount = (uint) tables.FileCount
                };

                CompressionParameters p = new CompressionParameters
                {
                    DictionarySize = Util.GetDictLength((uint)tables.Directory.Length),
                    UpdateRate = TableUpdateRate.Fastest,
                    Level = level
                };

                byte[] outputBufer = new byte[Math.Max(tables.Directory.Length*2,128)];
                uint addler = 0;
                int outsize = outputBufer.Length;
                CompressStatus status = Lzham.CompressMemory(p, tables.Directory, tables.Directory.Length, 0, outputBufer, ref outsize, 0, ref addler);

                if (status != CompressStatus.Success)
                {
                    Console.WriteLine("Failed to pack directory listing. ({status})");
                    Environment.FailFast("Failed to pack directory listing. ({status})");
                }

                header.directoryListingCompressedSize = (uint)outsize;
                header.directoryListingOffset = (ulong)fs.Position;
                fs.Write(outputBufer,0, outsize);

                p = new CompressionParameters
                {
                    DictionarySize = Util.GetDictLength((uint)tables.FileRegistry.Length),
                    UpdateRate = TableUpdateRate.Fastest,
                    Level = level
                };

                outputBufer = new byte[Math.Max(tables.FileRegistry.Length*2,128)];
                outsize = outputBufer.Length;
                status = Lzham.CompressMemory(p, tables.FileRegistry, tables.FileRegistry.Length, 0, outputBufer, ref outsize, 0, ref addler);

                if (status != CompressStatus.Success)
                {
                    Console.WriteLine("Failed to pack file registry. ({status})");
                    Environment.FailFast("Failed to pack file registry. ({status})");
                }

                header.fileRegistryCompressedSize = (uint)outsize;
                header.fileRegistryOffset = (ulong)fs.Position;
                fs.Write(outputBufer, 0, outsize);

                p = new CompressionParameters
                {
                    DictionarySize = Util.GetDictLength((uint)tables.HashTable.Length),
                    UpdateRate = TableUpdateRate.Fastest,
                    Level = level
                };

                outputBufer = new byte[Math.Max(tables.HashTable.Length*2,128)];
                outsize = outputBufer.Length;
                status = Lzham.CompressMemory(p, tables.HashTable, tables.HashTable.Length, 0, outputBufer, ref outsize, 0, ref addler);

                if (status != CompressStatus.Success)
                {
                    Console.WriteLine("Failed to pack hash table. ({status})");
                    Environment.FailFast("Failed to pack hash table. ({status})");
                }

                header.hashtableCompressedSize = (uint)outsize;
                header.hashtableOffset = (ulong)fs.Position;
                fs.Write(outputBufer, 0, outsize);

                p = new CompressionParameters
                {
                    DictionarySize = Util.GetDictLength((uint)tables.BucketTable.Length),
                    UpdateRate = TableUpdateRate.Fastest,
                    Level = level
                };

                outputBufer = new byte[Math.Max(tables.BucketTable.Length*2,128)];
                outsize = outputBufer.Length;
                status = Lzham.CompressMemory(p, tables.BucketTable, tables.BucketTable.Length, 0, outputBufer, ref outsize, 0, ref addler);

                if (status != CompressStatus.Success)
                {
                    Console.WriteLine("Failed to pack bucket table. ({status})");
                    Environment.FailFast("Failed to pack bucket table. ({status})");
                }

                header.bucketTableCompressedSize = (uint)outsize;
                header.bucketTableOffset = (ulong)fs.Position;
                fs.Write(outputBufer, 0, outsize);

                fs.Seek(0, SeekOrigin.Begin);

                headerBytes = Util.StructureToByteArray(header);
                fs.Write(headerBytes,0, headerSize);
            }
            return 0;
        }

        private static int Unpack(UnpackOptions options)
        {
            string inputFile = options.InputFile;
            string outputFolder = options.OutputFolder ?? Environment.CurrentDirectory;
            using (FileStream fs = File.OpenRead(inputFile))
            using (BinaryReader br = new BinaryReader(fs))
            {
                int headerSize = Marshal.SizeOf(typeof(TimHeader));
                TimHeader header = Util.ByteArrayToStructure<TimHeader>(br.ReadBytes(headerSize));

                int exitCode = ValidateHeader(header);
                if (exitCode != 0)
                {
                    return exitCode;
                }

                fs.Seek((long)header.directoryListingOffset, SeekOrigin.Begin);
                byte[] directoryListingCompressed = br.ReadBytes((int)header.directoryListingCompressedSize);

                DecompressionParameters p = new DecompressionParameters
                {
                    DictionarySize = Util.GetDictLength(header.directoryListngSize),
                    UpdateRate = TableUpdateRate.Fastest
                };

                byte[] directoryListing = new byte[header.directoryListngSize];
                uint addler = 0;
                int outsize = (int)header.directoryListngSize;
                DecompressStatus status = Lzham.DecompressMemory(p, directoryListingCompressed, directoryListingCompressed.Length, 0, directoryListing, ref outsize, 0, ref addler);

                if (status != DecompressStatus.Success)
                {
                    Console.WriteLine("Failed to unpack directory listing");
                    return 5;
                }
                MD5 md5 = MD5.Create();
                byte[] directoryListingHash = md5.ComputeHash(directoryListing);
                if (!StructuralComparisons.StructuralEqualityComparer.Equals(directoryListingHash,header.directoryListingDigest))
                {
                    Console.WriteLine("Directory listing hash mismatch");
                    return 7;
                }

                fs.Seek((long)header.fileRegistryOffset, SeekOrigin.Begin);
                byte[] fileRegistryCompressed = br.ReadBytes((int)header.fileRegistryCompressedSize);

                outsize = (int)header.fileCount * 24;
                p = new DecompressionParameters
                {
                    DictionarySize = Util.GetDictLength((uint)outsize),
                    UpdateRate = TableUpdateRate.Fastest
                };
                byte[] fileRegistry = new byte[outsize];
                status = Lzham.DecompressMemory(p, fileRegistryCompressed, fileRegistryCompressed.Length, 0, fileRegistry, ref outsize, 0, ref addler);
                if (status != DecompressStatus.Success)
                {
                    Console.WriteLine("Failed to unpack file registry");
                    return 6;
                }
                byte[] fileRegistryHash = md5.ComputeHash(fileRegistry);
                if (!StructuralComparisons.StructuralEqualityComparer.Equals(fileRegistryHash, header.fileRegistryDigest))
                {
                    Console.WriteLine("File registry hash mismatch");
                    return 8;
                }
                Unpacker unpacker = new Unpacker(fs, br, fileRegistry, directoryListing, outputFolder, header.fileCount);
                unpacker.Unpack();

                return 0;
            }
        }

        private static int ValidateHeader(TimHeader header)
        {
            if (header.id != TimHeader.signature)
            {
                Console.WriteLine("Unknown format");
                return 3;
            }
            if (header.version != TimHeader.currentVersion)
            {
                Console.WriteLine("Unknown archive version");
                return 4;
            }
            return 0;
        }
    }
}

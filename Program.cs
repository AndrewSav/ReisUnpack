using System;
using System.IO;
using CommandLine;
using LzhamWrapper;
using LzhamWrapper.Enums;

namespace ReisUnpack
{
    static class Program
    {
        static int Main(string[] args)
        {
            try
            {
                return Parser.Default.ParseArguments<Options>(args).MapResult(Unpack, errs => 1);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unexpected error:");
                Console.WriteLine(ex.ToString());
            }
            return 2;
        }

        private static int Unpack(Options options)
        {
            string inputFile = options.InputFile;
            string outputFolder = options.OutputFolder ?? Environment.CurrentDirectory;
            using (FileStream fs = File.OpenRead(inputFile))
            using (BinaryReader br = new BinaryReader(fs))
            {
                // Archive id 'YURI'
                UInt32 id = br.ReadUInt32();
                if (id != 1498763849)
                {
                    Console.WriteLine("Unknown format");
                    return 3;
                }
                // Size of the archive header
                br.ReadUInt32();
                // Version format of the archive (0 = default)
                UInt16 version = br.ReadUInt16();
                if (version != 1)
                {
                    Console.WriteLine("Unknown archive version");
                    return 4;
                }
                br.ReadUInt16();
                // Number of files contained in the archive
                UInt32 fileCount = br.ReadUInt32();
                // Offset from the beginning of the archive to the directory listing
                UInt64 directoryListingOffset = br.ReadUInt64();
                // The uncompressed size of the directory
                UInt32 directoryListngSize = br.ReadUInt32();
                // The compressed size of the directory listing
                UInt32 directoryListingCompressedSize = br.ReadUInt32();
                // Offset from the beginning of the archive to the file registry
                UInt64 fileRegistryOffset = br.ReadUInt64();
                // Compressed size of the file registry
                UInt32 fileRegistryCompressedSize = br.ReadUInt32();


                fs.Seek((long)directoryListingOffset, SeekOrigin.Begin);
                byte[] directoryListingCompressed = br.ReadBytes((int)directoryListingCompressedSize);

                DecompressionParameters p = new DecompressionParameters
                {
                    DictionarySize = Util.GetDictLength(directoryListngSize),
                    UpdateRate = TableUpdateRate.Fastest
                };

                byte[] directoryListing = new byte[directoryListngSize];
                uint addler = 0;
                int outsize = (int)directoryListngSize;
                DecompressStatus status = Lzham.DecompressMemory(p, directoryListingCompressed, directoryListingCompressed.Length, 0, directoryListing, ref outsize, 0, ref addler);

                if (status != DecompressStatus.Success)
                {
                    Console.WriteLine("Failed to unpack directory listing");
                    return 5;
                }

                fs.Seek((long)fileRegistryOffset, SeekOrigin.Begin);
                byte[] fileRegistryCompressed = br.ReadBytes((int)fileRegistryCompressedSize);

                outsize = (int)fileCount * 24;
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
                Unpacker unpacker = new Unpacker(fs, br, fileRegistry, directoryListing, outputFolder, fileCount);
                unpacker.Unpack();

                return 0;
            }
        }
    }
}

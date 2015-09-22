using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using LzhamWrapper;
using LzhamWrapper.Enums;

namespace ReisUnpack
{
    internal class Unpacker
    {
        private int _fileCounter;
        private readonly byte[] _fileRegistry;
        private readonly byte[] _directoryListing;
        private readonly FileStream _fs;
        private readonly BinaryReader _br;
        private readonly string _outputFolder;
        private readonly uint _total;

        internal Unpacker(FileStream fs, BinaryReader br, byte[] fileRegistry, byte[] directoryListing, string outputFolder, uint total)
        {
            _fs = fs;
            _br = br;
            _fileRegistry = fileRegistry;
            _directoryListing = directoryListing;
            _outputFolder = outputFolder;
            _total = total;
        }

        internal void Unpack()
        {
            ReadDirectory(0, string.Empty);
        }

        private int ReadDirectory(int offset, string dirPrefix)
        {

            int size = Marshal.SizeOf(typeof(DirectoryListingEntryHeader));
            DirectoryListingEntryHeader dleh = Util.ByteArrayToStructure<DirectoryListingEntryHeader>(_directoryListing, offset,  size);

            string name = Encoding.UTF8.GetString(_directoryListing, offset + size, dleh.nameLength);
            var newPrefix = string.IsNullOrEmpty(name) ? dirPrefix : dirPrefix + "\\" + name;
            Console.WriteLine($"[FOLDR/{_total}]{newPrefix}");

            int currentOffset = size + offset + dleh.nameLength;

            for (int i = 0; i < dleh.numDirectories; i++)
            {
                currentOffset = ReadDirectory(currentOffset, newPrefix);
            }

            for (int i = 0; i < dleh.numFiles; i++)
            {
                _fileCounter++;
                ushort fileNameLength = BitConverter.ToUInt16(_directoryListing, currentOffset);
                currentOffset += 2;
                string fileName = Encoding.UTF8.GetString(_directoryListing, currentOffset, fileNameLength);
                Console.WriteLine($"[{_fileCounter - 1:D5}/{_total}]{newPrefix + "\\" + fileName}");
                WriteFile(newPrefix + "\\" + fileName, _fileCounter - 1);
                currentOffset += fileNameLength;
            }
            return currentOffset;
        }

        private void WriteFile(string fileName, int offset)
        {
            int size = Marshal.SizeOf(typeof (FileRegistryEntry));
            FileRegistryEntry fileRegistryEntry = Util.ByteArrayToStructure<FileRegistryEntry>(_fileRegistry, offset * size, size);

            string fullName = Path.Combine(_outputFolder, fileName.Trim('\\'));
            string dir = Path.GetDirectoryName(fullName);
            Debug.Assert(dir != null, "dir != null");
            Directory.CreateDirectory(dir);
            _fs.Seek((long)fileRegistryEntry.fileOffset, SeekOrigin.Begin);

            if ((fileRegistryEntry.flag & 1) == 1)
            {
                byte[] inp = _br.ReadBytes((int)fileRegistryEntry.compressed);
                byte[] outp = new byte[fileRegistryEntry.uncompressed];

                DecompressionParameters p = new DecompressionParameters
                {
                    DictionarySize = Util.GetDictLength(fileRegistryEntry.uncompressed),
                    UpdateRate = TableUpdateRate.Fastest
                };
                int u = (int)fileRegistryEntry.uncompressed;
                uint addler = 0;
                DecompressStatus status = Lzham.DecompressMemory(p, inp, inp.Length, 0, outp, ref u, 0, ref addler);

                if (status != DecompressStatus.Success)
                {
                    Console.WriteLine($"Error decompressing file {fileName}. ({status})");
                    Environment.FailFast($"Error decompressing file {fileName}. ({status})");
                }

                File.WriteAllBytes(fullName, outp);
            }
            else
            {
                var hm = _br.ReadBytes((int)fileRegistryEntry.compressed);
                File.WriteAllBytes(fullName, hm);
            }
        }
    }
}

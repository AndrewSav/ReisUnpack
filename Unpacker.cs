using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using LzhamWrapper;
using LzhamWrapper.Enums;

namespace ReisUnpack
{
    class Unpacker
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
            ushort nameLength = BitConverter.ToUInt16(_directoryListing, 2 + offset);
            ushort numDirectories = BitConverter.ToUInt16(_directoryListing, 4 + offset);
            ushort numFiles = BitConverter.ToUInt16(_directoryListing, 6 + offset);

            string name = Encoding.UTF8.GetString(_directoryListing, offset + 8, nameLength);
            var newPrefix = string.IsNullOrEmpty(name) ? dirPrefix : dirPrefix + "\\" + name;
            Console.WriteLine($"[FOLDR/{_total}]{newPrefix}");

            int currentOffset = 8 + offset + nameLength;

            for (int i = 0; i < numDirectories; i++)
            {
                currentOffset = ReadDirectory(currentOffset, newPrefix);
            }

            for (int i = 0; i < numFiles; i++)
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
            ulong fileOffset = BitConverter.ToUInt64(_fileRegistry, offset * 24);
            uint uncompressed = BitConverter.ToUInt32(_fileRegistry, offset * 24 + 8);
            uint compressed = BitConverter.ToUInt32(_fileRegistry, offset * 24 + 12);
            byte flag = _fileRegistry[offset * 24 + 16];

            string fullName = Path.Combine(_outputFolder, fileName.Trim('\\'));
            string dir = Path.GetDirectoryName(fullName);
            Debug.Assert(dir != null, "dir != null");
            Directory.CreateDirectory(dir);
            _fs.Seek((long)fileOffset, SeekOrigin.Begin);

            if ((flag & 1) == 1)
            {
                byte[] inp = _br.ReadBytes((int)compressed);
                byte[] outp = new byte[uncompressed];

                DecompressionParameters p = new DecompressionParameters
                {
                    DictionarySize = Util.GetDictLength(uncompressed),
                    UpdateRate = TableUpdateRate.Fastest
                };
                int u = (int)uncompressed;
                uint addler = 0;
                DecompressStatus status = Lzham.DecompressMemory(p, inp, inp.Length, 0, outp, ref u, 0, ref addler);

                if (status != DecompressStatus.Success)
                {
                    Console.WriteLine($"Error decompressing file {fileName}");
                    Environment.FailFast($"Error decompressing file {fileName}");
                }

                File.WriteAllBytes(fullName, outp);
            }
            else
            {
                var hm = _br.ReadBytes((int)compressed);
                File.WriteAllBytes(fullName, hm);
            }
        }
    }
}
